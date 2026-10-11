using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.Expiry;
using Imova.Application.Features.Media.Sizes;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Stats;

// The agency's statistics (the Statistici tab, the weekly digest): over Moldovan days From..To
// (default: the last DefaultDays ending today; at most MaxDays), led by contacts. Views and phone
// reveals come from ListingDailyStats, the agency page's reveals from AgencyPhoneReveals, messages
// (conversations started) and favorites from their own tables. The listings counted are the ones
// under the agency now; an Agent gets only their own (and not the agency page's reveals).
// Null (404) when the caller may not see the agency; 403 for a signed-in non-member.
public record GetAgencyStatsQuery(Guid AgencyId, Guid UserId, bool IsAdmin, DateOnly? From = null, DateOnly? To = null)
    : IRequest<AgencyStatsDto?>
{
    public const int DefaultDays = 30;
    public const int MaxDays = 365;
}

public class GetAgencyStatsValidator : AbstractValidator<GetAgencyStatsQuery>
{
    public GetAgencyStatsValidator()
    {
        RuleFor(q => q).Must(q => q.From is null || q.To is null || q.From <= q.To)
            .WithName("From").WithMessage("From must not be after To.");
        RuleFor(q => q).Must(q => q.From is null || q.To is null || q.To.Value.DayNumber - q.From.Value.DayNumber < GetAgencyStatsQuery.MaxDays)
            .WithName("To").WithMessage($"A period is at most {GetAgencyStatsQuery.MaxDays} days.");
    }
}

