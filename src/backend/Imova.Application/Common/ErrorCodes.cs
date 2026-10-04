namespace Imova.Application.Common;

// Stable, language-neutral codes for the errors a user can actually run into. Every error response
// carries them next to its English message (see the global exception handler in Program.cs:
// `code` on a problem, `errorCodes` on a validation problem), and the web app turns them into
// ro/en/ru text (messages/*.json → "Errors.codes"). The English messages stay for developers and
// logs; the codes are what the UI relies on, so never rename one without updating the web app.
// Validation failures without an explicit code keep FluentValidation's own (NotEmptyValidator, …),
// which the web app translates generically per field.
public static class ErrorCodes
{
    // --- Auth & account ---
    public const string AuthFailed = "auth.failed";
    public const string InvalidCredentials = "auth.invalidCredentials";
    public const string LockedOut = "auth.lockedOut";
    public const string InvalidGoogleToken = "auth.invalidGoogleToken";
    public const string UserNotFound = "auth.userNotFound";
    public const string SessionExpired = "auth.sessionExpired";
    public const string EmailThrottled = "auth.emailThrottled";
    public const string InvalidLink = "link.invalid";
    public const string PhoneInvalid = "phone.invalid";
    public const string PasswordTooShort = "password.tooShort";
    public const string PasswordNoNumber = "password.noNumber";
    public const string PasswordNoSpecial = "password.noSpecial";
    public const string CurrentPasswordRequired = "password.currentRequired";

    // Deleting the account (see AccountDeletion).
    public const string AccountPasswordRequired = "account.passwordRequired";
    public const string AccountWrongPassword = "account.wrongPassword";
    public const string AccountNoPassword = "account.noPassword";

    // Granting the Admin role (see GrantAdmin).
    public const string AdminUserNotFound = "admin.userNotFound";
    public const string AdminEmailNotConfirmed = "admin.emailNotConfirmed";
    public const string AlreadyAdmin = "admin.alreadyAdmin";
    public const string AdminPasswordNeeded = "admin.passwordNeeded";

    // Identity's own errors pass through as "identity.<IdentityError.Code>" (DuplicateEmail, PasswordMismatch, …).
    public const string IdentityPrefix = "identity.";

    // --- General ---
    public const string Forbidden = "forbidden";
    public const string TooManyRequests = "tooManyRequests";
    public const string RateLimited = "rateLimited";

    // --- Listings ---
    public const string EmailNotConfirmed = "listing.emailNotConfirmed";
    public const string InvalidTransition = "listing.invalidTransition";
    public const string StreetRequired = "listing.streetRequired";
    public const string ListingAgencyUnknown = "listing.agencyUnknown";
    public const string ListingNotAgencyMember = "listing.notAgencyMember";
    public const string ListingAgencyInactive = "listing.agencyInactive";
    public const string ListingAgencyChangeAuthorOnly = "listing.agencyChangeAuthorOnly";
    public const string ContactNameRequired = "contact.nameRequired";
    public const string ContactEmailRequired = "contact.emailRequired";
    public const string HiddenPhoneNeedsMessages = "contact.hiddenPhoneNeedsMessages";
    public const string CallHoursInvalid = "contact.callHoursInvalid";

    // --- Uploads (listing photos, message images, profile picture) ---
    public const string UploadNotFound = "upload.notFound";
    public const string UploadTooLarge = "upload.tooLarge";
    public const string UploadNotAnImage = "upload.notAnImage";
    public const string UploadNotYours = "upload.notYours";

    // --- Messaging ---
    public const string MessagingBanned = "messaging.banned";
    public const string BlockedByRecipient = "messaging.blockedByRecipient";
    public const string YouBlocked = "messaging.youBlocked";
    public const string RecipientDeleted = "messaging.recipientDeleted";
    public const string OwnListing = "messaging.ownListing";
    public const string TooManyConversations = "messaging.tooManyConversations";
    public const string MessageEmpty = "message.empty";
    public const string MessageTooLong = "message.tooLong";
    public const string TooManyImages = "message.tooManyImages";
    public const string ReportDetailsRequired = "report.detailsRequired";

    // --- Listing reports (report.detailsRequired above is shared) ---
    public const string ReportOwnListing = "listingReport.ownListing";
    public const string ReportLimitReached = "listingReport.limitReached";
    public const string NoOpenReports = "listingReport.noneOpen";

    // --- Saved searches ---
    public const string SavedSearchLimit = "savedSearch.limitReached";

    // --- Agencies ---
    public const string AgencyWebsiteInvalid = "agency.websiteInvalid";
    public const string AgencyRaionUnknown = "agency.raionUnknown";
    public const string AgencyLogoType = "agency.logoType";
    public const string AgencyLogoTooSmall = "agency.logoTooSmall";
    public const string AgencyLimitReached = "agency.limitReached";
    public const string AgencyEmailNotConfirmed = "agency.emailNotConfirmed";
    public const string AgencyLastOwner = "agency.lastOwner";
    public const string AgencyReassignInvalid = "agency.reassignInvalid";
    public const string AgencyAlreadyMember = "agency.alreadyMember";
    public const string AgencyTooManyInvitations = "agency.tooManyInvitations";
    public const string AgencyInvitationRateLimit = "agency.invitationRateLimit";
    public const string AgencyInvitationResendTooSoon = "agency.invitationResendTooSoon";
    public const string AgencyInvitationExpired = "agency.invitationExpired";
    public const string AgencyInvitationClosed = "agency.invitationClosed";
    public const string AgencyInvitationWrongAccount = "agency.invitationWrongAccount";
    public const string AccountLastAgencyOwner = "account.lastAgencyOwner";
}
