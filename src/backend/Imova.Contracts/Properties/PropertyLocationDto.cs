namespace Imova.Contracts.Properties;

public record PropertyLocationDto(
    string Country,
    string? Region,
    Guid RaionId,
    string RaionName,
    Guid? LocalitateId,
    string? LocalitateName,
    string? Sector,
    string? Street,
    string? BuildingNumber,
    double? Latitude,
    double? Longitude);
