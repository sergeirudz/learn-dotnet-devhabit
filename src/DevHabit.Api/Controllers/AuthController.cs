using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Auth;
using DevHabit.Api.DTOs.Users;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(
    // Built in: Allow to interact with users table
    UserManager<IdentityUser> userManager,
    ApplicationIdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext
) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterUserDto registerUserDto)
    {
        using IDbContextTransaction transaction = await identityDbContext.Database.BeginTransactionAsync();

        // Make sure both DB connections use the same underlying connection under the hood
        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        // Make context aware of the database transaction. Now both DBContext use same transaction 
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        // Create identity user
        var identityUser = new IdentityUser
        {
            Email = registerUserDto.Email,
            UserName = registerUserDto.Email
        };

        // Creating user here and persisting user below have to be in the same DB transaction
        IdentityResult identityResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);


        if (identityResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>
            {
                {
                    "erros", identityResult.Errors.ToDictionary(e => e.Code, e => e.Description)
                }
            };
            return Problem(
                "Unable to register user, please try again",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        // Create app user
        User user = registerUserDto.ToEntity();
        user.IdentityId = identityUser.Id;

        await applicationDbContext.SaveChangesAsync();

        // Now both are persisted in one DB transaction 
        await transaction.CommitAsync();

        return Ok(user.Id);
    }
}
