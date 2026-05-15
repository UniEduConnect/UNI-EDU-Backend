namespace UNI_EDU_Backend.Application.Exceptions;

public class ValidationException : ApplicationException
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errorsDictionary) : base("Validation Failure", "Validation failed")
    {
        ErrorsDictionary = errorsDictionary;
    }

    public IReadOnlyDictionary<string, string[]> ErrorsDictionary { get; set; }
}
