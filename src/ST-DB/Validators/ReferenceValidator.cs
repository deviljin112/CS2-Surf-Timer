using System.ComponentModel.DataAnnotations;

namespace SurfTimer.Validators;

public class ReferenceValidator : ValidationAttribute
{
    public string TableName { get; }
    public string ForeignKey { get; }

    public ReferenceValidator(string tableName, string foreignKey)
    {
        TableName = tableName;
        ForeignKey = foreignKey;
    }

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        return ValidationResult.Success;
    }
}