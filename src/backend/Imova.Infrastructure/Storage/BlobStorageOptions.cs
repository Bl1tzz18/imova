namespace Imova.Infrastructure.Storage;

// Bound from the "Storage" configuration section (appsettings, environment variables, Docker
// Compose env, or eventually a secret store) — see BlobStorageService for how each is used, and
// appsettings.Development.json / docker-compose.yml for the local Azurite values.
public class BlobStorageOptions
{
    public const string SectionName = "Storage";

    // Full Azure Blob Storage connection string. Local/dev default points at Azurite (see
    // appsettings.Development.json); in Azure this becomes the storage account's connection
    // string (ideally sourced from Key Vault / App Service configuration, not committed).
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "listing-images";

    // Private (no public read) — message images are only served through the API's access check.
    public string MessageAttachmentsContainerName { get; set; } = "message-attachments";

    public int UploadSasExpiryMinutes { get; set; } = 15;

    // Optional override for the scheme+host used when building URLs handed to the browser
    // (SAS upload URLs and public blob URLs). Needed only when the host the *backend* uses to
    // reach storage differs from the host a *browser* can reach it at — e.g. in docker-compose
    // the backend talks to Azurite via the "azurite" service name, but the browser on the host
    // machine needs "localhost". Leave unset when the two coincide (local non-Docker dev, and
    // real Azure Storage in every deployed environment) — no rewriting happens.
    public string? PublicBlobEndpoint { get; set; }

    // Origin(s) allowed to upload directly to storage from the browser (comma-separated for
    // more than one). Uploads go straight from the browser to Blob Storage via the SAS URL, so
    // the *storage account's* CORS rules govern them, not the API's own CORS policy — see
    // BlobStorageService. Defaults to match the API's existing "Frontend" CORS policy
    // (Program.cs); override per environment when the frontend's real origin differs.
    public string AllowedOrigin { get; set; } = "http://localhost:3000";
}
