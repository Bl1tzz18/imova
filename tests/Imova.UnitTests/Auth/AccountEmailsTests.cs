using Imova.Application.Features.Auth;

namespace Imova.UnitTests.Auth;

public class AccountEmailsTests
{
    private const string Link = "https://imova.test/confirm-email?userId=1&token=abc";

    [Fact]
    public void ConfirmationEmail_HasTheLinkOnAButtonAndSpelledOut_InTheSharedLayout()
    {
        var email = AccountEmails.ConfirmationEmail("ana@example.com", "Ana Rusu", Link);

        Assert.Equal("Confirmă adresa de email — IMOVA", email.Subject);
        Assert.NotNull(email.HtmlBody);
        Assert.Contains("Bună, Ana Rusu!", email.HtmlBody);
        Assert.Contains(">Confirmă adresa de email</a>", email.HtmlBody);
        // Button and fallback both carry the link ('&' encoded inside HTML).
        Assert.Equal(3, CountOf(email.HtmlBody, "https://imova.test/confirm-email?userId=1&amp;token=abc"));
        Assert.Contains(">IMOVA<", email.HtmlBody);
        Assert.Contains(Link, email.TextBody);
    }

    [Fact]
    public void ConfirmationEmail_WithoutAName_GreetsPlainly_AndEncodesAName()
    {
        Assert.Contains("Bună!", AccountEmails.ConfirmationEmail("ana@example.com", null, Link).HtmlBody);

        var html = AccountEmails.ConfirmationEmail("ana@example.com", "<b>Ana</b>", Link).HtmlBody!;
        Assert.DoesNotContain("<b>Ana</b>", html);
        Assert.Contains("&lt;b&gt;Ana&lt;/b&gt;", html);
    }

    [Fact]
    public void PasswordResetEmail_HasTheLinkOnAButton_AndSaysItCanBeIgnored()
    {
        const string link = "https://imova.test/reset-password?email=ana%40example.com&token=xyz";

        var email = AccountEmails.PasswordResetEmail("ana@example.com", link);

        Assert.Equal("Resetează parola — IMOVA", email.Subject);
        Assert.Contains(">Alege o parolă nouă</a>", email.HtmlBody);
        Assert.Contains("href=\"https://imova.test/reset-password?email=ana%40example.com&amp;token=xyz\"", email.HtmlBody);
        Assert.Contains("parola ta rămâne neschimbată", email.HtmlBody);
        Assert.Contains(link, email.TextBody);
    }

    private static int CountOf(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
