using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Import;

public sealed record ImportTemplateFile(string FileName, string ContentType, byte[] Content);

// GET /api/v1/agencies/{id}/import/template?propertyType= — site admins only (ImportAccess), also before
// the agency is verified so a file can be prepared. Null → 404.
public sealed record GetImportTemplateQuery(Guid AgencyId, Guid UserId, bool IsAdmin, PropertyType PropertyType)
    : IRequest<ImportTemplateFile?>;

public class GetImportTemplateHandler(IApplicationDbContext dbContext) : IRequestHandler<GetImportTemplateQuery, ImportTemplateFile?>
{
    public async Task<ImportTemplateFile?> Handle(GetImportTemplateQuery request, CancellationToken cancellationToken)
    {
        if (!await ImportAccess.CanPrepareAsync(dbContext, request.AgencyId, request.IsAdmin, cancellationToken))
        {
            return null;
        }

        return new ImportTemplateFile(
            ImportTemplate.FileName(request.PropertyType), ImportTemplate.ContentType, ImportTemplate.Build(request.PropertyType));
    }
}

// GET /api/v1/agencies/{id}/import/columns?propertyType= — the same columns described for the import
// page's guide, with the amenity and proximity keys that apply to the type. Same access as the template.
public sealed record GetImportColumnsQuery(Guid AgencyId, Guid UserId, bool IsAdmin, PropertyType PropertyType)
    : IRequest<IReadOnlyList<ImportColumnDto>?>;

public class GetImportColumnsHandler(IApplicationDbContext dbContext) : IRequestHandler<GetImportColumnsQuery, IReadOnlyList<ImportColumnDto>?>
{
    public async Task<IReadOnlyList<ImportColumnDto>?> Handle(GetImportColumnsQuery request, CancellationToken cancellationToken)
    {
        if (!await ImportAccess.CanPrepareAsync(dbContext, request.AgencyId, request.IsAdmin, cancellationToken))
        {
            return null;
        }

        // Few rows (tens) — filtered in memory: ApplicablePropertyTypes is an int[] column.
        var amenities = (await dbContext.Amenities.AsNoTracking().ToListAsync(cancellationToken))
            .Where(a => a.AppliesTo(request.PropertyType)).Select(a => a.Key).Order(StringComparer.Ordinal).ToList();
        var proximities = (await dbContext.Proximities.AsNoTracking().ToListAsync(cancellationToken))
            .Where(p => p.AppliesTo(request.PropertyType)).Select(p => p.Key).Order(StringComparer.Ordinal).ToList();
        var example = ImportExamples.For(request.PropertyType);

        return ImportColumns.For(request.PropertyType)
            .Select(c => new ImportColumnDto(
                c.Name,
                c.Kind.ToString(),
                c.Requirement.ToString(),
                c.Scope.ToString(),
                c.Name switch
                {
                    ImportColumns.Amenities => amenities,
                    ImportColumns.Proximities => proximities,
                    _ => c.Choices,
                },
                c.MaxLength,
                example.GetValueOrDefault(c.Name)))
            .ToList();
    }
}
