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

    // Set by Reject()/cleared by SubmitForReview() — why an admin bounced the listing back to
    // the owner. Null unless Status is currently Rejected.
    public string? RejectionReason { get; private set; }

    // Set by Suspend()/cleared by Reinstate() — why an admin took an otherwise-live listing down.
    // Null unless Status is currently Suspended.
    public string? SuspensionReason { get; private set; }

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
        bool? petsAllowed = null,
        // Lets a caller (CreatePropertyCommand.Id) supply the id up front, so a client can start
        // uploading listing photos under a known property id before this row exists — see the
        // comment on PropertyMedia. Defaults to a fresh id when omitted, same as before.
        Guid? id = null)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("OwnerId is required.", nameof(ownerId));
        }

        EnsureValidDetails(title, description, price, currency, area, rooms, bathrooms, totalFloors);

        return new Property(
            id ?? Guid.NewGuid(),
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

    // Shared by Create and UpdateDetails so both apply the exact same invariants — a listing
    // can't be edited into a state that couldn't have been created in the first place.
    private static void EnsureValidDetails(
        string title,
        string description,
        decimal price,
        string currency,
        decimal? area,
        decimal? rooms,
        short? bathrooms,
        short? totalFloors)
    {
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
    }

    // Covers every field Create accepts (bar OwnerId/Currency-immutable concerns — there are
    // none) so the owner can edit a listing through the exact same set of fields they created it
    // with, via the same multi-step form on the frontend (see PropertyForm.tsx). Status/OwnerId
    // are untouched — this never changes the lifecycle state.
    public void UpdateDetails(
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
    {
        EnsureValidDetails(title, description, price, currency, area, rooms, bathrooms, totalFloors);

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
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Draft -> PendingReview (first submission), or Rejected -> PendingReview (resubmission after
    // the owner addresses whatever an admin flagged) — deliberately the same method for both, so
    // fixing a rejected listing doesn't force the owner back through Draft first.
    public void SubmitForReview()
    {
        if (Status is not (PropertyStatus.Draft or PropertyStatus.Rejected))
        {
            throw new InvalidOperationException("Only a draft or rejected listing can be submitted for review.");
        }

        RejectionReason = null;
        Status = PropertyStatus.PendingReview;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Admin-only in practice (enforced by ApproveListingHandler, not here) — PendingReview -> Published.
    public void Approve()
    {
        if (Status != PropertyStatus.PendingReview)
        {
            throw new InvalidOperationException("Only a listing pending review can be approved.");
        }

        Status = PropertyStatus.Published;
        PublishedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != PropertyStatus.PendingReview)
        {
            throw new InvalidOperationException("Only a listing pending review can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A rejection reason is required.", nameof(reason));
        }

        Status = PropertyStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend(string reason)
    {
        if (Status != PropertyStatus.Published)
        {
            throw new InvalidOperationException("Only a published listing can be suspended.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A suspension reason is required.", nameof(reason));
        }

        Status = PropertyStatus.Suspended;
        SuspensionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Admin lifting a suspension — Suspended -> Published. Distinct from Republish() (the owner's
    // own Archived -> Published), since a suspension wasn't the owner's choice to undo.
    public void Reinstate()
    {
        if (Status != PropertyStatus.Suspended)
        {
            throw new InvalidOperationException("Only a suspended listing can be reinstated.");
        }

        Status = PropertyStatus.Published;
        SuspensionReason = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsRented()
    {
        if (Status != PropertyStatus.Published)
        {
            throw new InvalidOperationException("Only a published listing can be marked as rented.");
        }

        if (ListingType != ListingType.Rent)
        {
            throw new InvalidOperationException("Only a rental listing can be marked as rented.");
        }

        Status = PropertyStatus.Rented;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsSold()
    {
        if (Status != PropertyStatus.Published)
        {
            throw new InvalidOperationException("Only a published listing can be marked as sold.");
        }

        if (ListingType != ListingType.Sale)
        {
            throw new InvalidOperationException("Only a for-sale listing can be marked as sold.");
        }

        Status = PropertyStatus.Sold;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Published/Rented/Sold -> Archived — anything earlier in the lifecycle never went live, so
    // there's nothing to "take down"; discard a Draft/PendingReview/Rejected listing via delete
    // instead.
    public void Archive()
    {
        if (Status is not (PropertyStatus.Published or PropertyStatus.Rented or PropertyStatus.Sold))
        {
            throw new InvalidOperationException("Only a published, rented, or sold listing can be archived.");
        }

        Status = PropertyStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Distinct from Approve() (PendingReview -> Published, requires admin review) — this is the
    // owner putting a previously deactivated listing back up themselves, without going through
    // review again since it was already approved once. Only accepts the Archived -> Published
    // edge and deliberately leaves an already-set PublishedAt untouched.
    public void Republish()
    {
        if (Status != PropertyStatus.Archived)
        {
            throw new InvalidOperationException("Only archived properties can be republished.");
        }

        Status = PropertyStatus.Published;
        PublishedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
