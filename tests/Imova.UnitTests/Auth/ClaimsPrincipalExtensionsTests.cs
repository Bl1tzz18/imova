using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Imova.Api.Common;

namespace Imova.UnitTests.Auth;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_WithSubClaim_ReturnsParsedGuid()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())]));

        Assert.Equal(userId, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_WithoutSubClaim_Throws()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }

    [Fact]
    public void GetUserId_WithMalformedSubClaim_Throws()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid")]));

        Assert.ThrowsAny<FormatException>(() => principal.GetUserId());
    }
}
