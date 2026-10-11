using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Imova.Application.Common;
using Imova.Domain.Agencies;

namespace Imova.Application.Features.Agencies.Import;

// One data line of an import file: its number as a spreadsheet shows it (the header is row 1) and
// its values by column name (trimmed; "" when the cell was empty or missing).
public sealed record ImportCsvRow(int RowNumber, IReadOnlyDictionary<string, string> Values)
{
    public string Get(string column) => Values.GetValueOrDefault(column, string.Empty);

    public bool Has(string column) => Get(column).Length > 0;
}

public sealed record ImportCsvFile(IReadOnlyList<string> Columns, IReadOnlyList<ImportCsvRow> Rows, Encoding Encoding, char Delimiter);

// Why a file as a whole can't be imported (ListingImportJob.FailureCode + its params).
public sealed record ImportFileError(string Code, string Message, IReadOnlyDictionary<string, string>? Params = null);

// Reads an agency's CSV the way people actually produce one:
// - encoding: UTF-8 (with or without a byte-order mark), UTF-16 with a byte-order mark, or else
//   what Excel saves as "CSV" on a Romanian (Windows-1250) or Russian (Windows-1251) Windows;
// - separator: ";" (Excel in Romanian), "," or a tab — whichever the header line uses most;
// - header: case and surrounding spaces don't matter; every name must be a known column
//   (ImportColumns.All — a typo would otherwise drop a field silently); external_ref and
//   property_type must be there; empty header cells (trailing separators) are ignored;
// - rows: completely empty lines are skipped; at most ListingImportJob.MaxRows.
public static class ImportCsvReader
{
    private static readonly string[] RequiredColumns = [ImportColumns.ExternalRef, ImportColumns.PropertyTypeColumn];

    private static readonly char[] Delimiters = [';', ',', '\t'];

    static ImportCsvReader()
    {
        // Windows-1250/1251 aren't built into .NET (Core) without this.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static (ImportCsvFile? File, ImportFileError? Error) Read(byte[] content)
    {
        var (text, encoding) = Decode(content);
        var delimiter = DetectDelimiter(text);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            BadDataFound = null,
            DetectColumnCountChanges = false,
            // Read blank lines as (empty) rows, skipped below — so row numbers stay the spreadsheet's.
            IgnoreBlankLines = false,
        };

        List<string?> header;
        var rows = new List<ImportCsvRow>();
        try
        {
            using var reader = new StringReader(text);
            using var parser = new CsvParser(reader, config);
            if (!parser.Read())
            {
                return (null, new ImportFileError(ErrorCodes.ImportNoRows, "The file is empty."));
            }

            header = parser.Record!.Select(name => Clean(name) is { Length: > 0 } clean ? clean.ToLowerInvariant() : null).ToList();
            var headerError = CheckHeader(header);
            if (headerError is not null)
            {
                return (null, headerError);
            }

            while (parser.Read())
            {
                var record = parser.Record!;
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var i = 0; i < header.Count; i++)
                {
                    if (header[i] is { } column)
                    {
                        values[column] = i < record.Length ? Clean(record[i]) : string.Empty;
                    }
                }

                if (values.Values.All(v => v.Length == 0))
                {
                    continue;
                }

                if (rows.Count == ListingImportJob.MaxRows)
                {
                    return (null, new ImportFileError(
                        ErrorCodes.ImportTooManyRows,
                        $"The file has more than {ListingImportJob.MaxRows} rows.",
                        new Dictionary<string, string> { ["max"] = ListingImportJob.MaxRows.ToString(CultureInfo.InvariantCulture) }));
                }

                rows.Add(new ImportCsvRow(parser.Row, values));
            }
        }
        catch (CsvHelperException)
        {
            return (null, new ImportFileError(ErrorCodes.ImportFileUnreadable, "The file isn't a readable CSV."));
        }

        if (rows.Count == 0)
        {
            return (null, new ImportFileError(ErrorCodes.ImportNoRows, "The file has a header but no rows."));
        }

        return (new ImportCsvFile(header.OfType<string>().ToList(), rows, encoding, delimiter), null);
    }

    // A byte-order mark decides; without one, UTF-8 if the bytes are valid UTF-8, else Windows-1251 or -1250.
    public static (string Text, Encoding Encoding) Decode(byte[] content)
    {
        if (content is [0xEF, 0xBB, 0xBF, ..])
        {
            return (Encoding.UTF8.GetString(content, 3, content.Length - 3), Encoding.UTF8);
        }

        if (content is [0xFF, 0xFE, ..])
        {
            return (Encoding.Unicode.GetString(content, 2, content.Length - 2), Encoding.Unicode);
        }

        if (content is [0xFE, 0xFF, ..])
        {
            return (Encoding.BigEndianUnicode.GetString(content, 2, content.Length - 2), Encoding.BigEndianUnicode);
        }

        try
        {
            var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return (strictUtf8.GetString(content), Encoding.UTF8);
        }
        catch (DecoderFallbackException)
        {
            var legacy = Encoding.GetEncoding(LooksCyrillic(content) ? 1251 : 1250);
            return (legacy.GetString(content), legacy);
        }
    }

    // Excel's "CSV" from a Russian Windows is Windows-1251, from a Romanian one Windows-1250. Every
    // Cyrillic letter is a byte ≥ 0x80 in 1251, while Romanian text is mostly ASCII letters with the
    // odd ă/î/ș — so when such bytes make up a good part of the letters, it's Cyrillic.
    private static bool LooksCyrillic(byte[] content)
    {
        var high = content.Count(b => b >= 0xC0);
        var ascii = content.Count(b => b is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z');
        return high > 0 && high >= (high + ascii) * 0.3;
    }

    // The separator the header line uses most (outside quotes); "," when it has none.
    public static char DetectDelimiter(string text)
    {
        var counts = Delimiters.ToDictionary(d => d, _ => 0);
        var inQuotes = false;
        foreach (var c in text)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && (c == '\n' || c == '\r'))
            {
                break;
            }
            else if (!inQuotes && counts.ContainsKey(c))
            {
                counts[c]++;
            }
        }

        var best = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => Array.IndexOf(Delimiters, kv.Key)).First();
        return best.Value > 0 ? best.Key : ',';
    }

    private static ImportFileError? CheckHeader(IReadOnlyList<string?> header)
    {
        var names = header.OfType<string>().ToList();

        var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            return ColumnsError(ErrorCodes.ImportDuplicateColumns, "Columns appear more than once", duplicates);
        }

        var unknown = names.Where(n => !ImportColumns.All.ContainsKey(n)).ToList();
        if (unknown.Count > 0)
        {
            return ColumnsError(ErrorCodes.ImportUnknownColumns, "Unknown columns", unknown);
        }

        var missing = RequiredColumns.Where(c => !names.Contains(c)).ToList();
        return missing.Count > 0 ? ColumnsError(ErrorCodes.ImportMissingColumns, "Missing columns", missing) : null;
    }

    private static ImportFileError ColumnsError(string code, string message, IReadOnlyList<string> columns) =>
        new(code, $"{message}: {string.Join(", ", columns)}.", new Dictionary<string, string> { ["columns"] = string.Join(", ", columns) });

    // Trimmed, without a stray byte-order mark or non-breaking spaces at the ends.
    private static string Clean(string? value) => (value ?? string.Empty).Trim().Trim('﻿', ' ').Trim();
}
