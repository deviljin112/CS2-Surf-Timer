using System.ComponentModel.DataAnnotations;

namespace SurfTimer.ST_DB.Validators;

public class UniqueValidator : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        return ValidationResult.Success;
    }
}