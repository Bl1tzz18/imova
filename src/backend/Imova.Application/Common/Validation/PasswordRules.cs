using FluentValidation;

namespace Imova.Application.Common.Validation;

// The password policy for every password a user chooses (register, change, reset): at least
// MinLength characters, one number, one special character. Identity's own PasswordOptions in
// Program.cs are set to the same rules, and the web app shows them as a live checklist
// (lib/auth/passwordRules.ts) — keep all three in step.
public static class PasswordRules
{
    public const int MinLength = 8;

    public const int MaxLength = 100;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(MinLength)
            .MaximumLength(MaxLength)
            .Must(HasNumber).WithMessage("Password must contain at least one number.")
            .Must(HasSpecialCharacter).WithMessage("Password must contain at least one special character.");

    // Same definitions as Identity's RequireDigit / RequireNonAlphanumeric. Null is NotEmpty's to report.
    public static bool HasNumber(string? password) => password is null || password.Any(char.IsDigit);

    public static bool HasSpecialCharacter(string? password) => password is null || password.Any(c => !char.IsLetterOrDigit(c));
}
