using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Imova.Domain.Properties;

namespace Imova.Application.Features.Agencies.Import;

// The CSV template an agency downloads for one property type: the header and one example row.
// Semicolon-separated and UTF-8 with a byte-order mark, so a double-click opens it in Excel set to
// Romanian (where the list separator is ";") with the diacritics intact. The import reads "," too.
public static class ImportTemplate
{
    public const string ContentType = "text/csv; charset=utf-8";

    public const string Delimiter = ";";

    public static string FileName(PropertyType propertyType) =>
        $"imova-import-{propertyType.ToString().ToLowerInvariant()}.csv";

    public static byte[] Build(PropertyType propertyType)
    {
        var columns = ImportColumns.For(propertyType);
        var example = ImportExamples.For(propertyType);

        using var buffer = new MemoryStream();
        using (var writer = new StreamWriter(buffer, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = Delimiter }))
        {
            foreach (var column in columns)
            {
                csv.WriteField(column.Name);
            }

            csv.NextRecord();

            foreach (var column in columns)
            {
                csv.WriteField(example.GetValueOrDefault(column.Name, string.Empty));
            }

            csv.NextRecord();
        }

        return buffer.ToArray();
    }
}
