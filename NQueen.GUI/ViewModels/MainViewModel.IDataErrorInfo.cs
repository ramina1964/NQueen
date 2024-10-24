namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : IDataErrorInfo
{
    public string this[string columnName]
    {
        get
        {
            var validationFailure = InputViewModel
                .Validate(this)
                .Errors
                .FirstOrDefault(item => item.PropertyName == columnName);

            return validationFailure == null
                   ? string.Empty
                   : validationFailure.ErrorMessage;
        }
    }

    public string Error
    {
        get
        {
            var results = InputViewModel.Validate(this);
            if (results == null || results.Errors.Count == 0)
            { return string.Empty; }

            var errors = string
                .Join(Environment.NewLine, results.Errors
                .Select(x => x.ErrorMessage)
                .ToArray());

            return errors;
        }
    }
}
