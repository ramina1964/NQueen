namespace NQueen.GUI.Views;

public class NumericValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult(false, "Value cannot be empty.");
        }

        if (!byte.TryParse(value.ToString(), out _))
        {
            return new ValidationResult(false, "Value must be a valid number.");
        }

        return ValidationResult.ValidResult;
    }
}
