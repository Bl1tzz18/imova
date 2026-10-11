using System.Text;
using Imova.Application.Common;
using Imova.Application.Features.Agencies.Import;
using Imova.Domain.Agencies;

namespace Imova.UnitTests.Agencies;

// Reading the file (encoding, separator, header, rows) and its cells (numbers, yes/no, choices, …).
public class ImportReadingTests
{
    static ImportReadingTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static byte[] Utf8(string text, bool bom = false) =>
        bom ? [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(text)] : Encoding.UTF8.GetBytes(text);

    [Fact]
    public void ASemicolonFile_WithABom_ReadsByColumnName_WithSpreadsheetRowNumbers()
    {
        var text = "External_Ref ; property_type;title;\r\nAP-1;Apartment;\"Apartament; 2 camere\"\r\n\r\n;;\r\nAP-2;Apartment;\"Pe două\nrânduri\"\r\nAP-3;Apartment;Fără ghilimele\r\n";

        var (file, error) = ImportCsvReader.Read(Utf8(text, bom: true));

        Assert.Null(error);
        Assert.Equal(';', file!.Delimiter);
        Assert.Equal(["external_ref", "property_type", "title"], file.Columns);
        Assert.Equal(3, file.Rows.Count);
        Assert.Equal((2, "Apartament; 2 camere"), (file.Rows[0].RowNumber, file.Rows[0].Get("title")));
        // The blank line and the line of empty cells are rows 3 and 4 of the spreadsheet.
        Assert.Equal((5, "Pe două\nrânduri"), (file.Rows[1].RowNumber, file.Rows[1].Get("title")));
        Assert.Equal(6, file.Rows[2].RowNumber);
        Assert.Equal(string.Empty, file.Rows[0].Get("locality"));
    }

    [Fact]
    public void CommasAndTabs_AreRecognised_FromTheHeader()
    {
        Assert.Equal(',', ImportCsvReader.Read(Utf8("external_ref,property_type,title\nA,House,\"x; y\"\n")).File!.Delimiter);
        Assert.Equal('\t', ImportCsvReader.Read(Utf8("external_ref\tproperty_type\nA\tHouse\n")).File!.Delimiter);
    }

    [Fact]
    public void ExcelFiles_WithoutUtf8_AreReadAsRomanianOrRussianWindows()
    {
        // Windows-1250 has ş with a cedilla, not ș with a comma — Romanian Excel writes that.
        var romanian = Encoding.GetEncoding(1250).GetBytes("external_ref;property_type;title\nA;Room;Cameră în Râşcani, aproape de parc\n");
        var russian = Encoding.GetEncoding(1251).GetBytes("external_ref;property_type;title\nA;Room;Комната в Рышкановке\n");

        var (ro, _) = ImportCsvReader.Read(romanian);
        var (ru, _) = ImportCsvReader.Read(russian);

        Assert.Equal(1250, ro!.Encoding.CodePage);
        Assert.Equal("Cameră în Râşcani, aproape de parc", ro.Rows[0].Get("title"));
        Assert.Equal(1251, ru!.Encoding.CodePage);
        Assert.Equal("Комната в Рышкановке", ru.Rows[0].Get("title"));
    }

    [Theory]
    [InlineData("external_ref;property_type;rooom\nA;Apartment;2\n", ErrorCodes.ImportUnknownColumns, "rooom")]
    [InlineData("external_ref;property_type;title;Title\nA;Apartment;x;y\n", ErrorCodes.ImportDuplicateColumns, "title")]
    [InlineData("property_type;title\nApartment;x\n", ErrorCodes.ImportMissingColumns, "external_ref")]
    public void AHeaderThatWouldLoseData_FailsTheFile(string text, string code, string columns)
    {
        var (file, error) = ImportCsvReader.Read(Utf8(text));

        Assert.Null(file);
        Assert.Equal(code, error!.Code);
        Assert.Equal(columns, error.Params!["columns"]);
    }

    [Fact]
    public void AnEmptyFile_AHeaderOnly_OrTooManyRows_FailTheFile()
    {
        Assert.Equal(ErrorCodes.ImportNoRows, ImportCsvReader.Read([]).Error!.Code);
        Assert.Equal(ErrorCodes.ImportNoRows, ImportCsvReader.Read(Utf8("external_ref;property_type\n;\n")).Error!.Code);

        var tooMany = new StringBuilder("external_ref;property_type\n");
        for (var i = 0; i <= ListingImportJob.MaxRows; i++)
        {
            tooMany.Append($"R{i};Garage\n");
        }

        var error = ImportCsvReader.Read(Utf8(tooMany.ToString())).Error!;
        Assert.Equal(ErrorCodes.ImportTooManyRows, error.Code);
        Assert.Equal("2000", error.Params!["max"]);
    }

    [Fact]
    public void TrailingEmptyHeaderCells_AreIgnored()
    {
        var (file, error) = ImportCsvReader.Read(Utf8("external_ref;property_type;;\nA;Garage;;\n"));

        Assert.Null(error);
        Assert.Equal(["external_ref", "property_type"], file!.Columns);
    }

    [Theory]
    [InlineData("78000", 78000)]
    [InlineData("78 000", 78000)]
    [InlineData("58,5", 58.5)]
    [InlineData("58.5", 58.5)]
    [InlineData("-1", -1)]
    public void Numbers_ReadWithADotOrAComma(string value, double expected)
    {
        Assert.True(ImportValues.TryDecimal(value, out var result));
        Assert.Equal((decimal)expected, result);
    }

    [Theory]
    [InlineData("1,234.5")]
    [InlineData("12a")]
    [InlineData("")]
    public void AmbiguousOrNonNumbers_DontRead(string value) => Assert.False(ImportValues.TryDecimal(value, out _));

    [Fact]
    public void WholeNumbers_YesNo_Dates_Choices_AndLists()
    {
        Assert.True(ImportValues.TryInt(" 12 ", out var twelve) && twelve == 12);
        Assert.False(ImportValues.TryInt("2.5", out _));

        Assert.True(ImportValues.TryYesNo("Da", out var yes) && yes);
        Assert.True(ImportValues.TryYesNo("0", out var no) && !no);
        Assert.False(ImportValues.TryYesNo("poate", out _));

        Assert.True(ImportValues.TryDate("2026-11-01", out var iso) && iso == new DateOnly(2026, 11, 1));
        Assert.True(ImportValues.TryDate("01.11.2026", out var local) && local == new DateOnly(2026, 11, 1));
        Assert.False(ImportValues.TryDate("11/01/2026", out _));

        Assert.Equal("OwnBoiler", ImportValues.MatchChoice("own_boiler", ["OwnBoiler", "HeatPump"]));
        Assert.Equal("OwnBoiler", ImportValues.MatchChoice(" OWN boiler", ["OwnBoiler", "HeatPump"]));
        Assert.Null(ImportValues.MatchChoice("centrala", ["OwnBoiler", "HeatPump"]));

        Assert.Equal(["a", "b"], ImportValues.List(" a | | b |"));
    }
}