public class GetAgencyStatsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService, TimeProvider timeProvider)
    : IRequestHandler<GetAgencyStatsQuery, AgencyStatsDto?>
{
    public const int TopListings = 10;
    public const int MaxAttention = 10;

    // "Views but no contacts" only means something past a few views.
    public const int NoContactsAfterViews = 20;

    // The order of the "De îmbunătățit" list: FewPhotos first (it's what stops a listing from being
    // edited or renewed), then NoContacts, then ExpiringSoon. Also the order AttentionReason checks them.
    private static readonly string[] AttentionOrder = ["FewPhotos", "NoContacts", "ExpiringSoon"];

    public async Task<AgencyStatsDto?> Handle(GetAgencyStatsQuery request, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking().Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, request.UserId, request.IsAdmin))
        {
            return null;
        }

        var role = agency.RoleOf(request.UserId);
        if (!request.IsAdmin && role is null)
        {
            throw new ForbiddenAccessException();
        }

        var now = timeProvider.GetUtcNow();
        var to = request.To ?? MoldovaTime.Day(now);
        var from = request.From ?? to.AddDays(-(GetAgencyStatsQuery.DefaultDays - 1));
        var length = to.DayNumber - from.DayNumber + 1;
        var previousTo = from.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(length - 1));

        var agentOnly = !request.IsAdmin && role is AgencyRole.Agent;
        var listingsQuery = dbContext.Listings.AsNoTracking().Where(l => l.AgencyId == agency.Id);
        if (agentOnly)
        {
            var myPublisherIds = dbContext.Publishers.Where(p => p.UserId == request.UserId).Select(p => p.Id);
            listingsQuery = listingsQuery.Where(l => myPublisherIds.Contains(l.PublisherId));
        }

        var listings = await listingsQuery.ToListAsync(cancellationToken);
        var ids = listings.Select(l => l.Id).ToList();

        // Everything from the start of the previous period to the end of this one, then split.
        var start = MoldovaStart(previousFrom);
        var end = MoldovaStart(to.AddDays(1));

        var daily = await dbContext.ListingDailyStats.AsNoTracking()
            .Where(s => ids.Contains(s.ListingId) && s.Day >= previousFrom && s.Day <= to)
            .ToListAsync(cancellationToken);
        var conversations = await dbContext.Conversations.AsNoTracking()
            .Where(c => ids.Contains(c.ListingId) && c.CreatedAt >= start && c.CreatedAt < end)
            .Select(c => new { c.ListingId, c.CreatedAt })
            .ToListAsync(cancellationToken);
        var favorites = await dbContext.Favorites.AsNoTracking()
            .Where(f => ids.Contains(f.ListingId) && f.CreatedAt >= start && f.CreatedAt < end)
            .Select(f => new { f.ListingId, f.CreatedAt })
            .ToListAsync(cancellationToken);
        var agencyReveals = agentOnly
            ? []
            : await dbContext.AgencyPhoneReveals.AsNoTracking()
                .Where(r => r.AgencyId == agency.Id && r.Day >= previousFrom && r.Day <= to)
                .Select(r => r.Day)
                .ToListAsync(cancellationToken);

        var events = new List<StatEvent>();
        events.AddRange(daily.Select(s => new StatEvent(s.ListingId, s.Day, s.Views, s.PhoneReveals, 0, 0, 0)));
        events.AddRange(conversations.Select(c => new StatEvent(c.ListingId, MoldovaTime.Day(c.CreatedAt), 0, 0, 1, 0, 0)));
        events.AddRange(favorites.Select(f => new StatEvent(f.ListingId, MoldovaTime.Day(f.CreatedAt), 0, 0, 0, 1, 0)));
        events.AddRange(agencyReveals.Select(day => new StatEvent(null, day, 0, 0, 0, 0, 1)));

        var current = events.Where(e => e.Day >= from && e.Day <= to).ToList();
        var previous = events.Where(e => e.Day >= previousFrom && e.Day <= previousTo).ToList();

        var series = Enumerable.Range(0, length)
            .Select(i => from.AddDays(i))
            .GroupJoin(current, day => day, e => e.Day, (day, es) =>
            {
                var list = es.ToList();
                return new AgencyStatsDayDto(day, list.Sum(e => e.Views), list.Sum(e => e.Contacts));
            })
            .ToList();

        var perListing = current.Where(e => e.ListingId is not null)
            .GroupBy(e => e.ListingId!.Value)
            .ToDictionary(g => g.Key, g => (Views: g.Sum(e => e.Views), Reveals: g.Sum(e => e.PhoneReveals), Messages: g.Sum(e => e.Messages), Favorites: g.Sum(e => e.Favorites)));

        var photos = await dbContext.Photos.AsNoTracking().Where(p => ids.Contains(p.ListingId)).ToListAsync(cancellationToken);
        var photosByListing = photos.GroupBy(p => p.ListingId).ToDictionary(g => g.Key, g => g.ToList());

        var top = listings
            .Where(l => perListing.ContainsKey(l.Id))
            .Select(l => (Listing: l, Numbers: perListing[l.Id]))
            .OrderByDescending(x => x.Numbers.Reveals + x.Numbers.Messages)
            .ThenByDescending(x => x.Numbers.Views)
            .ThenBy(x => x.Listing.Id)
            .Take(TopListings)
            .Select(x => new AgencyStatsListingDto(
                x.Listing.Id,
                x.Listing.Number,
                x.Listing.Title,
                x.Listing.Status.ToString(),
                Cover(photosByListing.GetValueOrDefault(x.Listing.Id)),
                x.Numbers.Views,
                x.Numbers.Reveals,
                x.Numbers.Messages,
                x.Numbers.Favorites))
            .ToList();

        var attention = listings
            .Where(l => l.Status == ListingStatus.Active)
            .Select(l => (Listing: l, Views: perListing.GetValueOrDefault(l.Id).Views, Reason: AttentionReason(l, perListing.GetValueOrDefault(l.Id), photosByListing.GetValueOrDefault(l.Id)?.Count ?? 0, now)))
            .Where(x => x.Reason is not null)
            .OrderBy(x => Array.IndexOf(AttentionOrder, x.Reason))
            .ThenByDescending(x => x.Views)
            .Take(MaxAttention)
            .Select(x => new AgencyStatsAttentionDto(x.Listing.Id, x.Listing.Number, x.Listing.Title, x.Reason!, x.Views, x.Listing.ExpiresAt))
            .ToList();

        return new AgencyStatsDto(from, to, Totals(current), Totals(previous), series, top, attention);
    }

    // One reason per listing, the first that applies in AttentionOrder.
    private static string? AttentionReason(
        Listing listing, (int Views, int Reveals, int Messages, int Favorites) numbers, int photoCount, DateTimeOffset now)
    {
        if (photoCount < ListingPhotoRules.MinPhotos)
        {
            return "FewPhotos";
        }

        if (numbers.Views >= NoContactsAfterViews && numbers.Reveals + numbers.Messages == 0)
        {
            return "NoContacts";
        }

        return listing.ExpiresAt is { } expiresAt && expiresAt - now <= ListingExpiry.ReminderBefore ? "ExpiringSoon" : null;
    }

    private string? Cover(List<Photo>? photos) =>
        photos is null or { Count: 0 }
            ? null
            : PhotoSizes.Url(photos.OrderByDescending(p => p.IsPrimary).ThenBy(p => p.SortOrder).First(), PhotoSize.Thumbnail, blobStorageService);

    private static AgencyStatsTotalsDto Totals(List<StatEvent> events)
    {
        var reveals = events.Sum(e => e.PhoneReveals);
        var agencyReveals = events.Sum(e => e.AgencyPhoneReveals);
        var messages = events.Sum(e => e.Messages);
        return new AgencyStatsTotalsDto(
            reveals + agencyReveals + messages, events.Sum(e => e.Views), reveals, agencyReveals, messages, events.Sum(e => e.Favorites));
    }

    // The instant a Moldovan day starts, in UTC (Npgsql only writes UTC offsets to timestamptz).
    private static DateTimeOffset MoldovaStart(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, MoldovaTime.Zone.GetUtcOffset(local)).ToUniversalTime();
    }

    private sealed record StatEvent(Guid? ListingId, DateOnly Day, int Views, int PhoneReveals, int Messages, int Favorites, int AgencyPhoneReveals)
    {
        public int Contacts => PhoneReveals + Messages + AgencyPhoneReveals;
    }
}
