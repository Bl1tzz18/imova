using Imova.Domain.Agencies;

namespace Imova.UnitTests.Agencies;

public class AgencySlugTests
{
    [Theory]
    [InlineData("Casa Ta Imobiliare", "casa-ta-imobiliare")]
    [InlineData("Agenția Ușoară", "agentia-usoara")]
    [InlineData("Ţară şi Ştiinţă", "tara-si-stiinta")]
    [InlineData("Înălțimi & Câmpii", "inaltimi-campii")]
    [InlineData("  --Imobil   Grup!!  ", "imobil-grup")]
    [InlineData("Дом и Жильё", "dom-i-zhile")]
    [InlineData("Объект", "obekt")]
    [InlineData("Ӂамбул", "jambul")]
    [InlineData("Agenție 2025", "agentie-2025")]
    public void FromName_LowerCasesTransliteratesAndDashes(string name, string expected)
    {
        Assert.Equal(expected, AgencySlug.FromName(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("!!! ???")]
    [InlineData("★★★")]
    public void FromName_WithNothingUsable_FallsBack(string name)
    {
        Assert.Equal(AgencySlug.Fallback, AgencySlug.FromName(name));
    }

    [Theory]
    [InlineData("New", "new-2")]
    [InlineData("ADMIN", "admin-2")]
    [InlineData("Edit", "edit-2")]
    [InlineData("api", "api-2")]
    public void FromName_ReservedWord_IsNumbered(string name, string expected)
    {
        Assert.Equal(expected, AgencySlug.FromName(name));
    }

    [Fact]
    public void FromName_IsCutToTheMaximumLength_WithoutATrailingDash()
    {
        var slug = AgencySlug.FromName(string.Concat(Enumerable.Repeat("imobil ", 40)));

        Assert.True(slug.Length <= AgencySlug.MaxLength);
        Assert.False(slug.EndsWith('-'));
        Assert.StartsWith("imobil-imobil", slug);
    }

    [Fact]
    public void WithNumber_AppendsTheNumber()
    {
        Assert.Equal("imobil-grup-2", AgencySlug.WithNumber("imobil-grup", 2));
        Assert.Equal("imobil-grup-13", AgencySlug.WithNumber("imobil-grup", 13));
    }

    [Fact]
    public void WithNumber_ShortensALongSlugSoTheResultStillFits()
    {
        var longSlug = new string('a', AgencySlug.MaxLength);

        var numbered = AgencySlug.WithNumber(longSlug, 12);

        Assert.Equal(AgencySlug.MaxLength, numbered.Length);
        Assert.EndsWith("a-12", numbered);
    }

    [Fact]
    public void WithNumber_BelowTwo_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AgencySlug.WithNumber("x", 1));
    }
}
