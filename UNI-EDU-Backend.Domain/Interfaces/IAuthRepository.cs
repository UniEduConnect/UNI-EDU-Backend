using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Domain.Interfaces
{
    public interface IAuthRepository : IGenericRepository<User>
    {
        Task<bool> IsPhonenumberOrEmailTakenAsync(string phoneNumber, string email);

        Task<User> GetUserByEmailAsync(string email);

        Task AddRefreshTokenAsync(RefreshToken refreshToken);
    }
}