namespace UNI_EDU_Backend.API.Commons;

public class ApiResponse<T> where T : class
{
    public int StatusCode { get; set; }

    public string? Message { get; set; }

    public T? Data { get; set; }
}
