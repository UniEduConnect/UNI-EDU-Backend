namespace UNI_EDU_Backend.Application.Exceptions;

public class BadRequestException : ApplicationException
{
    public BadRequestException(string title, string message) : base(title, message)
    {
        
    }

    public BadRequestException(string message) : base("Bad Request", message)
    {
        
    }
}
