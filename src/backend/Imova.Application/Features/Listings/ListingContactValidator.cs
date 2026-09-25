using FluentValidation;
using Imova.Application.Common.Validation;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings;

// The Contact step of the listing form. What doesn't apply (a Self contact's name/email, apps or
// call hours on a hidden number) is dropped by ListingContact.Normalized() rather than rejected.
public class ListingContactValidator : AbstractValidator<ListingContact>
{
    public ListingContactValidator()
    {
        RuleFor(c => c.PersonType).IsInEnum();

        // Required for Self too — the account's phone may not be the one to publish.
        RuleFor(c => c.Phone).Cascade(CascadeMode.Stop).ValidPhoneNumber();

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Contact name is required when the contact is another person.")
            .When(c => c.PersonType == ContactPersonType.Other);
        RuleFor(c => c.Name).MaximumLength(100);

        // Another person needs some way to be reached: an email becomes required when their phone
        // is missing or invalid (which the Phone rule reports too).
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Contact email is required when the contact is another person without a valid phone number.")
            .When(c => c.PersonType == ContactPersonType.Other && !HasValidPhone(c));
        RuleFor(c => c.Email)
            .EmailAddress()
            .MaximumLength(254)
            .When(c => !string.IsNullOrWhiteSpace(c.Email));

        RuleForEach(c => c.MessagingApps).IsInEnum();

        RuleFor(c => c.PreferredContactMethod).IsInEnum();
        RuleFor(c => c.PreferredContactMethod)
            .Equal(PreferredContactMethod.PlatformMessages)
            .WithMessage("A hidden phone number can only be contacted through platform messages.")
            .When(c => c.HidePhoneNumber);

        // Picked from two dropdowns (ListingContact.CallHourSlots), so anything else is a bad request.
        RuleFor(c => c)
            .Must(c => ListingContact.IsValidCallHourRange(Blank(c.CallHoursFrom), Blank(c.CallHoursTo)))
            .WithMessage("Call hours must be two half-hour times between 06:00 and 23:00, the start before the end.")
            .OverridePropertyName(nameof(ListingContact.CallHoursFrom))
            // Dropped anyway (ListingContact.Normalized) when visitors can't call.
            .When(c => c.AllowsCalls());
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HasValidPhone(ListingContact contact) =>
        new InlineValidator<ListingContact> { v => v.RuleFor(c => c.Phone).Cascade(CascadeMode.Stop).ValidPhoneNumber() }
            .Validate(contact).IsValid;
}
