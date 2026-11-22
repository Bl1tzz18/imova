using Imova.Application.Common;
using Imova.Application.Common.Emails;

namespace Imova.UnitTests.Common;

public class EmailLayoutTests
{
    [Theory]
    [InlineData("ro", "Politica de confidențialitate")]
    [InlineData("en", "Privacy policy")]
    [InlineData("ru", "Политика конфиденциальности")]
    // Every email ends with a link to the privacy policy, in the reader's language, under its own footer.
    public void Page_EndsWithALinkToThePrivacyPolicy(string language, string label)
    {
        var html = EmailLayout.Page("Titlu", EmailLayout.Paragraph("Corp"), "Subsol", language, "https://imova.md/ru/privacy");

        Assert.Contains($"""<a href="https://imova.md/ru/privacy" style="color:#7592ac;">{label}</a>""", html);
        Assert.True(html.IndexOf("Subsol", StringComparison.Ordinal) < html.IndexOf(label, StringComparison.Ordinal));
    }

    [Fact]
    public void AnAccountEmail_LinksToThePrivacyPolicyInTheRecipientsLanguage()
    {
        var privacyUrl = new AppOptions { WebBaseUrl = "https://imova.md" }.PrivacyUrl("en");

        var email = Imova.Application.Features.Auth.AccountEmails.PasswordResetEmail("ana@example.com", "https://imova.md/x", "en", privacyUrl);

        Assert.Contains("https://imova.md/en/privacy", email.HtmlBody);
    }
}
