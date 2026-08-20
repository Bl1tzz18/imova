namespace Imova.Contracts.Properties;

public record PropertyLocationDto(
    string Country,
    string? Region,
    string City,
    string? District,
    string? Sector,
    string? Street,
    string? BuildingNumber,
    double Latitude,
    double Longitude);
