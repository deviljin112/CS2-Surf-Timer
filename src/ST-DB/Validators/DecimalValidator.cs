using System.ComponentModel.DataAnnotations;

namespace SurfTimer.Validators;

public class DecimalValidator : ValidationAttribute
{
    public int MaxDigits { get; }
    public int NumberOfDigits { get; }

    public DecimalValidator(int maxDigits, int numberOfDigits)
    {
        MaxDigits = maxDigits;
        NumberOfDigits = numberOfDigits;
    }

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        return ValidationResult.Success;
    }
}