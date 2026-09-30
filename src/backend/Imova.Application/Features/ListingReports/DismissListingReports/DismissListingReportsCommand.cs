using MediatR;

namespace Imova.Application.Features.ListingReports.DismissListingReports;

// Admin-only: closes the open reports on a listing without acting on it (nothing wrong found, or
// the listing is no longer active anyway). The note is for the other admins, never the owner.
public record DismissListingReportsCommand(bool IsAdmin, Guid AdminUserId, Guid ListingId, string? Note) : IRequest;
