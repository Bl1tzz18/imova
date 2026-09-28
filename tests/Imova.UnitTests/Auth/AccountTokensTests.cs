using Imova.Application.Features.Auth;

namespace Imova.UnitTests.Auth;

public class AccountTokensTests
{
    [Fact]
    public void Encode_ProducesAUrlSafeValueThatDecodesBack()
    {
        // Identity tokens are Base64 with '+', '/' and '=' — exactly what breaks in a URL.
        const string token = "CfDJ8+abc/def==";

        var encoded = AccountTokens.Encode(token);

        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('=', encoded);
        Assert.Equal(token, AccountTokens.Decode(encoded));
    }

    [Theory]
    [InlineData("not base64url!")]
    [InlineData("a")]
    public void Decode_WithAMangledValue_ReturnsNull(string encoded)
    {
        Assert.Null(AccountTokens.Decode(encoded));
    }
}
