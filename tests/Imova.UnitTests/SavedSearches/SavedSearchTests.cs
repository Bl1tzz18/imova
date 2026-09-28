using Imova.Domain.SavedSearches;

namespace Imova.UnitTests.SavedSearches;

public class SavedSearchTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private static SavedSearch Create(AlertFrequency frequency = AlertFrequency.Daily) =>
        SavedSearch.Create(Guid.NewGuid(), "  Apartamente Botanica  ", "transactionType=Sale", frequency, Now);

    [Fact]
    public void Create_TrimsTheName_AndStartsBothClocksAtCreation()
    {
        var search = Create();

        Assert.Equal("Apartamente Botanica", search.Name);
        Assert.Equal(Now, search.LastViewedAt);
        Assert.Equal(Now, search.LastAlertedUpTo);
        Assert.Null(search.LastAlertSentAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => SavedSearch.Create(Guid.NewGuid(), name, "", AlertFrequency.Off, Now));
    }

    [Fact]
    public void IsAlertDue_Instant_EveryRun_Off_Never()
    {
        Assert.True(Create(AlertFrequency.Instant).IsAlertDue(Now));
        Assert.False(Create(AlertFrequency.Off).IsAlertDue(Now));
    }

    [Fact]
    public void IsAlertDue_Daily_AtMostOneEmailPerDay()
    {
        var search = Create(AlertFrequency.Daily);
        Assert.True(search.IsAlertDue(Now));

        search.RecordAlertRun(Now, emailSent: true, Now);
        Assert.False(search.IsAlertDue(Now.AddHours(12)));
        Assert.True(search.IsAlertDue(Now.AddHours(23)));
    }

    [Fact]
    public void IsAlertDue_Daily_ARunWithoutAnEmailDoesntStartTheDay()
    {
        var search = Create(AlertFrequency.Daily);

        search.RecordAlertRun(Now, emailSent: false, Now);

        Assert.True(search.IsAlertDue(Now.AddMinutes(5)));
    }

    [Fact]
    public void RecordAlertRun_OnlyEverMovesTheWatermarkForward()
    {
        var search = Create();

        search.RecordAlertRun(Now.AddHours(1), emailSent: false, Now.AddHours(1));
        search.RecordAlertRun(Now.AddMinutes(30), emailSent: false, Now.AddHours(2));

        Assert.Equal(Now.AddHours(1), search.LastAlertedUpTo);
    }
}
