using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.API.Middleware;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Services.Tutors;
using UNI_EDU_Backend.Application.Services.Users;
using UNI_EDU_Backend.Infrastructure;
using UNI_EDU_Backend.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITutorService, TutorService>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();

Env.Load("../.env");
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
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
