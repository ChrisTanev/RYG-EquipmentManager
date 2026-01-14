using FluentValidation;
using RYG.Application.DTOs;

namespace RYG.Application.Validators;

public class ChangeStateValidator : AbstractValidator<ChangeStateRequest>
{
    public ChangeStateValidator()
    {
        RuleFor(x => x.State)
            .IsInEnum().WithMessage("Invalid equipment state");
    }
}