namespace UNI_EDU_Backend.Application.Interfaces;

public interface IUserRepository
{
    public Task<bool> CheckPhoneNumber(string phoneNumber);
}
