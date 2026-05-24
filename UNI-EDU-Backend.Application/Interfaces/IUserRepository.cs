namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IUserRepository
{
    public Task<bool> CheckPhoneNumber(string phoneNumber);
}
