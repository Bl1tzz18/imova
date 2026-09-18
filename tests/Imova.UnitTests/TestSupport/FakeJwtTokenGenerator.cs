using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

// Isolates handler tests (e.g. GoogleLoginHandler) from real JWT signing, which has its own
// dedicated coverage in JwtTokenGeneratorTests.
internal sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public ApplicationUser? LastUser { get; private set; }

    public IList<string>? LastRoles { get; private set; }

    public JwtToken GenerateToken(ApplicationUser user, IList<string> roles)
    {
        LastUser = user;
        LastRoles = roles;
        return new JwtToken("fake-token", DateTimeOffset.UtcNow.AddHours(1));
    }
}
