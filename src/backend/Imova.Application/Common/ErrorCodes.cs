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
    public const string ListingAgencyLeaveManagerOnly = "listing.agencyLeaveManagerOnly";
    public const string ListingNotEnoughPhotos = "listing.notEnoughPhotos";
    public const string ListingLastPhotos = "listing.lastPhotos";
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
    public const string AgencyConfirmNameMismatch = "agency.confirmNameMismatch";
    public const string AccountLastAgencyOwner = "account.lastAgencyOwner";

    // --- Agency CSV import: the whole file (ListingImportJob.FailureCode, or the upload's 400) ---
    public const string ImportFileUnreadable = "import.fileUnreadable";
    public const string ImportNoRows = "import.noRows";
    public const string ImportTooManyRows = "import.tooManyRows";
    public const string ImportMissingColumns = "import.missingColumns";
    public const string ImportUnknownColumns = "import.unknownColumns";
    public const string ImportDuplicateColumns = "import.duplicateColumns";
    public const string ImportFileRequired = "import.fileRequired";
    public const string ImportFileTooLarge = "import.fileTooLarge";
    public const string ImportAgencyNotVerified = "import.agencyNotVerified";
    public const string ImportAgencyInactive = "import.agencyInactive";
    public const string ImportAlreadyRunning = "import.alreadyRunning";
    public const string ImportFileMissing = "import.fileMissing";
    public const string ImportWorkerFailed = "import.workerFailed";

    // --- Agency CSV import: one row (ListingImportRow.Problems; listing-form codes are reused too) ---
    public const string ImportRequired = "import.required";
    public const string ImportNotANumber = "import.notANumber";
    public const string ImportNotAWholeNumber = "import.notAWholeNumber";
    public const string ImportNotYesNo = "import.notYesNo";
    public const string ImportNotADate = "import.notADate";
    public const string ImportUnknownValue = "import.unknownValue";
    public const string ImportTooLong = "import.tooLong";
    public const string ImportColumnNotForType = "import.columnNotForType";
    public const string ImportRentOnly = "import.rentOnly";
    public const string ImportDuplicateRef = "import.duplicateRef";
    public const string ImportUnknownCity = "import.unknownCity";
    public const string ImportUnknownLocality = "import.unknownLocality";
    public const string ImportAmbiguousLocality = "import.ambiguousLocality";
    public const string ImportUnknownSector = "import.unknownSector";
    public const string ImportSectorOutsideChisinau = "import.sectorOutsideChisinau";
    public const string ImportLocalityAndSector = "import.localityAndSector";
    public const string ImportCoordinatesIncomplete = "import.coordinatesIncomplete";
    public const string ImportTooFewPhotos = "import.tooFewPhotos";
    public const string ImportTooManyPhotos = "import.tooManyPhotos";
    public const string ImportBadPhotoUrl = "import.badPhotoUrl";
    public const string ImportUnknownAmenity = "import.unknownAmenity";
    public const string ImportUnknownProximity = "import.unknownProximity";
    public const string ImportInvalidValue = "import.invalidValue";
    // Something unexpected went wrong saving the row (logged); the other rows carry on.
    public const string ImportRowFailed = "import.rowFailed";
    // Fewer than ListingPhotoRules.MinPhotos of the row's photos could be downloaded and read.
    public const string ImportNotEnoughPhotos = "import.notEnoughPhotos";
    public const string ImportPhotoFailed = "import.photoFailed";

    // Warnings: the row is imported anyway.
    public const string ImportAgentNotMember = "import.agentNotMember";
    public const string ImportCoordinatesIgnored = "import.coordinatesIgnored";
}
