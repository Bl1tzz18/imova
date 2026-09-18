using System.Text.RegularExpressions;
using FluentValidation;

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
            .WithMessage("Phone number must be a valid phone number, e.g. +373 69 123 456.");

    private static bool BeAValidPhoneNumber(string phoneNumber)
    {
        if (!Regex.IsMatch(phoneNumber, @"^\+?[\d\s\-()]+$"))
        {
            return false;
        }

        var digitCount = phoneNumber.Count(char.IsDigit);
        return digitCount is >= 7 and <= 15;
    }
}
