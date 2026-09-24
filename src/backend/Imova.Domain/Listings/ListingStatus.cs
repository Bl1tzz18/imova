namespace Imova.Domain.Listings;

// Values 1–8 deliberately line up with the old PropertyStatus (Published -> Active) so migrated
// rows keep their integer value; Expired is new.
public enum ListingStatus
{
    Draft = 1,
    PendingReview = 2,
    Active = 3,
    Rejected = 4,
    Suspended = 5,
    Rented = 6,
    Sold = 7,
    Archived = 8,
    Expired = 9,
}
