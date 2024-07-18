using System.ComponentModel.DataAnnotations;

namespace SurfTimer.Validators;

public class IncrementValidator : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        return ValidationResult.Success;
    }
}