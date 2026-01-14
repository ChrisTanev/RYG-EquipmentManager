using FluentValidation;
using RYG.Application.DTOs;

namespace RYG.Application.Validators;

public class UpdateEquipmentValidator : AbstractValidator<UpdateEquipmentRequest>
{
    public UpdateEquipmentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Equipment name is required")
            .MaximumLength(100).WithMessage("Equipment name cannot exceed 100 characters");
    }
}