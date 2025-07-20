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
            var validationContext = new ValidationContext(userInput);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(userInput, validationContext, validationResults, true))
            {
                return Results.ValidationProblem(validationResults.ToDictionary(
                    vr => vr.MemberNames.FirstOrDefault() ?? string.Empty,
                    vr => vr.ErrorMessage is not null ? new[] { vr.ErrorMessage } : Array.Empty<string>()
                ));
            }

            var customErrors = new Dictionary<string, string[]>();
            if (await userManager.FindByNameAsync(userInput.UserName) != null)
            {
                customErrors.Add(nameof(UserInputModel.UserName), new[] { "A user with this username already exists." });
            }
            if (await userManager.FindByEmailAsync(userInput.Email) != null)
            {
                customErrors.Add(nameof(UserInputModel.Email), new[] { "A user with this email address already exists." });
            }

            var dummyUser = new ApplicationUser
            {
                UserName = userInput.UserName,
                Email = userInput.Email
            };

            foreach (var validator in userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(userManager, dummyUser, userInput.Password);
                if (!result.Succeeded)
                {
                    customErrors.Add(nameof(UserInputModel.Password), result.Errors.Select(e => e.Description).ToArray());
                    break;
                }
            }

            if (customErrors.Count > 0)
                return Results.ValidationProblem(customErrors);

            return Results.Ok();
        });

        app.MapPost("/account/register", async (
            [FromForm] RegistrationInputModel input,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext,
            ILogger<Program> logger) =>
        {
            var allValidationResults = new List<ValidationResult>();
            var userValidationContext = new ValidationContext(input.User);
            Validator.TryValidateObject(input.User, userValidationContext, allValidationResults, true);
            var orgValidationContext = new ValidationContext(input.Organization);
            Validator.TryValidateObject(input.Organization, orgValidationContext, allValidationResults, true);

            if (allValidationResults.Count > 0)
            {
                var errorMessage = string.Join(" ", allValidationResults.Select(v => v.ErrorMessage));
                var encodedError = WebUtility.UrlEncode(errorMessage);
                return Results.Redirect($"/account/register?postSubmitError={encodedError}");
            }

            if (await dbContext.Organizations.AnyAsync(o => o.Slug == input.Organization.Slug))
            {
                var errorMessage = "An organization with that slug already exists. Please choose a different name or edit the slug.";
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
                    IsActive = true
                };
                dbContext.Organizations.Add(organization);
                await dbContext.SaveChangesAsync();

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
                    await transaction.CommitAsync();
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return Results.Redirect("/");
                }
                else
                {
                    await transaction.RollbackAsync();
                    var errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                    var encodedError = WebUtility.UrlEncode(errorMessage);
                    return Results.Redirect($"/account/register?postSubmitError={encodedError}");
                }
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