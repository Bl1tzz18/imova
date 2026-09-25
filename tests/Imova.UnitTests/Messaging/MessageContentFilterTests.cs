using Imova.Application.Features.Messaging;

namespace Imova.UnitTests.Messaging;

public class MessageContentFilterTests
{
    [Theory]
    [InlineData("Trimite banii prin Western Union", "Money transfer service")]
    [InlineData("Te rog plătește în avans 200 euro", "Advance payment request")]
    [InlineData("Please send an upfront payment first", "Advance payment request")]
    [InlineData("Нужна предоплата за просмотр", "Advance payment request")]
    [InlineData("Dă-mi codul din SMS ca să confirm", "Card or verification details")]
    [InlineData("Scrie-mi numărul cardului", "Card or verification details")]
    [InlineData("Accept doar Revolut sau bitcoin", "Off-platform payment")]
    [InlineData("Detalii aici: https://example.com/oferta", "External link")]
    public void SuspiciousMessages_AreFlaggedWithAReason(string body, string reason)
    {
        Assert.Equal(reason, MessageContentFilter.Check(body));
    }

    [Theory]
    [InlineData("Bună ziua, mai este disponibil apartamentul?")]
    [InlineData("Putem vedea casa sâmbătă la ora 10?")]
    [InlineData("Care este prețul final?")]
    [InlineData("")]
    [InlineData(null)]
    public void OrdinaryMessages_AreNotFlagged(string? body)
    {
        Assert.Null(MessageContentFilter.Check(body));
    }

    [Fact]
    public void Fold_LowercasesAndDropsDiacritics()
    {
        Assert.Equal("platesc in avans", MessageContentFilter.Fold("Plătesc ÎN Avans"));
    }
}
