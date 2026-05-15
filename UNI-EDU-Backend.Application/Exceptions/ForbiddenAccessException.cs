namespace UNI_EDU_Backend.Application.Exceptions;

public class ForbiddenAccessException : ApplicationException
{
    public ForbiddenAccessException(string title, string message) : base(title, message)
    {
    }

    public ForbiddenAccessException(string messsage) : base("Forbidden Access", messsage)
    {
        
    }
}
