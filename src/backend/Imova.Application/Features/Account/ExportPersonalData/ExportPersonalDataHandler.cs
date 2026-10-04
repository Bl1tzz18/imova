using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Contracts.Account;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Account.ExportPersonalData;

public class ExportPersonalDataHandler(
    IApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IBlobStorageService blobStorageService,
    AppOptions appOptions,
    TimeProvider timeProvider) : IRequestHandler<ExportPersonalDataQuery, PersonalDataExport>
{
    // Bumped whenever the shape of imova-data.json changes in a way a reader would notice.
    public const string Format = "imova.personal-data.v2";

    public async Task<PersonalDataExport> Handle(ExportPersonalDataQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);
        var files = new List<ExportFile>();

        var account = await AccountAsync(user, files);
        var publishers = await dbContext.Publishers.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        var publisherIds = publishers.Select(p => p.Id).ToList();

        var data = new PersonalDataExportDto(
            Format,
            timeProvider.GetUtcNow(),
            account,
            await SessionsAsync(userId, cancellationToken),
            publishers.Select(p => new ExportedPublisherDto(p.Id, p.DisplayName, p.Phone, p.Email, p.CreatedAt)).ToList(),
            await AgenciesAsync(userId, cancellationToken),
            await ListingsAsync(userId, publisherIds, files, cancellationToken),
            await StrayPhotosAsync(userId, files, cancellationToken),
            await FavoritesAsync(userId, cancellationToken),
            await SavedSearchesAsync(userId, cancellationToken),
            await ConversationsAsync(userId, files, cancellationToken),
            await BlocksAsync(userId, cancellationToken),
            await ReportsAsync(userId, cancellationToken),
            await ListingReportsAsync(userId, cancellationToken),
            MissingFiles: []);

        return new PersonalDataExport(data, files);
    }

    private async Task<List<ExportedAgencyMembershipDto>> AgenciesAsync(Guid userId, CancellationToken cancellationToken) =>
        (await (
                from m in dbContext.AgencyMembers.AsNoTracking()
                join a in dbContext.Agencies.AsNoTracking() on m.AgencyId equals a.Id
                where m.UserId == userId
                orderby m.JoinedAt
                select new { a.Id, a.Name, m.Role, m.JoinedAt })
            .ToListAsync(cancellationToken))
        .Select(x => new ExportedAgencyMembershipDto(x.Id, x.Name, x.Role.ToString(), x.JoinedAt))
        .ToList();

    private async Task<ExportedAccountDto> AccountAsync(ApplicationUser user, List<ExportFile> files)
    {
        string? pictureFile = null;
        if (user.ProfilePictureUrl is not null && blobStorageService.TryGetBlobNameFromUrl(user.ProfilePictureUrl) is { } blobName)
        {
            pictureFile = $"profile/picture{Path.GetExtension(blobName)}";
            files.Add(new ExportFile(pictureFile, ExportFileSource.Public, blobName));
        }

        var providers = userManager.SupportsUserLogin
            ? (await userManager.GetLoginsAsync(user)).Select(l => l.ProviderDisplayName ?? l.LoginProvider).ToList()
            : [];

        return new ExportedAccountDto(
            user.Id,
            user.Email!,
            user.EmailConfirmed,
            user.DisplayName,
            user.PhoneNumber,
            pictureFile,
            await userManager.HasPasswordAsync(user),
            providers,
            (await userManager.GetRolesAsync(user)).ToList(),
            user.IsBannedFromMessaging,
            user.EmailFavoriteUpdates);
    }

    // A session is a chain of refresh tokens sharing a SessionId (see RefreshToken) — only its
    // times are exported, never the token hashes.
    private async Task<List<ExportedSessionDto>> SessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens.AsNoTracking().Where(t => t.UserId == userId).ToListAsync(cancellationToken);
        return tokens
            .GroupBy(t => t.SessionId)
            .Select(g => new ExportedSessionDto(
                g.Min(t => t.CreatedAt),
                g.Max(t => t.CreatedAt),
                g.First().Persistent,
                g.First().SessionExpiresAt,
                g.Max(t => t.RevokedAt)))
            .OrderBy(s => s.StartedAt)
            .ToList();
    }

    private async Task<List<ExportedListingDto>> ListingsAsync(
        Guid userId, List<Guid> publisherIds, List<ExportFile> files, CancellationToken cancellationToken)
    {
        var listings = await dbContext.Listings.AsNoTracking()
            .Where(l => publisherIds.Contains(l.PublisherId))
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
        var dtos = await ListingDtoLoader.LoadAsync(
            dbContext, blobStorageService, listings, userId, cancellationToken, includeContactDetails: true);

        var listingIds = listings.Select(l => l.Id).ToList();
        var photosByListing = (await dbContext.Photos.AsNoTracking()
                .Where(p => listingIds.Contains(p.ListingId))
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync(cancellationToken))
            .GroupBy(p => p.ListingId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var priceHistoryByListing = (await dbContext.ListingPriceChanges.AsNoTracking()
                .Where(c => listingIds.Contains(c.ListingId))
                .OrderBy(c => c.ChangedAt)
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.ListingId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ExportedPriceChangeDto>)g
                    .Select(c => new ExportedPriceChangeDto(
                        c.OldAmount, c.OldCurrency.ToString(), c.OldPriceEur, c.NewAmount, c.NewCurrency.ToString(), c.NewPriceEur, c.ChangedAt))
                    .ToList());

        var exported = new List<ExportedListingDto>();
        foreach (var dto in dtos)
        {
            var photoFiles = new List<string>();
            var photos = photosByListing.GetValueOrDefault(dto.Id, []);
            for (var i = 0; i < photos.Count; i++)
            {
                var path = $"listings/{dto.Id}/photo-{i + 1:00}{Path.GetExtension(photos[i].BlobName)}";
                files.Add(new ExportFile(path, ExportFileSource.Public, photos[i].BlobName));
                photoFiles.Add(path);
            }

            exported.Add(new ExportedListingDto(dto, photoFiles, priceHistoryByListing.GetValueOrDefault(dto.Id, [])));
        }

        return exported;
    }

    private async Task<List<ExportedPhotoDto>> StrayPhotosAsync(Guid userId, List<ExportFile> files, CancellationToken cancellationToken)
    {
        var photos = await dbContext.Photos.AsNoTracking()
            .Where(p => p.UploadedByUserId == userId && !dbContext.Listings.Any(l => l.Id == p.ListingId))
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var exported = new List<ExportedPhotoDto>();
        foreach (var photo in photos)
        {
            var path = $"photos-not-in-a-listing/{photo.Id}{Path.GetExtension(photo.BlobName)}";
            files.Add(new ExportFile(path, ExportFileSource.Public, photo.BlobName));
            exported.Add(new ExportedPhotoDto(photo.ListingId, path, photo.CreatedAt));
        }

        return exported;
    }

    private async Task<List<ExportedFavoriteDto>> FavoritesAsync(Guid userId, CancellationToken cancellationToken) =>
        await (from f in dbContext.Favorites.AsNoTracking()
               where f.UserId == userId
               join l in dbContext.Listings.AsNoTracking() on f.ListingId equals l.Id into found
               from l in found.DefaultIfEmpty()
               orderby f.CreatedAt
               select new ExportedFavoriteDto(f.ListingId, l == null ? null : l.Title, f.CreatedAt, f.PriceAlertSentAt, f.EndedAlertSentAt))
            .ToListAsync(cancellationToken);

    private async Task<List<ExportedSavedSearchDto>> SavedSearchesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var searches = await dbContext.SavedSearches.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
        return searches.Select(s => new ExportedSavedSearchDto(
            s.Name,
            appOptions.WebUrl(s.QueryString.Length == 0 ? "/search" : $"/search?{s.QueryString}"),
            s.AlertFrequency.ToString(),
            s.CreatedAt,
            s.LastViewedAt,
            s.LastAlertSentAt)).ToList();
    }

    // Both sides of every conversation the user is in — what they wrote and what they received is
    // all part of their correspondence. The other person appears by display name only.
    private async Task<List<ExportedConversationDto>> ConversationsAsync(
        Guid userId, List<ExportFile> files, CancellationToken cancellationToken)
    {
        var conversations = await dbContext.Conversations.AsNoTracking()
            .Where(c => c.InitiatorUserId == userId || c.PublisherUserId == userId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
        var ids = conversations.Select(c => c.Id).ToList();
        var listingIds = conversations.Select(c => c.ListingId).Distinct().ToList();
        var otherIds = conversations.Select(c => c.OtherParticipant(userId)).Distinct().ToList();

        var titles = await dbContext.Listings.AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Title, cancellationToken);
        var names = await dbContext.Users.AsNoTracking()
            .Where(u => otherIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? string.Empty, cancellationToken);
        var messagesByConversation = (await dbContext.Messages.AsNoTracking()
                .Include(m => m.Attachments)
                .Where(m => ids.Contains(m.ConversationId))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken))
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var exported = new List<ExportedConversationDto>();
        foreach (var conversation in conversations)
        {
            var messages = new List<ExportedMessageDto>();
            foreach (var message in messagesByConversation.GetValueOrDefault(conversation.Id, []))
            {
                var attachmentFiles = new List<string>();
                foreach (var attachment in message.Attachments.OrderBy(a => a.SortOrder))
                {
                    var path = $"messages/{conversation.Id}/{attachment.Id}{Path.GetExtension(attachment.BlobName)}";
                    files.Add(new ExportFile(path, ExportFileSource.MessageAttachment, attachment.BlobName));
                    attachmentFiles.Add(path);
                }

                messages.Add(new ExportedMessageDto(
                    message.SenderUserId == userId, message.Body, message.CreatedAt, message.DeliveredAt, message.ReadAt, attachmentFiles));
            }

            exported.Add(new ExportedConversationDto(
                conversation.Id,
                conversation.ListingId,
                titles.GetValueOrDefault(conversation.ListingId),
                conversation.InitiatorUserId == userId ? "Visitor" : "Publisher",
                names.TryGetValue(conversation.OtherParticipant(userId), out var name) ? name : null,
                conversation.CreatedAt,
                conversation.IsArchivedFor(userId),
                messages));
        }

        return exported;
    }

    private async Task<List<ExportedBlockDto>> BlocksAsync(Guid userId, CancellationToken cancellationToken) =>
        await (from b in dbContext.UserBlocks.AsNoTracking()
               where b.BlockerUserId == userId
               join u in dbContext.Users.AsNoTracking() on b.BlockedUserId equals u.Id into found
               from u in found.DefaultIfEmpty()
               orderby b.CreatedAt
               select new ExportedBlockDto(u == null ? null : u.DisplayName, b.CreatedAt))
            .ToListAsync(cancellationToken);

    private async Task<List<ExportedReportDto>> ReportsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var reports = await dbContext.ConversationReports.AsNoTracking()
            .Where(r => r.ReporterUserId == userId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return reports.Select(r => new ExportedReportDto(r.ConversationId, r.Reason.ToString(), r.Details, r.CreatedAt, r.ResolvedAt))
            .ToList();
    }

    private async Task<List<ExportedListingReportDto>> ListingReportsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var reports = await dbContext.ListingReports.AsNoTracking()
            .Where(r => r.ReporterUserId == userId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return reports
            .Select(r => new ExportedListingReportDto(r.ListingId, r.Reason.ToString(), r.Details, r.CreatedAt, r.ResolvedAt, r.Outcome?.ToString()))
            .ToList();
    }
}
