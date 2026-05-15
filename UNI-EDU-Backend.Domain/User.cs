namespace UNI_EDU_Backend.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;
}
