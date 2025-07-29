using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

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
            [FromBody] UserInputModel userInput,
            UserManager<ApplicationUser> userManager) =>
        {
            var customErrors = new Dictionary<string, string[]>();
            var existingUserByEmail = await userManager.FindByEmailAsync(userInput.Email);

            if (existingUserByEmail != null && existingUserByEmail.PrimaryOrganizationId != null)
            {
                customErrors.Add(nameof(UserInputModel.Email),
                new[] { "An account with this email already exists. Please try logging in instead." });
            }

            var existingUserByName = await userManager.FindByNameAsync(userInput.UserName);
            if (existingUserByName != null)
            {
                customErrors.Add(nameof(UserInputModel.UserName), new[] { "A user with this username already exists." });
            }

            if (customErrors.Any())
            {
                return Results.ValidationProblem(customErrors);
            }

            return Results.Ok(new UserValidationResponse
            {
                CanProceed = true,
                IsExistingEndUser = existingUserByEmail != null && existingUserByEmail.PrimaryOrganizationId == null
            });
        });

        app.MapPost("/account/register", async (
            [FromForm] RegistrationInputModel input,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext,
            ILogger<Program> logger) =>
        {
            if (await dbContext.Organizations.AnyAsync(o => o.Slug == input.Organization.Slug))
            {
                var errorMessage = "An organization with this slug already exists. Please choose a different name or edit the slug.";
                var encodedError = WebUtility.UrlEncode(errorMessage);
                return Results.Redirect($"/account/register?postSubmitError={encodedError}");
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var organization = new Organization
                {
                    Name = input.Organization.Name,
                    Description = input.Organization.Description,
                    Slug = input.Organization.Slug,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                dbContext.Organizations.Add(organization);
                await dbContext.SaveChangesAsync();

                ApplicationUser userToSignIn;

                var existingUser = await userManager.FindByEmailAsync(input.User.Email) ??
                    throw new InvalidOperationException("User for upgrade not found.");

                if (existingUser != null && existingUser.PrimaryOrganizationId == null)
                {
                    existingUser.PrimaryOrganizationId = organization.Id;
                    existingUser.UserName = input.User.UserName;
                    existingUser.DisplayName = input.User.DisplayName;

                    // update password
                    if (await userManager.HasPasswordAsync(existingUser))
                    {
                        var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                        await userManager.ResetPasswordAsync(existingUser, token, input.User.Password);
                    }
                    else
                    {
                        await userManager.AddPasswordAsync(existingUser, input.User.Password);
                    }

                    await userManager.UpdateAsync(existingUser);
                    userToSignIn = existingUser;

                    var updateResult = await userManager.UpdateAsync(existingUser);
                    if (!updateResult.Succeeded)
                        throw new Exception(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    userToSignIn = existingUser;
                }
                else
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = input.User.UserName,
                        Email = input.User.Email,
                        DisplayName = input.User.DisplayName,
                        PrimaryOrganizationId = organization.Id
                    };
                    var createResult = await userManager.CreateAsync(newUser, input.User.Password);
                    if (!createResult.Succeeded) throw new Exception(string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    userToSignIn = newUser;
                }

                await transaction.CommitAsync();
                await signInManager.SignInAsync(userToSignIn, isPersistent: false);
                return Results.Redirect("/");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during registration. Input: {@Input}", input);
                await transaction.RollbackAsync();
                var encodedError = WebUtility.UrlEncode("An unexpected server error occurred. Please try again.");
                return Results.Redirect($"/account/register?postSubmitError={encodedError}");
            }
        });

        app.MapPost("/account/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/");
        }).RequireAuthorization();
    }
}

public class RegistrationInputModel
{
    public UserInputModel User { get; set; } = new();
    public OrganizationInputModel Organization { get; set; } = new();
    public bool IsUpgrade { get; set; } // <-- ДОДАЙТЕ ЦЕЙ РЯДОК
}
public class UserInputModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string UserName { get; set; } = "";
    [Required(ErrorMessage = "Display name is required.")]
    public string DisplayName { get; set; } = "";
    [Required(ErrorMessage = "Email is required."), EmailAddress]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = "";
}
public class OrganizationInputModel
{
    [Required] public string Name { get; set; } = "";
    public string? Description { get; set; }
    [Required] public string Slug { get; set; } = "";
}
public class UserValidationResponse
{
    public bool CanProceed { get; set; }
    public bool IsExistingEndUser { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}