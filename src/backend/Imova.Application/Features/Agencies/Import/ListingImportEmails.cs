using System.Globalization;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;

namespace Imova.Application.Features.Agencies.Import;

// The email to whoever uploaded an import, once it has finished (or failed as a whole), with the
// counts and a link to its result page. Romanian, in the shared EmailLayout look.
public static class ListingImportEmails
{
    private const string Footer = "Primești acest email pentru că ai încărcat un fișier de import pe IMOVA.";

    public static EmailMessage Finished(string to, string agencyName, ListingImportJob job, string resultUrl)
    {
        var subject = job.Status == ListingImportStatus.Failed
            ? $"Importul pentru {agencyName} nu a putut fi procesat — IMOVA"
            : job.FailedCount > 0
                ? $"Import finalizat pentru {agencyName}: {job.FailedCount} rânduri cu erori — IMOVA"
                : $"Import finalizat pentru {agencyName} — IMOVA";

        var lines = job.Status == ListingImportStatus.Failed
            ? new List<string> { $"Fișierul „{job.FileName}” nu a putut fi procesat. Detaliile sunt pe pagina importului." }
            : new List<string>
            {
                $"Fișierul „{job.FileName}”: {Count(job.TotalRows)} rânduri.",
                $"Anunțuri noi: {Count(job.CreatedCount)}. Actualizate: {Count(job.UpdatedCount)}. Cu erori (neimportate): {Count(job.FailedCount)}.",
            };

        if (job.Status != ListingImportStatus.Failed && job.WarningCount > 0)
        {
            lines.Add($"{Count(job.WarningCount)} rânduri au fost importate cu avertismente.");
        }

        if (job.Status != ListingImportStatus.Failed && job.NotInFileCount > 0)
        {
            lines.Add($"{Count(job.NotInFileCount)} anunțuri active ale agenției nu erau în acest fișier — nu s-a schimbat nimic la ele.");
        }

        var text = string.Join("\n", lines) + $"\n\nVezi rezultatul:\n{resultUrl}\n";
        var content = EmailLayout.Heading(job.Status == ListingImportStatus.Failed ? "Importul nu a putut fi procesat" : "Importul s-a încheiat")
            + string.Concat(lines.Select(line => EmailLayout.Paragraph(EmailLayout.Encode(line))))
            + EmailLayout.Button("Vezi rezultatul", resultUrl);

        return new EmailMessage(to, subject, text, EmailLayout.Page(subject, content, Footer));
    }

    // Plain digits: at most ListingImportJob.MaxRows, and the app runs with invariant globalization
    // (no "ro-RO" culture to format with).
    private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
}
