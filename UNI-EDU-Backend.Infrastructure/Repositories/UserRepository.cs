using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<User> CheckPhoneNumber(string phoneNumber)
    {
        User user = await _dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber) ?? throw new NotFoundException("User with the given phone number not found.");

        return user;
    }
}
