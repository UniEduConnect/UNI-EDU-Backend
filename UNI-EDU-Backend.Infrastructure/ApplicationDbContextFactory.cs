using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UNI_EDU_Backend.Infrastructure;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        Env.Load("../.env");

        var host = Env.GetString("POSTGRES_HOST") ?? "localhost";
        var port = Env.GetString("POSTGRES_PORT") ?? "5432";
        var db = Env.GetString("POSTGRES_DB");
        var user = Env.GetString("POSTGRES_USER");
        var pass = Env.GetString("POSTGRES_PASSWORD");
        // RDS requires SSL; local docker-compose does not. Mirror Program.cs: default Disable,
        // set POSTGRES_SSLMODE=Require for managed/remote databases.
        var sslMode = Env.GetString("POSTGRES_SSLMODE") ?? "Disable";

        var connectionString = $"Host={host};Port={port};Username={user};Password={pass};Database={db};SSL Mode={sslMode};Trust Server Certificate=true;";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
