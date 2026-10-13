using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Logos;
using Imova.Contracts.Agencies;
using Imova.Contracts.Common;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Admin;

// The admins' agency list (/admin/agencies): every agency, deactivated ones too — not yet verified
// first, then the newest. Q: the name (case-insensitive) or, spelled like a slug, the slug. Verified:
// only verified (true) or only not yet verified (false). The route is admin-only (RequireAdmin).
public record GetAdminAgenciesQuery(string? Q, bool? Verified, int Page = 1, int PageSize = GetAdminAgenciesQuery.DefaultPageSize)
    : IRequest<PagedResult<AdminAgencyDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
}

public class GetAdminAgenciesValidator : AbstractValidator<GetAdminAgenciesQuery>
{
    public GetAdminAgenciesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminAgenciesQuery.MaxPageSize);
        RuleFor(x => x.Q).MaximumLength(100);
    }
}

public class GetAdminAgenciesHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetAdminAgenciesQuery, PagedResult<AdminAgencyDto>>
{
    public async Task<PagedResult<AdminAgencyDto>> Handle(GetAdminAgenciesQuery request, CancellationToken cancellationToken)
    {
        var agencies = dbContext.Agencies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var text = request.Q.Trim().ToLower();
            var key = AgencySlug.SearchKey(request.Q);
            agencies = key.Length > 0
                ? agencies.Where(a => a.Name.ToLower().Contains(text) || a.Slug.Contains(key))
                : agencies.Where(a => a.Name.ToLower().Contains(text));
        }

        if (request.Verified is { } verified)
        {
            agencies = agencies.Where(a => a.IsVerified == verified);
        }

