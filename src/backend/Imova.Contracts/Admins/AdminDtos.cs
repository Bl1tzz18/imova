namespace Imova.Contracts.Admins;

// GrantedAt/GrantedBy: from the audit trail — null for an admin made before it existed (by SQL).
public record AdminUserDto(Guid Id, string Email, string? DisplayName, DateTimeOffset? GrantedAt, string? GrantedBy);

// What's waiting for an admin, per admin area (GET /api/v1/admin/summary).
public record AdminSummaryDto(int PendingReviewListings, int ReportedListings, int OpenConversationReports, int FlaggedMessages);
