using System.ComponentModel.DataAnnotations;

namespace SurfTimer.ST_DB.Validators;

/// <summary>
/// This validator will override the C# datatype definition when building the SQL query.
/// This is handy when working with blobs as the C# equivalent would just be a byte[].
/// </summary>
public class CustomTypeValidator : ValidationAttribute
{
    public string CustomType { get; }

    public CustomTypeValidator(string customType)
    {
        CustomType = customType;
    }

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        return ValidationResult.Success;
    }
}