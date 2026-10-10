using App.DTOs;
using FluentValidation;

namespace App.Validator;


public class UpdateUserPasswordDtoValidator : AbstractValidator<UserPasswordUpdateDto>
{
    public UpdateUserPasswordDtoValidator()
    {
        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("The password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("The password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("The password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("The password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("The password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm the password.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}