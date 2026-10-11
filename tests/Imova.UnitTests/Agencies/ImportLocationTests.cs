using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Import;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Agencies;

// Matching a row's city / locality / sector, and deciding whether its coordinates are used.
public class ImportLocationTests
{
    private static readonly ImportTestPlaces Places = new();
    private static readonly ImportLocationIndex Index = ImportLocationIndex.From(Places.Raioane, Places.Localitati, Places.Sectors);

    [Theory]
    [InlineData("Chișinău")]
    [InlineData("chisinau")]
    [InlineData("mun. Chișinău")]
    [InlineData("Municipiul Chisinau")]
    public void TheCity_MatchesWhateverWayItsWritten(string city)
    {
        var (location, problems) = Index.Resolve(city, "", "Botanica");

        Assert.Empty(problems);
        Assert.Equal((Places.Chisinau.Id, Places.Botanica.Id), (location!.RaionId, location.ChisinauSectorId));
        Assert.Equal("Chișinău", location.RaionName);
    }

    [Fact]
    public void ALocality_IsLookedUpInItsCity_TheTownWinningOverAVillageOfTheSameName()
    {
        Assert.Equal(Places.Durlesti.Id, Index.Resolve("Chișinău", "or. Durlesti", "").Location!.LocalitateId);
        Assert.Equal(Places.CodruTown.Id, Index.Resolve("Chișinău", "Codru", "").Location!.LocalitateId);
        Assert.Equal(Places.IaloveniTown.Id, Index.Resolve("Ialoveni", "Ialoveni", "").Location!.LocalitateId);

        var rascani = Index.Resolve("Chisinau", "", "rascani").Location!;
        Assert.Equal(("Râșcani", Places.Rascani.Id), (rascani.ChisinauSectorName, rascani.ChisinauSectorId));
    }

    [Theory]
    [InlineData("Atlantida", "", "", "city", ErrorCodes.ImportUnknownCity)]
    [InlineData("Ialoveni", "Durlești", "", "locality", ErrorCodes.ImportUnknownLocality)]
    [InlineData("Orhei", "Pohrebeni", "", "locality", ErrorCodes.ImportAmbiguousLocality)]
    [InlineData("Bălți", "", "Botanica", "sector", ErrorCodes.ImportSectorOutsideChisinau)]
    [InlineData("Chișinău", "", "Manhattan", "sector", ErrorCodes.ImportUnknownSector)]
    [InlineData("Chișinău", "Durlești", "Botanica", "sector", ErrorCodes.ImportLocalityAndSector)]
    public void WhatCantBeMatched_IsReportedOnItsColumn(string city, string locality, string sector, string column, string code)
    {
        var (location, problems) = Index.Resolve(city, locality, sector);

        Assert.Null(location);
        var problem = Assert.Single(problems);
        Assert.Equal((column, code), (problem.Column, problem.Code));
    }

    [Fact]
    public void Coordinates_AreUsed_OnlyInsideMoldova_AndNearThePlace()
    {
        var botanica = new GeocodingResult(46.9908, 28.8586, "Botanica, Chișinău");

        Assert.Equal(ImportCoordinatesDecision.Use, ImportCoordinates.Decide(46.9850, 28.8700, botanica));
        // Bălți is ~110 km away.
        Assert.Equal(ImportCoordinatesDecision.TooFar, ImportCoordinates.Decide(47.7617, 27.9289, botanica));
        // Latitude and longitude swapped.
        Assert.Equal(ImportCoordinatesDecision.OutsideMoldova, ImportCoordinates.Decide(28.8586, 46.9908, botanica));
        Assert.Equal(ImportCoordinatesDecision.Unverifiable, ImportCoordinates.Decide(46.9850, 28.8700, null));
        Assert.InRange(ImportCoordinates.DistanceKm(47.0105, 28.8638, 47.7617, 27.9289), 105, 115);
    }

    [Fact]
    public async Task TheGeolocator_ChecksEachPlaceOnce_AndLooksTheAddressUpWhenItIgnoresCoordinates()
    {
        var geocoder = new CountingGeocoder(new GeocodingResult(46.9908, 28.8586, "Botanica"));
        var geolocator = new ImportGeolocator(geocoder);
        var botanica = Index.Resolve("Chișinău", "", "Botanica").Location!;

        var near = await geolocator.LocateAsync(botanica, "bd. Dacia", "27", 46.9850, 28.8700, CancellationToken.None);
        var nearAgain = await geolocator.LocateAsync(botanica, "bd. Dacia", "29", 46.9851, 28.8701, CancellationToken.None);
        Assert.Equal((46.9850, 28.8700), (near.Latitude!.Value, near.Longitude!.Value));
        Assert.Null(near.Warning);
        Assert.Null(nearAgain.Warning);
        Assert.Equal(1, geocoder.Calls);

        var far = await geolocator.LocateAsync(botanica, "bd. Dacia", "27", 47.7617, 27.9289, CancellationToken.None);
        Assert.Equal(ErrorCodes.ImportCoordinatesIgnored, far.Warning!.Code);
        Assert.Equal("TooFar", far.Warning.Params!["reason"]);
        Assert.Equal((46.9908, 28.8586), (far.Latitude!.Value, far.Longitude!.Value));
        Assert.Contains("bd. Dacia", geocoder.LastAddress);

        var none = await geolocator.LocateAsync(botanica, "bd. Dacia", "27", null, null, CancellationToken.None);
        Assert.Null(none.Warning);
        Assert.Equal(3, geocoder.Calls);
    }

    private sealed class CountingGeocoder(GeocodingResult? result) : IGeocodingService
    {
        public int Calls { get; private set; }

        public string LastAddress { get; private set; } = string.Empty;

        public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken)
        {
            Calls++;
            LastAddress = address;
            return Task.FromResult(result);
        }
    }
}
