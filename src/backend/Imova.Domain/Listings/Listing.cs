using Imova.Domain.Common;

namespace Imova.Domain.Listings;

// A published offer for a Property — title, price, terms, and its own moderation lifecycle.
// Several Listings can point at the same Property over time (sold then relisted at a new price, a
// separate rental offer, ...), which is why status lives here and not on Property.
//
// PublisherId, not a user id: a listing is published under a Publisher identity (the user's
// Individual one, or their Agency), and ownership checks go through Publisher.UserId.
public sealed class Listing : AggregateRoot
{
    // For EF Core materialization only.
    private Listing()
        : base(Guid.Empty)
    {
        Title = null!;
        Description = null!;
        Price = null!;
    }

    private Listing(
        Guid id,
        Guid propertyId,
        Guid publisherId,
        TransactionType transactionType,
        string title,
        string description,
        Price price,
        SaleDetails? saleDetails,
        RentalDetails? rentalDetails,
        ListingContact? contact)
        : base(id)
    {
        PropertyId = propertyId;
        PublisherId = publisherId;
        TransactionType = transactionType;
        Title = title;
        Description = description;
        Price = price;
        SaleDetails = saleDetails;
        RentalDetails = rentalDetails;
        Contact = contact;
        Status = ListingStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid PropertyId { get; private set; }

    public Guid PublisherId { get; private set; }

    public TransactionType TransactionType { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public Price Price { get; private set; }

    // Only ever set when TransactionType is Sale — see EnsureValidDetails.
    public SaleDetails? SaleDetails { get; private set; }

    // Only ever set when TransactionType is Rent — see EnsureValidDetails.
    public RentalDetails? RentalDetails { get; private set; }

    // Null only for listings created before contact details existed — those are contacted through
    // their publisher's own phone/email (see ListingMapping).
    public ListingContact? Contact { get; private set; }

    public ListingStatus Status { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    // Set by Reject()/cleared by SubmitForReview() — why an admin bounced the listing back.
    // Null unless Status is currently Rejected.
    public string? RejectionReason { get; private set; }

    // Set by Suspend()/cleared by Reinstate() — why an admin took an otherwise-live listing down.
    // Null unless Status is currently Suspended.
    public string? SuspensionReason { get; private set; }

    public static Listing Create(
        Guid propertyId,
        Guid publisherId,
        TransactionType transactionType,
        string title,
        string description,
        Price price,
        SaleDetails? saleDetails = null,
        RentalDetails? rentalDetails = null,
        ListingContact? contact = null,
        // Lets the caller supply the id up front, so a client can start uploading photos under a
        // known listing id before this row exists — see Photo.
        Guid? id = null)
    {
        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("PropertyId is required.", nameof(propertyId));
        }

        if (publisherId == Guid.Empty)
        {
            throw new ArgumentException("PublisherId is required.", nameof(publisherId));
        }

        saleDetails = DefaultSaleDetails(transactionType, saleDetails);
        EnsureValidDetails(transactionType, title, description, price, saleDetails, rentalDetails);
        contact = NormalizedContact(contact);

        return new Listing(
            id ?? Guid.NewGuid(),
            propertyId,
            publisherId,
            transactionType,
            title,
            description,
            price,
            saleDetails,
            rentalDetails,
            contact);
    }

    // Never touches Status/PublisherId/PropertyId — editing an offer's content is not a lifecycle
    // transition (resubmitting a Rejected listing is the caller's separate SubmitForReview() call).
    public void UpdateDetails(
        TransactionType transactionType,
        string title,
        string description,
        Price price,
        SaleDetails? saleDetails,
        RentalDetails? rentalDetails,
        ListingContact? contact = null)
    {
        saleDetails = DefaultSaleDetails(transactionType, saleDetails);
        EnsureValidDetails(transactionType, title, description, price, saleDetails, rentalDetails);
        contact = NormalizedContact(contact);

        TransactionType = transactionType;
        Title = title;
        Description = description;
        Price = price;
        SaleDetails = saleDetails;
        RentalDetails = rentalDetails;
        Contact = contact;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static ListingContact? NormalizedContact(ListingContact? contact)
    {
        if (contact is null)
        {
            return null;
        }

        ListingContact.EnsureValid(contact);
        return contact.Normalized();
    }

    // SaleDetails is only a placeholder today, so a sale listing gets an empty one rather than
    // forcing every caller to construct it.
    private static SaleDetails? DefaultSaleDetails(TransactionType transactionType, SaleDetails? saleDetails) =>
        transactionType == TransactionType.Sale ? saleDetails ?? new SaleDetails() : saleDetails;

    private static void EnsureValidDetails(
        TransactionType transactionType,
        string title,
        string description,
        Price price,
        SaleDetails? saleDetails,
        RentalDetails? rentalDetails)
    {
        if (!Enum.IsDefined(transactionType))
        {
            throw new ArgumentOutOfRangeException(nameof(transactionType), transactionType, "Unknown transaction type.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        ArgumentNullException.ThrowIfNull(price);

        if (transactionType == TransactionType.Rent && rentalDetails is null)
        {
            throw new ArgumentException("A rental listing requires RentalDetails.", nameof(rentalDetails));
        }

        if (transactionType == TransactionType.Rent && saleDetails is not null)
        {
            throw new ArgumentException("A rental listing cannot have SaleDetails.", nameof(saleDetails));
        }

        if (transactionType == TransactionType.Sale && rentalDetails is not null)
        {
            throw new ArgumentException("A sale listing cannot have RentalDetails.", nameof(rentalDetails));
        }

        if (rentalDetails is { MinLeasePeriodMonths: <= 0 })
        {
            throw new ArgumentOutOfRangeException(nameof(rentalDetails), "MinLeasePeriodMonths must be greater than zero.");
        }

        if (rentalDetails is { SecurityDepositAmount: < 0 })
        {
            throw new ArgumentOutOfRangeException(nameof(rentalDetails), "SecurityDepositAmount cannot be negative.");
        }
    }

    // Draft -> PendingReview (first submission), or Rejected -> PendingReview (resubmission after
    // the owner addresses whatever an admin flagged) — deliberately the same method for both, so
    // fixing a rejected listing doesn't force the owner back through Draft first.
    public void SubmitForReview()
    {
        if (Status is not (ListingStatus.Draft or ListingStatus.Rejected))
        {
            throw new InvalidOperationException("Only a draft or rejected listing can be submitted for review.");
        }

        RejectionReason = null;
        Status = ListingStatus.PendingReview;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Admin-only in practice (enforced by ApproveListingHandler, not here) — PendingReview -> Active.
    public void Approve()
    {
        if (Status != ListingStatus.PendingReview)
        {
            throw new InvalidOperationException("Only a listing pending review can be approved.");
        }

        Status = ListingStatus.Active;
        PublishedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != ListingStatus.PendingReview)
        {
            throw new InvalidOperationException("Only a listing pending review can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A rejection reason is required.", nameof(reason));
        }

        Status = ListingStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend(string reason)
    {
        if (Status != ListingStatus.Active)
        {
            throw new InvalidOperationException("Only an active listing can be suspended.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A suspension reason is required.", nameof(reason));
        }

        Status = ListingStatus.Suspended;
        SuspensionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Admin lifting a suspension — Suspended -> Active. Distinct from Publish() (the owner's own
    // Archived/Expired -> Active), since a suspension wasn't the owner's choice to undo.
    public void Reinstate()
    {
        if (Status != ListingStatus.Suspended)
        {
            throw new InvalidOperationException("Only a suspended listing can be reinstated.");
        }

        Status = ListingStatus.Active;
        SuspensionReason = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsRented()
    {
        if (Status != ListingStatus.Active)
        {
            throw new InvalidOperationException("Only an active listing can be marked as rented.");
        }

        if (TransactionType != TransactionType.Rent)
        {
            throw new InvalidOperationException("Only a rental listing can be marked as rented.");
        }

        Status = ListingStatus.Rented;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsSold()
    {
        if (Status != ListingStatus.Active)
        {
            throw new InvalidOperationException("Only an active listing can be marked as sold.");
        }

        if (TransactionType != TransactionType.Sale)
        {
            throw new InvalidOperationException("Only a for-sale listing can be marked as sold.");
        }

        Status = ListingStatus.Sold;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Active -> Expired. Nothing calls this automatically yet (no expiry job) — it exists so the
    // status is reachable and its transitions are defined in one place.
    public void Expire()
    {
        if (Status != ListingStatus.Active)
        {
            throw new InvalidOperationException("Only an active listing can expire.");
        }

        Status = ListingStatus.Expired;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Active/Rented/Sold/Expired -> Archived — anything earlier in the lifecycle never went live,
    // so there's nothing to "take down"; discard a Draft/PendingReview/Rejected listing via delete
    // instead.
    public void Archive()
    {
        if (Status is not (ListingStatus.Active or ListingStatus.Rented or ListingStatus.Sold or ListingStatus.Expired))
        {
            throw new InvalidOperationException("Only an active, rented, sold, or expired listing can be archived.");
        }

        Status = ListingStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // The owner putting a previously deactivated (or expired) listing back up themselves, without
    // going through review again since it was already approved once. Distinct from Approve()
    // (PendingReview -> Active, admin-only). Leaves an already-set PublishedAt untouched.
    public void Publish()
    {
        if (Status is not (ListingStatus.Archived or ListingStatus.Expired))
        {
            throw new InvalidOperationException("Only an archived or expired listing can be published again.");
        }

        Status = ListingStatus.Active;
        PublishedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
