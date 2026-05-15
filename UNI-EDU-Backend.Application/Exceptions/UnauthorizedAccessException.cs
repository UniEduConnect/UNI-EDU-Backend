namespace UNI_EDU_Backend.Application.Exceptions;

public class UnauthorizedAccessException : ApplicationException
{
    public UnauthorizedAccessException(string title, string message) : base(title, message)
    {
    }

    public UnauthorizedAccessException(string message) : base("Unauthorized Access", message)
    {
        
    }
}
