namespace UNI_EDU_Backend.Application.Exceptions;

public class NotFoundException : ApplicationException
{
    public NotFoundException(string title, string message) : base(title, message)
    {
        
    }

    public NotFoundException(string message) : base("Not Found", message)
    {
        
    }
}
