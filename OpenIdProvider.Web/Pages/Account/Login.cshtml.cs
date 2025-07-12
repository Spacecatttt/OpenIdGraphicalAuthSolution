using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using OpenIdProvider.Data.Models;


namespace OpenIdProvider.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager,
                          UserManager<ApplicationUser> userManager,
                          ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel? Input { get; set; }

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email or Username is required.")]
            // [EmailAddress]
            public required string EmailOrUsername { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public required string Password { get; set; }

            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid || Input == null)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.EmailOrUsername);
            user ??= await _userManager.FindByNameAsync(Input.EmailOrUsername);
            if (user != null)
            {
                var passwordCheck = await _userManager.CheckPasswordAsync(user, Input.Password);

                if (passwordCheck)
                {
                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                    // Reset the access failed count before signing in
                    await _userManager.ResetAccessFailedCountAsync(user);
                    _logger.LogInformation("User logged in.");

                    // --- START: Add Custom Claims Here ---

                    var currentUserWithDetails = await _userManager.Users
                        .Include(u => u.PrimaryOrganization)
                        .FirstOrDefaultAsync(u => u.Id == user.Id);

                    var principal = await _signInManager.CreateUserPrincipalAsync(user);
                    var identity = (ClaimsIdentity)principal.Identity!;
                    identity.AddClaim(new Claim("DisplayName", currentUserWithDetails?.DisplayName ?? user.UserName!));
                    identity.AddClaim(new Claim("AvatarUrl", currentUserWithDetails?.AvatarUrl ?? "https://i.pravatar.cc/32"));
                    identity.AddClaim(new Claim("PrimaryOrganizationId", currentUserWithDetails?.PrimaryOrganizationId.ToString() ?? string.Empty));
                    // --- END: Add Custom Claims Here ---

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = Input.RememberMe,
                        // ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                    };

                    await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, authProperties);

                    return LocalRedirect(returnUrl);
                }
                else
                {
                    await _userManager.AccessFailedAsync(user);
                }
            }
            _logger.LogWarning("Invalid login attempt.");
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}