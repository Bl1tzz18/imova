namespace Imova.Application.Common.Interfaces;

public record GoogleUserInfo(string Email, string? Name);

public interface IGoogleTokenValidator
{
    // Returns null when the token fails verification (bad signature, wrong audience, expired, …).
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
