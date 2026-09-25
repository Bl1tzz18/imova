using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Imova.Application.Common.Interfaces;

namespace Imova.Infrastructure.Storage;

// Azure.Storage.Blobs works identically against Azurite (local/dev, via a connection string
// pointing at its emulated endpoint) and a real Azure Storage account (via the account's real
// connection string) — moving between environments is a configuration change only, not a code
// change. See BlobStorageOptions for what's configurable per environment.
public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly BlobContainerClient _messageAttachmentsClient;
    private readonly Uri? _publicBlobEndpoint;

    public TimeSpan DefaultUploadExpiry { get; }

    public BlobStorageService(BlobStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "Storage:ConnectionString is not configured. Set it in appsettings, via the " +
                "Storage__ConnectionString environment variable, or from a secret store, " +
                "depending on environment.");
        }

        var serviceClient = new BlobServiceClient(options.ConnectionString);
        _containerClient = serviceClient.GetBlobContainerClient(options.ContainerName);

        // Public read access on the container: listing photos are meant to be publicly viewable
        // (rendered directly in <img> tags without minting a read SAS per page view). Writes
        // still always require the Create+Write SAS from GenerateUploadSasUrl — public read
        // never implies public write. In Azure this additionally requires the storage account
        // itself to have "Allow Blob public access" enabled.
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);

        // Message images are private between two people: no public access at all. Reads only go
        // through the API (which checks the requester is a participant or an admin).
        _messageAttachmentsClient = serviceClient.GetBlobContainerClient(options.MessageAttachmentsContainerName);
        _messageAttachmentsClient.CreateIfNotExists(PublicAccessType.None);

        // Uploads PUT straight from the browser to storage via the SAS URL — that's a
        // cross-origin request with custom headers (x-ms-blob-type), so the browser sends a
        // CORS preflight (OPTIONS) first. Without a CORS rule on the storage account itself,
        // Azurite/Azure reject that preflight and the upload never happens (a 403 on the
        // OPTIONS request, never even reaching PUT). Read-then-merge so this doesn't clobber any
        // other service properties (Logging, Metrics, retention policy, ...) someone configured
        // separately, especially on a real Azure account.
        if (!string.IsNullOrWhiteSpace(options.AllowedOrigin))
        {
            var properties = serviceClient.GetProperties().Value;
            properties.Cors = new List<BlobCorsRule>
            {
                new()
                {
                    AllowedOrigins = options.AllowedOrigin,
                    AllowedMethods = "GET,HEAD,PUT,OPTIONS",
                    AllowedHeaders = "*",
                    ExposedHeaders = "*",
                    MaxAgeInSeconds = 3600,
                },
            };
            serviceClient.SetProperties(properties);
        }

        DefaultUploadExpiry = TimeSpan.FromMinutes(options.UploadSasExpiryMinutes);
        _publicBlobEndpoint = string.IsNullOrWhiteSpace(options.PublicBlobEndpoint)
            ? null
            : new Uri(options.PublicBlobEndpoint, UriKind.Absolute);
    }

    public string GenerateBlobName(Guid listingId, string fileExtension) =>
        $"{listingId}/{Guid.NewGuid()}{fileExtension}";

    public string GenerateProfilePictureBlobName(Guid userId, string fileExtension) =>
        $"profile-pictures/{userId}/{Guid.NewGuid()}{fileExtension}";

    public string GenerateMessageAttachmentBlobName(Guid senderUserId, string fileExtension) =>
        $"messages/{senderUserId}/{Guid.NewGuid()}{fileExtension}";

    public string GenerateUploadSasUrl(string blobName, TimeSpan expiry) =>
        GenerateUploadSasUrl(_containerClient, blobName, expiry);

    public string GenerateMessageAttachmentUploadSasUrl(string blobName, TimeSpan expiry) =>
        GenerateUploadSasUrl(_messageAttachmentsClient, blobName, expiry);

    public Task<UploadedBlobInfo?> TryGetUploadedBlobInfoAsync(string blobName, CancellationToken cancellationToken) =>
        TryGetUploadedBlobInfoAsync(_containerClient, blobName, cancellationToken);

    public Task<UploadedBlobInfo?> TryGetMessageAttachmentInfoAsync(string blobName, CancellationToken cancellationToken) =>
        TryGetUploadedBlobInfoAsync(_messageAttachmentsClient, blobName, cancellationToken);

    public async Task<Stream?> OpenMessageAttachmentAsync(string blobName, CancellationToken cancellationToken)
    {
        var blobClient = _messageAttachmentsClient.GetBlobClient(blobName);
        try
        {
            var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return download.Value.Content;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private string GenerateUploadSasUrl(BlobContainerClient container, string blobName, TimeSpan expiry)
    {
        var blobClient = container.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "The configured storage credential cannot generate SAS URIs. Use a connection " +
                "string that includes an account key (Azurite's well-known key locally, or a " +
                "storage account connection string/key in Azure).");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        return RewriteHostIfConfigured(blobClient.GenerateSasUri(sasBuilder));
    }

    public string GetPublicUrl(string blobName) =>
        RewriteHostIfConfigured(_containerClient.GetBlobClient(blobName).Uri);

    public string? TryGetBlobNameFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        // Can't just prefix-match "/{containerName}/" — Azurite's URL shape is
        // "/{account}/{container}/{blob}" (the account is a path segment), while real Azure
        // Storage's is "https://{account}.blob.core.windows.net/{container}/{blob}" (the account
        // is a subdomain, not in the path). _containerClient.Uri.AbsolutePath already reflects
        // whichever shape is actually in play, so deriving the prefix from it instead of
        // hardcoding it works for both.
        var containerPrefix = _containerClient.Uri.AbsolutePath.TrimEnd('/') + "/";
        if (!uri.AbsolutePath.StartsWith(containerPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        return Uri.UnescapeDataString(uri.AbsolutePath[containerPrefix.Length..]);
    }

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        await _containerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private static async Task<UploadedBlobInfo?> TryGetUploadedBlobInfoAsync(
        BlobContainerClient container, string blobName, CancellationToken cancellationToken)
    {
        var blobClient = container.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

        var headerLength = (int)Math.Min(16, properties.Value.ContentLength);
        var header = new byte[headerLength];

        if (headerLength > 0)
        {
            var download = await blobClient.DownloadStreamingAsync(
                new BlobDownloadOptions { Range = new Azure.HttpRange(0, headerLength) },
                cancellationToken);
            await download.Value.Content.ReadExactlyAsync(header, cancellationToken);
        }

        return new UploadedBlobInfo(properties.Value.ContentLength, properties.Value.ContentType, header);
    }

    // The SAS signature covers the account/container/blob path, not the scheme+host, so
    // swapping just those two (query string, including "sig", untouched) keeps it valid — this
    // is what lets a single generated URI be handed out under a different, browser-reachable
    // host. See BlobStorageOptions.PublicBlobEndpoint.
    private string RewriteHostIfConfigured(Uri uri)
    {
        if (_publicBlobEndpoint is null)
        {
            return uri.ToString();
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = _publicBlobEndpoint.Scheme,
            Host = _publicBlobEndpoint.Host,
            Port = _publicBlobEndpoint.Port,
        };

        return builder.Uri.ToString();
    }
}
