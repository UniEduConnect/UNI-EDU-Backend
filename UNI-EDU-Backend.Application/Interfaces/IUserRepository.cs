using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces;

public interface IUserRepository
{
    public Task<User> CheckPhoneNumber(string phoneNumber);
}
