using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Blazor.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapPost("/account/validate-user-step", async (
            UserInputModel input,
            UserManager<ApplicationUser> userManager) =>
        {
            var errors = new Dictionary<string, string[]>();

            // Check if username exists
            if (await userManager.FindByNameAsync(input.UserName) != null)
            {
                errors.Add(nameof(input.UserName), new[] { "A user with this username already exists." });
            }

            // Check if email exists
            if (await userManager.FindByEmailAsync(input.Email) != null)
            {
                errors.Add(nameof(input.Email), new[] { "A user with this email address already exists." });
            }

            // Validate password policies
            var tempUser = new ApplicationUser { UserName = input.UserName, Email = input.Email };
            foreach (var validator in userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(userManager, tempUser, input.Password);
                if (!result.Succeeded)
                {
                    errors.Add(nameof(input.Password), result.Errors.Select(e => e.Description).ToArray());
                }
            }

            if (errors.Any())
            {
                return Results.ValidationProblem(errors);
            }

            return Results.Ok();
        });

        app.MapPost("/account/register", async (
            RegistrationInputModel input,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext,
            ILogger<Program> logger) =>
        {
            if (input is null) return Results.BadRequest("Input is required.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Create Organization
                var organization = new Organization
                {
                    Name = input.Organization.Name,
                    Description = input.Organization.Description,
                    Slug = input.Organization.Slug,
                    IsActive = true
                };
                dbContext.Organizations.Add(organization);
                await dbContext.SaveChangesAsync();

                // Create User
                var user = new ApplicationUser
                {
                    UserName = input.User.UserName,
                    DisplayName = input.User.DisplayName,
                    Email = input.User.Email,
                    PrimaryOrganizationId = organization.Id
                };
                var result = await userManager.CreateAsync(user, input.User.Password);

                if (result.Succeeded)
                {
                    logger.LogInformation("User created a new account.");
                    await transaction.CommitAsync();

                    // Sign In
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return Results.Ok();
                }
                else
                {
                    await transaction.RollbackAsync();
                    return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception during registration endpoint.");
                await transaction.RollbackAsync();
                return Results.Problem("An unexpected error occurred during registration.");
            }
        });

        app.MapPost("/account/logout", async (
                    SignInManager<ApplicationUser> signInManager,
                    [FromForm] string returnUrl) =>
                {
                    await signInManager.SignOutAsync();
                    return Results.LocalRedirect($"~/{returnUrl}");
                }).RequireAuthorization();
    }
}

public class RegistrationInputModel
{
    public UserInputModel User { get; set; } = new();
    public OrgInputModel Organization { get; set; } = new();
}
public class UserInputModel
{
    [Required(ErrorMessage = "Username is required.")] public string UserName { get; set; } = "";
    [Required(ErrorMessage = "Display name is required.")] public string DisplayName { get; set; } = "";
    [Required(ErrorMessage = "Email is required."), EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)] public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
}
public class OrgInputModel
{
    [Required] public string Name { get; set; } = "";
    public string? Description { get; set; }
    [Required] public string Slug { get; set; } = "";
}