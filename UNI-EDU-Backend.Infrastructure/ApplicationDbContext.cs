using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Domain;

namespace UNI_EDU_Backend.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}
