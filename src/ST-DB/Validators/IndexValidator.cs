using System.ComponentModel.DataAnnotations;

namespace SurfTimer.Validators;

public class IndexValidator : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        return ValidationResult.Success;
    }
}