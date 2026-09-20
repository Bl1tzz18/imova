using Imova.Domain.Common;

namespace Imova.Domain.Locations;

// Reference data seeded once from CUATM (see CuatmLocationSeeder). RaionId always points at the
// top-level Raion whose flattened locality list this belongs to (every CUATM descendant, at any
// depth, is flattened into its owning Raion's list — see CuatmLocationSeeder's comment).
// ParentLocalityId is the true CUATM immediate-parent chain, kept only for data fidelity/possible
// future deeper-drill-down UI — it's null for localities that sit directly under their Raion
// (depth 1), and set for e.g. a village that's itself part of a commune that's part of a
// Chișinău sector (depth 2-3). Neither the API nor the current frontend selector reads it.
public sealed class Localitate : Entity
{
    private Localitate(
        Guid id,
        Guid sourceId,
        Guid raionId,
        Guid? parentLocalityId,
        string code,
        string nameRo,
        string? nameRu)
        : base(id)
    {
        SourceId = sourceId;
        RaionId = raionId;
        ParentLocalityId = parentLocalityId;
        Code = code;
        NameRo = nameRo;
        NameRu = nameRu;
    }

    public Guid SourceId { get; private set; }

    public Guid RaionId { get; private set; }

    public Guid? ParentLocalityId { get; private set; }

    public string Code { get; private set; }

    public string NameRo { get; private set; }

    public string? NameRu { get; private set; }

    public static Localitate Create(
        Guid sourceId,
        Guid raionId,
        Guid? parentLocalityId,
        string code,
        string nameRo,
        string? nameRu) =>
        new(Guid.NewGuid(), sourceId, raionId, parentLocalityId, code, nameRo, nameRu);
}
