using FluentValidation;
using RYG.Application.DTOs;

namespace RYG.Application.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotEmpty().WithMessage("Equipment ID is required");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Order description is required")
            .MaximumLength(500).WithMessage("Order description cannot exceed 500 characters");

        RuleFor(x => x.ScheduledAt)
            .NotEmpty().WithMessage("Scheduled time is required");
    }
}