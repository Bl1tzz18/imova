using System.Text.RegularExpressions;
using FluentValidation;
using Imova.Application.Common;

namespace Imova.Application.Common.Validation;

public static class PhoneNumberRules
{
    // Not Moldova-specific yet — just enough to reject obvious garbage: an optional leading "+"
    // followed by digits/spaces/dashes/parentheses, with 7-15 actual digits (E.164's range).
    public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(20)
            .Must(BeAValidPhoneNumber)
            .WithMessage("Phone number must be a valid phone number, e.g. +373 69 123 456.").WithErrorCode(ErrorCodes.PhoneInvalid);

    // A missing (null) phone is NotEmpty's error to report — the rules run on regardless, and the
    // regex used to throw on null, turning a missing phoneNumber on register into a 500.
    private static bool BeAValidPhoneNumber(string? phoneNumber)
    {
        if (phoneNumber is null)
        {
            return true;
        }

        if (!Regex.IsMatch(phoneNumber, @"^\+?[\d\s\-()]+$"))
        {
            return false;
        }

        var digitCount = phoneNumber.Count(char.IsDigit);
        return digitCount is >= 7 and <= 15;
    }
}
