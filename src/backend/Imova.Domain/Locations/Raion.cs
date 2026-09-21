using Imova.Domain.Common;

namespace Imova.Domain.Locations;

// Reference data seeded once from Moldova's official CUATM administrative-division classifier
// (see CuatmLocationSeeder) — a raion, municipiu, or the UTA Găgăuzia/Transnistria top-level
// units. No business invariants beyond "the row exists"; unlike Property/PropertyLocation this
// has no richer factory/validation ceremony since it's never created or edited outside the seeder.
public sealed class Raion : Entity
{
    private Raion(
        Guid id,
        Guid sourceId,
        string code,
        string nameRo,
        string? nameRu,
        LocalityLabel localityLabel)
        : base(id)
    {
        SourceId = sourceId;
        Code = code;
        NameRo = nameRo;
        NameRu = nameRu;
        LocalityLabel = localityLabel;
    }

    // The CUATM record's own id, kept only to make re-running the seeder against a refreshed
    // source dataset idempotent/traceable — never exposed outside the seeder.
    public Guid SourceId { get; private set; }

    public string Code { get; private set; }

    public string NameRo { get; private set; }

    public string? NameRu { get; private set; }

    public LocalityLabel LocalityLabel { get; private set; }

    public static Raion Create(Guid sourceId, string code, string nameRo, string? nameRu, LocalityLabel localityLabel) =>
        new(Guid.NewGuid(), sourceId, code, nameRo, nameRu, localityLabel);
}
