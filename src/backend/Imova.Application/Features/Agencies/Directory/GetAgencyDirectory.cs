using FluentValidation;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Logos;
using Imova.Contracts.Agencies;
using Imova.Contracts.Common;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Directory;

// The public agency directory (/agencies): active agencies only, verified ones first, then the
// ones with the most active listings, then by name. Q matches the name (case-insensitive) or, spelled
// like a slug, the slug — so "agentia" finds "Agenția …" and Cyrillic finds its transliteration.
public record GetAgencyDirectoryQuery(string? Q, Guid? RaionId, bool VerifiedOnly, int Page = 1, int PageSize = GetAgencyDirectoryQuery.DefaultPageSize)
    : IRequest<PagedResult<AgencyCardDto>>
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 50;
    public const int MaxQueryLength = 100;
}

public class GetAgencyDirectoryValidator : AbstractValidator<GetAgencyDirectoryQuery>
{
    public GetAgencyDirectoryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAgencyDirectoryQuery.MaxPageSize);
        RuleFor(x => x.Q).MaximumLength(GetAgencyDirectoryQuery.MaxQueryLength);
    }
}

public class GetAgencyDirectoryHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetAgencyDirectoryQuery, PagedResult<AgencyCardDto>>
{
    public async Task<PagedResult<AgencyCardDto>> Handle(GetAgencyDirectoryQuery request, CancellationToken cancellationToken)
    {
        var agencies = dbContext.Agencies.AsNoTracking().Where(a => a.Status == AgencyStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var text = request.Q.Trim().ToLower();
            var key = AgencySlug.SearchKey(request.Q);
            agencies = key.Length > 0
                ? agencies.Where(a => a.Name.ToLower().Contains(text) || a.Slug.Contains(key))
                : agencies.Where(a => a.Name.ToLower().Contains(text));
        }

        if (request.RaionId is { } raionId)
        {
            agencies = agencies.Where(a => a.RaionId == raionId);
        }

        if (request.VerifiedOnly)
        {
            agencies = agencies.Where(a => a.IsVerified);
        }

        var total = await agencies.CountAsync(cancellationToken);

        var rows = await agencies
            .Select(a => new
            {
                a.Id,
                a.Slug,
                a.Name,
                a.LogoBlobName,
                a.IsVerified,
                RaionName = dbContext.Raioane.Where(r => r.Id == a.RaionId).Select(r => r.NameRo).FirstOrDefault(),
                ActiveListings = dbContext.Listings.Count(l => l.AgencyId == a.Id && l.Status == ListingStatus.Active),
            })
            .OrderByDescending(a => a.IsVerified)
            .ThenByDescending(a => a.ActiveListings)
            .ThenBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(a => new AgencyCardDto(
                a.Id,
                a.Slug,
                a.Name,
                a.LogoBlobName is null ? null : blobStorageService.GetPublicUrl(AgencyLogo.ThumbnailBlobName(a.LogoBlobName)),
                a.IsVerified,
                a.RaionName,
                a.ActiveListings))
            .ToList();

        return new PagedResult<AgencyCardDto>(items, request.Page, request.PageSize, total);
    }
}
