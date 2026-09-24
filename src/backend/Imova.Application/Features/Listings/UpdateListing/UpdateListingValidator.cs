using FluentValidation;
using Imova.Application.Common.Interfaces;

namespace Imova.Application.Features.Listings.UpdateListing;

public class UpdateListingValidator : ListingWriteValidator<UpdateListingCommand>
{
    public UpdateListingValidator(IApplicationDbContext dbContext)
        : base(dbContext)
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
