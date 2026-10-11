using Imova.Domain.Locations;
using Imova.Infrastructure;

namespace Imova.UnitTests.TestSupport;

// The handful of places the import tests (and the templates' example rows) need: Chișinău with three
// sectors and the suburb Durlești, Ialoveni, Bălți, and Orhei with two villages called Pohrebeni
// (neither top-level, so they can't be told apart) plus a town and village both called Codru in
// Chișinău (the town wins).
internal sealed class ImportTestPlaces
{
    public ImportTestPlaces()
    {
        Chisinau = Raion.Create(Guid.NewGuid(), "0100", "Chișinău", "Кишинёв", LocalityLabel.Sector);
        Ialoveni = Raion.Create(Guid.NewGuid(), "5500", "Ialoveni", null, LocalityLabel.Localitate);
        Balti = Raion.Create(Guid.NewGuid(), "0300", "Bălți", null, LocalityLabel.Localitate);
        Orhei = Raion.Create(Guid.NewGuid(), "6400", "Orhei", null, LocalityLabel.Localitate);

        Durlesti = Localitate.Create(Guid.NewGuid(), Chisinau.Id, null, "0101", "Durlești", null);
        CodruTown = Localitate.Create(Guid.NewGuid(), Chisinau.Id, null, "0102", "Codru", null);
        CodruVillage = Localitate.Create(Guid.NewGuid(), Chisinau.Id, CodruTown.Id, "0103", "Codru", null);
        IaloveniTown = Localitate.Create(Guid.NewGuid(), Ialoveni.Id, null, "5501", "Ialoveni", null);
        var commune = Localitate.Create(Guid.NewGuid(), Orhei.Id, null, "6401", "Ivancea", null);
        Pohrebeni1 = Localitate.Create(Guid.NewGuid(), Orhei.Id, commune.Id, "6402", "Pohrebeni", null);
        Pohrebeni2 = Localitate.Create(Guid.NewGuid(), Orhei.Id, commune.Id, "6403", "Pohrebeni", null);
        Localitati = [Durlesti, CodruTown, CodruVillage, IaloveniTown, commune, Pohrebeni1, Pohrebeni2];

        Botanica = ChisinauSector.Create("Botanica");
        Centru = ChisinauSector.Create("Centru");
        Rascani = ChisinauSector.Create("Râșcani");
    }

    public Raion Chisinau { get; }

    public Raion Ialoveni { get; }

    public Raion Balti { get; }

    public Raion Orhei { get; }

    public Localitate Durlesti { get; }

    public Localitate CodruTown { get; }

    public Localitate CodruVillage { get; }

    public Localitate IaloveniTown { get; }

    public Localitate Pohrebeni1 { get; }

    public Localitate Pohrebeni2 { get; }

    public IReadOnlyList<Localitate> Localitati { get; }

    public ChisinauSector Botanica { get; }

    public ChisinauSector Centru { get; }

    public ChisinauSector Rascani { get; }

    public IReadOnlyList<Raion> Raioane => [Chisinau, Ialoveni, Balti, Orhei];

    public IReadOnlyList<ChisinauSector> Sectors => [Botanica, Centru, Rascani];

    public void AddTo(ImovaDbContext db)
    {
        db.Raioane.AddRange(Raioane);
        db.Localitati.AddRange(Localitati);
        db.ChisinauSectors.AddRange(Sectors);
    }
}
