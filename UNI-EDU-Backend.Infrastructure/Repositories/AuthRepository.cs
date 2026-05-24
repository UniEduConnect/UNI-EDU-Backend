using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories
{
    public class AuthRepository : GenericRepository<User>, IAuthRepository
    {
        public AuthRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<bool> IsPhonenumberOrEmailTakenAsync(string phoneNumber, string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber || u.Email == email) != null;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _dbSet
            .Include(u => u.Tutor)
            .Include(u => u.Student)
            .Include(u => u.Parent)
            .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.AddAsync(refreshToken);
        }
    }
}