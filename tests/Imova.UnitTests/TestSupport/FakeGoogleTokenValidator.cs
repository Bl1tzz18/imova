using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

internal sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public GoogleUserInfo? UserToReturn { get; set; }

    public Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken) =>
        Task.FromResult(UserToReturn);
}
