using Imova.Domain.Common;

namespace Imova.Domain.Properties;

public sealed class Property : AggregateRoot
{
    private Property(
        Guid id,
        Guid ownerId,
        string title,
        string description,
        PropertyType propertyType,
        ListingType listingType,
        decimal price,
        string currency,
        decimal? area,
        decimal? rooms,
        short? bathrooms,
        short? floor,
        short? totalFloors,
        short? yearBuilt,
        bool? furnished,
        bool? parkingAvailable,
        bool? petsAllowed)
        : base(id)
    {
        OwnerId = ownerId;
        Title = title;
        Description = description;
        PropertyType = propertyType;
        ListingType = listingType;
        Price = price;
        Currency = currency;
        Area = area;
        Rooms = rooms;
        Bathrooms = bathrooms;
        Floor = floor;
        TotalFloors = totalFloors;
        YearBuilt = yearBuilt;
        Furnished = furnished;
        ParkingAvailable = parkingAvailable;
        PetsAllowed = petsAllowed;

        Status = PropertyStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid OwnerId { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public PropertyType PropertyType { get; private set; }

    public ListingType ListingType { get; private set; }

    public PropertyStatus Status { get; private set; }

    public decimal Price { get; private set; }

    public string Currency { get; private set; }

    public decimal? Area { get; private set; }

    public decimal? Rooms { get; private set; }

    public short? Bathrooms { get; private set; }

    public short? Floor { get; private set; }

    public short? TotalFloors { get; private set; }

    public short? YearBuilt { get; private set; }

    public bool? Furnished { get; private set; }

    public bool? ParkingAvailable { get; private set; }

    public bool? PetsAllowed { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public static Property Create(
        Guid ownerId,
        string title,
        string description,
        PropertyType propertyType,
        ListingType listingType,
        decimal price,
        string currency,
        decimal? area = null,
        decimal? rooms = null,
        short? bathrooms = null,
        short? floor = null,
        short? totalFloors = null,
        short? yearBuilt = null,
        bool? furnished = null,
        bool? parkingAvailable = null,
        bool? petsAllowed = null)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("OwnerId is required.", nameof(ownerId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (area is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(area), "Area must be greater than zero.");
        }

        if (rooms is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rooms), "Rooms must be greater than zero.");
        }

        if (bathrooms is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bathrooms), "Bathrooms cannot be negative.");
        }

        if (totalFloors is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalFloors), "TotalFloors must be greater than zero.");
        }

        return new Property(
            Guid.NewGuid(),
            ownerId,
            title,
            description,
            propertyType,
            listingType,
            price,
            currency,
            area,
            rooms,
            bathrooms,
            floor,
            totalFloors,
            yearBuilt,
            furnished,
            parkingAvailable,
            petsAllowed);
    }

    public void UpdateDetails(
        string title,
        string description,
        decimal price)
    {
        Title = title;
        Description = description;
        Price = price;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        if (Status != PropertyStatus.Draft)
        {
            throw new InvalidOperationException("Only draft properties can be published.");
        }

        Status = PropertyStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status = PropertyStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
