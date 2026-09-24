using FluentValidation;
using FluentValidation.Results;

namespace Imova.Application.Features.Listings;

// Listing's lifecycle methods guard their own legal transitions (throwing
// InvalidOperationException); this turns an illegal one into a 400 with the domain's own message,
// so handlers don't have to duplicate each guard just to produce a friendly error.
public static class ListingTransitions
{
    public static void Apply(Action transition)
    {
        try
        {
            transition();
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException([new ValidationFailure("Id", ex.Message)]);
        }
    }
}
