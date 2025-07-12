using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;


namespace OpenIdProvider.Web.Pages.Organization
{
    public class CreateOrganizationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CreateOrganizationModel> _logger;

        public CreateOrganizationModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext,
            ILogger<CreateOrganizationModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        [BindProperty]
        public InputModel? Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Organization name is required.")]
            public required string Name { get; set; }
            public string? Description { get; set; }
            public string Slug { get; set; } = string.Empty;

        }

        // This is a private property to hold the deserialized user data.
        private Account.RegisterModel.InputModel? NewUserData { get; set; }

        public IActionResult OnGet()
        {
            // Check if the user data is present. If not, the user accessed this page directly.
            // Redirect them back to the start of the registration process.
            if (TempData["NewUserData"] == null)
            {
                return RedirectToPage("/Account/Register");
            }

            // Keep the data for the OnPost handler.
            TempData.Keep("NewUserData");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Retrieve and deserialize the user registration data from TempData.
            var newUserDataJson = TempData["NewUserData"] as string;

            if (string.IsNullOrEmpty(newUserDataJson))
            {
                ModelState.AddModelError(string.Empty, "Your session has expired. Please start over.");
                return RedirectToPage("/Account/Register");
            }

            NewUserData = JsonSerializer.Deserialize<Account.RegisterModel.InputModel>(newUserDataJson);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Start a database transaction to ensure both user and organization are created successfully.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Create the Organization
                var organization = new OpenIdProvider.Data.Models.Organization
                {
                    Name = Input!.Name,
                    Description = Input.Description,
                    Slug = Input.Name.ToLower().Replace(" ", "-"),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                _dbContext.Organizations.Add(organization);
                await _dbContext.SaveChangesAsync(); // Save to generate the organization's ID

                var user = new ApplicationUser
                {
                    UserName = NewUserData!.UserName,
                    DisplayName = NewUserData!.DisplayName,
                    Email = NewUserData.Email,
                    PrimaryOrganizationId = organization.Id
                };

                var result = await _userManager.CreateAsync(user, NewUserData.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with a password.");
                    // If everything is successful, commit the transaction.
                    await transaction.CommitAsync();

                    // --- START: Add Custom Claims Here ---
                    var currentUserWithDetails = await _userManager.Users
                        .Include(u => u.PrimaryOrganization)
                        .FirstOrDefaultAsync(u => u.Id == user.Id);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.UserName!),

                        new Claim("DisplayName", currentUserWithDetails?.DisplayName ?? user.UserName!),
                        new Claim("AvatarUrl", currentUserWithDetails?.AvatarUrl ?? "https://i.pravatar.cc/32"),
                        new Claim("PrimaryOrganizationId", currentUserWithDetails?.PrimaryOrganizationId.ToString() ?? string.Empty)
                    };

                    var originalPrincipal = await _signInManager.CreateUserPrincipalAsync(user);
                    var identity = (ClaimsIdentity)originalPrincipal.Identity!;

                    identity.AddClaims(claims);
                    // --- END: Add Custom Claims Here ---

                    // Sign the new user in.
                    await _signInManager.SignInAsync(user, isPersistent: false, identity.AuthenticationType);

                    return LocalRedirect(returnUrl);
                }

                // If user creation failed, add errors to ModelState. The transaction will be rolled back.
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the registration and organization creation process.");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            }

            // If we reached here, something failed. Rollback the transaction.
            await transaction.RollbackAsync();
            return Page();
        }
    }
}