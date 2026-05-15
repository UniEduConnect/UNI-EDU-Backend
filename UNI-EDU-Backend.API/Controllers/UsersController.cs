using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.Domain;
using UNI_EDU_Backend.Infrastructure;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public ActionResult<User> GetTest()
    {
        var user = dbContext.Users.FirstOrDefault() ?? 
            throw new InvalidDataException("No users found in the database.");
        return StatusCode(StatusCodes.Status200OK, user);
    }
}
