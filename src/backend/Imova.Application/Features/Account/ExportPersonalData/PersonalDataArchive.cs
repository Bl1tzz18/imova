using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Imova.Application.Common.Interfaces;

namespace Imova.Application.Features.Account.ExportPersonalData;

// Writes a PersonalDataExport as the ZIP the user downloads:
//   imova-data.json   — the data (indented, readable; see PersonalDataExportDto)
//   CITESTE-MA.txt    — what the files are, in Romanian and English
//   profile/…, listings/<id>/photo-01.jpg, photos-not-in-a-listing/…, messages/<conversation>/…
// Files first, the JSON last, so a file that has vanished from storage is listed under
// MissingFiles instead of pointing at nothing.
public static class PersonalDataArchive
{
    public const string DataFileName = "imova-data.json";
    public const string ReadmeFileName = "CITESTE-MA.txt";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // A file for people to read: keep "ă", "ț", "„…”" as they are instead of \u escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string FileName(DateTimeOffset generatedAt) => $"imova-date-personale-{generatedAt:yyyy-MM-dd}.zip";

    // destination must accept synchronous writes (a temp file, a MemoryStream) — ZipArchive
    // finishes the archive synchronously when disposed.
    public static async Task WriteAsync(
        PersonalDataExport export, IBlobStorageService blobStorageService, Stream destination, CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);

        foreach (var file in export.Files)
        {
            await using var source = file.Source == ExportFileSource.MessageAttachment
                ? await blobStorageService.OpenMessageAttachmentAsync(file.BlobName, cancellationToken)
                : await blobStorageService.OpenAsync(file.BlobName, cancellationToken);
            if (source is null)
            {
                missing.Add(file.Path);
                continue;
            }

            // Images are already compressed; storing them as-is is as small and much faster.
            await using var target = zip.CreateEntry(file.Path, CompressionLevel.NoCompression).Open();
            await source.CopyToAsync(target, cancellationToken);
        }

        await using (var target = zip.CreateEntry(DataFileName, CompressionLevel.Optimal).Open())
        {
            await JsonSerializer.SerializeAsync(target, export.Data with { MissingFiles = missing }, JsonOptions, cancellationToken);
        }

        await using (var target = zip.CreateEntry(ReadmeFileName, CompressionLevel.Optimal).Open())
        {
            await target.WriteAsync(Encoding.UTF8.GetBytes(Readme(export.Data.GeneratedAt)), cancellationToken);
        }
    }

    private static string Readme(DateTimeOffset generatedAt) =>
        $"""
        IMOVA — datele tale personale / your personal data
        Generat la / Generated at: {generatedAt:yyyy-MM-dd HH:mm} UTC

        RO
        Această arhivă conține toate datele pe care IMOVA le păstrează despre contul tău:
          {DataFileName}  datele contului, sesiunile de autentificare, profilurile de publicare,
                          anunțurile (cu toate detaliile lor și istoricul prețului),
                          favoritele, căutările salvate, conversațiile (mesajele trimise și
                          primite), utilizatorii blocați și sesizările trimise — în format
                          JSON, ce poate fi deschis sau importat în alte aplicații.
          profile/        fotografia de profil
          listings/       fotografiile anunțurilor tale
          photos-not-in-a-listing/  fotografii încărcate pentru anunțuri nepublicate
          messages/       imaginile din conversațiile tale
        Câmpurile care se termină în "File"/"Files" indică fișierul din arhivă. Orele sunt în
        UTC (ISO 8601). Poți corecta sau șterge oricând datele din pagina Contul meu.

        EN
        This archive holds everything IMOVA stores about your account:
          {DataFileName}  account details, sign-in sessions, publisher profiles, listings (with
                          all their details and price history), favorites, saved searches,
                          conversations (messages sent and received), blocked users and reports
                          you filed — as JSON, readable by people and importable by other
                          software.
          profile/        your profile picture
          listings/       your listings' photos
          photos-not-in-a-listing/  photos uploaded for listings that were never created
          messages/       images from your conversations
        Fields ending in "File"/"Files" point at the file in this archive. Times are UTC
        (ISO 8601). You can correct or delete your data at any time from your account page.
        """;
}
