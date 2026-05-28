using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Blazor.Services;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenIdProvider.Blazor.Endpoints;

public static class AccountEndpoints
{
    private const string InvalidCredentialsError = "Invalid email/username or password.";
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/account/handle-login", async (
        HttpContext httpContext,
        [FromQuery] string? returnUrl,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "/";
            }

            var form = await httpContext.Request.ReadFormAsync();
            var input = new LoginInputModel
            {
                EmailOrUsername = form["EmailOrUsername"].ToString(),
                Password = form["Password"],
                RememberMe = form["RememberMe"].Contains("true"),
                GraphicalPasswordFile = form.Files.GetFile("GraphicalPasswordFile")
            };

            // Validation logic ---
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(input);
            string? encodedError;
            if (!Validator.TryValidateObject(input, validationContext, validationResults, true))
            {
                var errorMessage = string.Join(" ", validationResults.Select(v => v.ErrorMessage));
                encodedError = WebUtility.UrlEncode(errorMessage);
                return Results.Redirect($"/account/login?error={encodedError}&returnUrl={returnUrl}");
            }

            var user = await userManager.FindByNameAsync(input.EmailOrUsername) ??
                       await userManager.FindByEmailAsync(input.EmailOrUsername);
            if (user == null)
            {
                encodedError = WebUtility.UrlEncode(InvalidCredentialsError);
                return Results.Redirect($"/account/login?error={encodedError}&returnUrl={returnUrl}");
            }

            Microsoft.AspNetCore.Identity.SignInResult result;
            string passwordToCheck = input.Password ?? string.Empty;

            // Graphical Password
            if (input.GraphicalPasswordFile is not null)
            {
                if (string.IsNullOrEmpty(user.GraphicalPasswordKey))
                {
                    encodedError = WebUtility.UrlEncode("Graphical login is not set up for this user.");
                    return Results.Redirect($"/account/login?error={encodedError}&returnUrl={returnUrl}");
                }

                try
                {
                    await using var imageStream = input.GraphicalPasswordFile.OpenReadStream();
                    using var image = await Image.LoadAsync<Rgba32>(imageStream);
                    // Extract the password from the image
                    passwordToCheck = ImageSteganographyUtility.ExtractText(image, user.GraphicalPasswordKey);
                }
                catch (InvalidOperationException)
                {
                    // This is a technical error (e.g., bad file format), not a failed login attempt.
                    encodedError = WebUtility.UrlEncode("The provided file is invalid or corrupted.");
                    return Results.Redirect($"/account/login?error={encodedError}&returnUrl={returnUrl}");
                }
                catch (CryptographicException)
                {
                    await userManager.AccessFailedAsync(user);
                    encodedError = WebUtility.UrlEncode(InvalidCredentialsError);
                    return Results.Redirect($"/account/login?error={encodedError}&returnUrl={returnUrl}");
                }
            }

            result = await signInManager.PasswordSignInAsync(user, passwordToCheck, input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return Results.Redirect(returnUrl);
            }

            var failMessage = InvalidCredentialsError;
            if (result.IsLockedOut)
            {
                failMessage = "This account has been locked out, please try again later.";
            }

            var finalEncodedError = WebUtility.UrlEncode(failMessage);
            return Results.Redirect($"/account/login?error={finalEncodedError}&returnUrl={returnUrl}");
        });

        app.MapPost("/account/handle-member-login", async (
             HttpContext httpContext,
             [FromQuery] string? returnUrl,
             SignInManager<ApplicationUser> signInManager,
             UserManager<ApplicationUser> userManager,
             IIdentityServerInteractionService interaction,
             ApplicationDbContext dbContext,
             ConfigurationDbContext configDbContext) =>
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "/";
            }

            var form = await httpContext.Request.ReadFormAsync();
            var input = new LoginInputModel
            {
                EmailOrUsername = form["EmailOrUsername"].ToString(),
                Password = form["Password"],
                RememberMe = form["RememberMe"].Contains("true"),
                GraphicalPasswordFile = form.Files.GetFile("GraphicalPasswordFile")
            };

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(input, new ValidationContext(input), validationResults, true))
            {
                var error = WebUtility.UrlEncode(string.Join(" ", validationResults.Select(v => v.ErrorMessage)));
                return Results.Redirect($"/account/login?error={error}&returnUrl={returnUrl}");
            }

            var user = await userManager.FindByNameAsync(input.EmailOrUsername) ?? await userManager.FindByEmailAsync(input.EmailOrUsername);
            if (user == null)
            {
                var error = WebUtility.UrlEncode(InvalidCredentialsError);
                return Results.Redirect($"/account/login?error={error}&returnUrl={returnUrl}");
            }

            // OIDC Flow
            var context = await interaction.GetAuthorizationContextAsync(returnUrl);
            if (context == null)
            {
                var error = WebUtility.UrlEncode("Invalid login request. Authorization context not found.");
                return Results.Redirect($"/account/login?error={error}&returnUrl={returnUrl}");
            }

            var client = await configDbContext.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == context.Client.ClientId);

            if (client == null)
            {
                var error = WebUtility.UrlEncode("Client application is not configured correctly.");
                return Results.Redirect($"/account/login?error={error}&returnUrl={returnUrl}");
            }
            var clientDbId = client.Id;

            // Check permissions user for client
            var hasUserPermission = await dbContext.UserClientPermissions.AnyAsync(p => p.UserId == user.Id && p.ClientId == clientDbId);
            if (!hasUserPermission)
            {
                var hasOrgPermission = await dbContext.OrganizationClientPermissions
                    .AnyAsync(p => p.ClientId == clientDbId &&
                        dbContext.UserOrganizationRoles.Any(r => r.UserId == user.Id && r.OrganizationId == p.OrganizationId));

                if (!hasOrgPermission)
                {
                    var error = WebUtility.UrlEncode("You do not have permission to access this application.");
                    return Results.Redirect($"/account/login?error={error}&returnUrl={returnUrl}");
                }
            }

            Microsoft.AspNetCore.Identity.SignInResult result;
            string passwordToCheck = input.Password ?? string.Empty;

            if (input.GraphicalPasswordFile is not null)
            {
                if (string.IsNullOrEmpty(user.GraphicalPasswordKey))
                {
                    var error = WebUtility.UrlEncode("Graphical login is not set up for this user.");
                    return Results.Redirect($"/account/login?error={error}&returnUrl={returnUrl}");
                }
                try
                {
                    await using var imageStream = input.GraphicalPasswordFile.OpenReadStream();
                    using var image = await Image.LoadAsync<Rgba32>(imageStream);
                    passwordToCheck = ImageSteganographyUtility.ExtractText(image, user.GraphicalPasswordKey);
                }
                catch (InvalidOperationException)
                {
                    // This is a technical error (e.g., bad file format), not a failed login attempt.
                    var error = WebUtility.UrlEncode("The provided file is invalid or corrupted.");
                    return Results.Redirect($"/account/login?error={error}&returnUrl={returnUrl}");
                }
                catch (CryptographicException)
                {
                    await userManager.AccessFailedAsync(user);
                    var error = WebUtility.UrlEncode(InvalidCredentialsError);
                    return Results.Redirect($"/account/login?error={error}&returnUrl={returnUrl}");
                }
            }

            result = await signInManager.PasswordSignInAsync(user, passwordToCheck, input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var isUser = new IdentityServerUser(user.Id.ToString())
                {
                    DisplayName = user.DisplayName
                };
                await httpContext.SignInAsync(isUser, new AuthenticationProperties { IsPersistent = input.RememberMe });
                return Results.Redirect(returnUrl);
            }

            var failMessage = InvalidCredentialsError;
            if (result.IsLockedOut)
            {
                failMessage = "This account has been locked out, please try again later.";
            }

            var finalError = WebUtility.UrlEncode(failMessage);
            return Results.Redirect($"/account/login?error={finalError}&returnUrl={returnUrl}");
        });

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

                var existingUser = await userManager.FindByEmailAsync(input.User.Email);

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
                    if (!createResult.Succeeded)
                        throw new Exception(string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    userToSignIn = newUser;
                }

                dbContext.UserOrganizationRoles.Add(new UserOrganizationRole
                {
                    UserId = userToSignIn.Id,
                    OrganizationId = organization.Id,
                    Role = OrganizationRole.Owner
                });

                await dbContext.SaveChangesAsync();
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

public class LoginInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Please input your Email or Username.")]
    public string EmailOrUsername { get; set; } = string.Empty;
    public string? Password { get; set; }
    public IFormFile? GraphicalPasswordFile { get; set; }
    public bool RememberMe { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Password) && GraphicalPasswordFile is null)
        {
            yield return new ValidationResult(
                "Please provide either a password or a graphical password file.");
        }
    }
}
public class RegistrationInputModel
{
    public UserInputModel User { get; set; } = new();
    public OrganizationInputModel Organization { get; set; } = new();
    public bool IsUpgrade { get; set; }
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
    [RegularExpression(@".*\d.*", ErrorMessage = "Password must contain at least one number.")]
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