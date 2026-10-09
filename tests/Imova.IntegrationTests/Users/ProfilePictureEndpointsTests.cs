using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Imova.Contracts.Auth;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Users;

// The profile picture's real size limit is the API's 5 MB — what the account page promises. (The web
// app sends it through a route handler, not a server action, whose body would stop at 1 MB.)
public class ProfilePictureEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const int PhonePhotoBytes = 4_352_835;
    private const int MaxBytes = 5 * 1024 * 1024;

    private readonly WebApplicationFactory<Program> _factory;

    public ProfilePictureEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    // A JPEG as the API sniffs it (its signature), padded to the given size.
    private static MultipartFormDataContent Jpeg(int size)
    {
        var bytes = new byte[size];
        byte[] signature = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00];
        signature.CopyTo(bytes, 0);
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent { { file, "file", "IMG_2041.jpg" } };
    }

    [Fact]
    public async Task APhonePhotoOfFourMegabytes_IsTaken()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);

        var response = await client.PostAsync("/api/v1/users/me/profile-picture", Jpeg(PhonePhotoBytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<UserProfileDto>())!;
        Assert.NotNull(profile.ProfilePictureUrl);
    }

    [Fact]
    public async Task OverFiveMegabytes_IsRefusedWithTheTooLargeCode()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);

        var response = await client.PostAsync("/api/v1/users/me/profile-picture", Jpeg(MaxBytes + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("upload.tooLarge", await response.Content.ReadAsStringAsync());
    }
}
