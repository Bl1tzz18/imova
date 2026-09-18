using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Imova.Application.Common.Identity;
using Imova.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Imova.UnitTests.Auth;

public class JwtTokenGeneratorTests
{
    private static readonly JwtOptions Options = new()
    {
        Key = "this-is-a-test-signing-key-that-is-long-enough-for-hs256",
        Issuer = "imova-tests",
        Audience = "imova-tests-audience",
        ExpiryMinutes = 60,
    };

    private static ApplicationUser TestUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        DisplayName = "Test User",
    };

    private static TokenValidationParameters ValidationParameters(SecurityKey? signingKeyOverride = null, string? audienceOverride = null) => new()
    {
        ValidIssuer = Options.Issuer,
        ValidAudience = audienceOverride ?? Options.Audience,
        IssuerSigningKey = signingKeyOverride ?? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Key)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
    };

    [Fact]
    public void GenerateToken_ProducesTokenWithExpectedClaims()
    {
        var generator = new JwtTokenGenerator(Options);
        var user = TestUser();

        var token = generator.GenerateToken(user, new List<string> { "User" });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.DisplayName, jwt.Claims.Single(c => c.Type == "name").Value);
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "User");
        Assert.Equal(Options.Issuer, jwt.Issuer);
        Assert.Equal(Options.Audience, jwt.Audiences.Single());
    }

    [Fact]
    public void GenerateToken_WithNoDisplayName_FallsBackToEmailForNameClaim()
    {
        var generator = new JwtTokenGenerator(Options);
        var user = TestUser();
        user.DisplayName = null;

        var token = generator.GenerateToken(user, []);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == "name").Value);
    }

    [Fact]
    public void GenerateToken_WithMultipleRoles_IncludesAllRoleClaims()
    {
        var generator = new JwtTokenGenerator(Options);

        var token = generator.GenerateToken(TestUser(), new List<string> { "User", "Admin" });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        var roleValues = jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        Assert.Equal(new[] { "User", "Admin" }, roleValues);
    }

    [Fact]
    public void GenerateToken_SetsExpiryFromOptions()
    {
        var generator = new JwtTokenGenerator(Options);
        var before = DateTimeOffset.UtcNow;

        var token = generator.GenerateToken(TestUser(), []);

        var expectedExpiry = before.AddMinutes(Options.ExpiryMinutes);
        Assert.True(Math.Abs((token.ExpiresAt - expectedExpiry).TotalSeconds) < 5);
    }

    [Fact]
    public void GenerateToken_ProducesTokenThatValidatesWithCorrectSigningKeyIssuerAndAudience()
    {
        var generator = new JwtTokenGenerator(Options);
        var user = TestUser();

        var token = generator.GenerateToken(user, new List<string> { "User" });

        // Mirrors Program.cs's JwtBearerOptions.MapInboundClaims = false, without which the
        // handler remaps short claim names ("sub") to their long ClaimTypes.* equivalents.
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token.Value, ValidationParameters(), out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    }

    [Fact]
    public void GenerateToken_FailsValidation_WhenSigningKeyDoesNotMatch()
    {
        var generator = new JwtTokenGenerator(Options);
        var token = generator.GenerateToken(TestUser(), []);
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-signing-key-value"));

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token.Value, ValidationParameters(signingKeyOverride: wrongKey), out _));
    }

    [Fact]
    public void GenerateToken_FailsValidation_WhenAudienceDoesNotMatch()
    {
        var generator = new JwtTokenGenerator(Options);
        var token = generator.GenerateToken(TestUser(), []);

        Assert.Throws<SecurityTokenInvalidAudienceException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token.Value, ValidationParameters(audienceOverride: "some-other-audience"), out _));
    }
}
