using System.ComponentModel.DataAnnotations;

namespace SurfTimer.Validators;

public class CommentValidator : ValidationAttribute
{
    public string Comment { get; }

    public CommentValidator(string comment)
    {
        Comment = comment;
    }

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        return ValidationResult.Success;
    }
}