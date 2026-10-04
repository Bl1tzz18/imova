using FluentValidation;
using Imova.Application.Common.Interfaces;

namespace Imova.Application.Features.Listings.CreateListing;

public class CreateListingValidator : ListingWriteValidator<CreateListingCommand>
{
    public CreateListingValidator(IApplicationDbContext dbContext)
        : base(dbContext)
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty).When(c => c.Id.HasValue);
        RuleFor(c => c.RequestingUserId).NotEmpty();
        RuleFor(c => c.AgencyId).NotEqual(Guid.Empty).When(c => c.AgencyId.HasValue);
    }
}