        var total = await agencies.CountAsync(cancellationToken);
        var rows = await agencies
            .OrderBy(a => a.IsVerified)
            .ThenByDescending(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new
            {
                a.Id,
                a.Slug,
                a.Name,
                a.LogoBlobName,
                a.IsVerified,
                a.VerifiedAt,
                a.Status,
                a.CreatedAt,
                RaionName = dbContext.Raioane.Where(r => r.Id == a.RaionId).Select(r => r.NameRo).FirstOrDefault(),
                MemberCount = dbContext.AgencyMembers.Count(m => m.AgencyId == a.Id),
                ActiveListings = dbContext.Listings.Count(l => l.AgencyId == a.Id && l.Status == ListingStatus.Active),
                OwnerId = dbContext.AgencyMembers
                    .Where(m => m.AgencyId == a.Id && m.Role == AgencyRole.Owner)
                    .OrderBy(m => m.JoinedAt)
                    .Select(m => (Guid?)m.UserId)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var ownerIds = rows.Select(r => r.OwnerId).OfType<Guid>().Distinct().ToList();
        var owners = await dbContext.Users.AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var items = rows
            .Select(r =>
            {
                var owner = r.OwnerId is { } id ? owners.GetValueOrDefault(id) : null;
                return new AdminAgencyDto(
                    r.Id,
                    r.Slug,
                    r.Name,
                    r.LogoBlobName is null ? null : blobStorageService.GetPublicUrl(AgencyLogo.ThumbnailBlobName(r.LogoBlobName)),
                    r.IsVerified,
                    r.VerifiedAt,
                    r.Status.ToString(),
                    r.RaionName,
                    r.MemberCount,
                    r.ActiveListings,
                    r.CreatedAt,
                    owner is null ? null : string.IsNullOrWhiteSpace(owner.DisplayName) ? owner.Email : owner.DisplayName,
                    owner?.Email);
            })
            .ToList();

        return new PagedResult<AdminAgencyDto>(items, request.Page, request.PageSize, total);
    }
}

// Verify or unverify an agency (site admins only — the route checks). Verifying emails its Owners
// (confirmed addresses, best effort). False when there's no such agency (404).
public record SetAgencyVerifiedCommand(Guid AgencyId, Guid AdminUserId, bool Verified) : IRequest<bool>;

public class SetAgencyVerifiedHandler(
    IApplicationDbContext dbContext,
    AgencyVerifiedEmail email,
    TimeProvider timeProvider,
    ILogger<SetAgencyVerifiedHandler> logger) : IRequestHandler<SetAgencyVerifiedCommand, bool>
{
    public async Task<bool> Handle(SetAgencyVerifiedCommand request, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == request.AgencyId, cancellationToken);
        if (agency is null)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        var changed = request.Verified ? agency.Verify(now) : agency.Unverify(now);
        if (!changed)
        {
            return true;
        }

        // Who verified (or unverified) which agency, and when — in the same save as the change.
        dbContext.AdminAuditEntries.Add(new AdminAuditEntry
        {
            Id = Guid.NewGuid(),
            Action = request.Verified ? AdminAuditEntry.VerifyAgency : AdminAuditEntry.UnverifyAgency,
            ActorUserId = request.AdminUserId,
            TargetAgencyId = agency.Id,
            CreatedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Agency {AgencyId} {Action} by admin {AdminUserId}.", agency.Id, request.Verified ? "verified" : "unverified", request.AdminUserId);

        if (request.Verified)
        {
            var ownerIds = agency.Members.Where(m => m.Role == AgencyRole.Owner).Select(m => m.UserId).ToList();
            var owners = await dbContext.Users.AsNoTracking()
                .Where(u => ownerIds.Contains(u.Id) && u.EmailConfirmed && u.Email != null)
                .Select(u => new { Email = u.Email!, u.PreferredLanguage })
                .ToListAsync(cancellationToken);
            foreach (var owner in owners)
            {
                try
                {
                    await email.SendAsync(owner.Email, agency.Name, agency.Slug, owner.PreferredLanguage, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Couldn't send the verification email for agency {AgencyId}.", agency.Id);
                }
            }
        }

        return true;
    }
}

// "Agenția {Agency} a fost verificată" (HTML + text, in the owner's language), with a link to its page.
public class AgencyVerifiedEmail(IEmailSender emailSender, AppOptions appOptions)
{
    public Task SendAsync(string to, string agencyName, string slug, string language, CancellationToken cancellationToken) =>
        emailSender.SendAsync(
            Build(to, agencyName, appOptions.WebUrl($"/agencies/{Uri.EscapeDataString(slug)}"), language), cancellationToken);

    public static EmailMessage Build(string to, string agencyName, string link, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var heading = T(
            $"Agenția {agencyName} a fost verificată",
            $"The agency {agencyName} has been verified",
            $"Агентство {agencyName} прошло проверку");
        var body = T(
            "Echipa IMOVA a verificat agenția. De acum, pagina ei și anunțurile ei poartă semnul „Agenție verificată”, iar agenția apare printre primele în lista agențiilor.",
            "The IMOVA team has verified the agency. From now on its page and its listings carry the “Verified agency” mark, and the agency is shown among the first in the list of agencies.",
            "Команда IMOVA проверила агентство. Теперь на его странице и объявлениях стоит знак «Проверенное агентство», а само агентство показывается одним из первых в списке агентств.");
        var page = T("Pagina agenției", "The agency's page", "Страница агентства");
        var footer = T(
            "Ai primit acest email pentru că ești proprietarul agenției pe IMOVA.",
            "You're getting this email because you own the agency on IMOVA.",
            "Вы получили это письмо, потому что вы владелец агентства на IMOVA.");

        var text = $"{heading}.\n\n{body}\n\n{page}: {link}\n";
        var html = EmailLayout.Page(
            heading,
            EmailLayout.Heading(heading)
            + EmailLayout.Paragraph(EmailLayout.Encode(body))
            + EmailLayout.Button(T("Vezi pagina agenției", "View agency page", "Открыть страницу агентства"), link),
            EmailLayout.Encode(footer),
            language);
        return new EmailMessage(to, $"{heading} — IMOVA", text, html);
    }
}
