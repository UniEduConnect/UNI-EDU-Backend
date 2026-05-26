using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using UNI_EDU_Backend.API.Middleware;
using UNI_EDU_Backend.Application.Mappings;
using UNI_EDU_Backend.API.Json;
using UNI_EDU_Backend.Application.Services.Classes;
using UNI_EDU_Backend.Application.Services.Parents;
using UNI_EDU_Backend.Application.Services.Tutors;
using UNI_EDU_Backend.Application.Services.Users;
using UNI_EDU_Backend.Infrastructure;
using UNI_EDU_Backend.Infrastructure.Repositories;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Application.Services.Auths;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Exceptions;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new FlexibleTimeOnlyConverter());
    });

//Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITutorService, TutorService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IParentService, ParentService>();

builder.Services.AddAutoMapper(typeof(MappingProfile));

var assemblyApplication = typeof(UNI_EDU_Backend.Application.IAssemblyReference).Assembly;
builder.Services.AddValidatorsFromAssembly(assemblyApplication);

Env.Load("../.env");
builder.Configuration.AddEnvironmentVariables();
var db = Env.GetString("POSTGRES_DB");
var user = Env.GetString("POSTGRES_USER");
var pass = Env.GetString("POSTGRES_PASSWORD");
var host = Env.GetString("POSTGRES_HOST") ?? "localhost";
var port = Env.GetString("POSTGRES_PORT") ?? "5432";
var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass};";
builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseNpgsql(connectionString));

// Enable Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT only (without the 'Bearer ' prefix). Swagger UI will add it."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc, null),
            new List<string>()
        }
    });
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var secretKey = builder.Configuration["Jwt:SecretKey"] ?? Env.GetString("JWT_SecretKey")
    ?? throw new InvalidOperationException("Jwt:SecretKey (or JWT_SecretKey env var) is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // small tolerance for host/container time drift
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                // Suppress the default empty 401 so the thrown exception reaches GlobalExceptionHandlerMiddleware.
                ctx.HandleResponse();
                var message = !string.IsNullOrWhiteSpace(ctx.ErrorDescription)
                    ? ctx.ErrorDescription
                    : "Authentication is required to access this resource.";
                throw new UnauthorizedAccessException(message);
            },
            OnForbidden = ctx =>
            {
                throw new ForbiddenAccessException("You do not have permission to access this resource.");
            }
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.InjectJavascript("/swagger-auth.js");
    });
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
