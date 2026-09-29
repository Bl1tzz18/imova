using Imova.Contracts.Account;
using MediatR;

namespace Imova.Application.Features.Account.ExportPersonalData;

// Everything stored about the signed-in user (the right of access and to data portability — GDPR
// art. 15 and 20, Moldova's Law 133/2011): the data itself plus the list of files to put next to
// it. PersonalDataArchive turns that into the ZIP the user downloads.
public record ExportPersonalDataQuery(Guid UserId) : IRequest<PersonalDataExport>;

public enum ExportFileSource
{
    // The public container: listing photos, profile pictures.
    Public = 1,

    // The private message-attachments container.
    MessageAttachment = 2,
}

// Path: where the file goes inside the ZIP (what the data's *File fields refer to).
public record ExportFile(string Path, ExportFileSource Source, string BlobName);

public record PersonalDataExport(PersonalDataExportDto Data, IReadOnlyList<ExportFile> Files);
