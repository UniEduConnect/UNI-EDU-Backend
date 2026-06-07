using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<bool> CheckPhoneNumber(string phoneNumber)
    {
        var isExist = await _dbContext.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);

        if (!isExist) 
            throw new NotFoundException("User with the given phone number not found.");

        return isExist;
    }
}
