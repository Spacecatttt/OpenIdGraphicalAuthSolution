using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Web.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        [BindProperty]
        public InputModel? Input { get; set; }

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required.")]
            public required string UserName { get; set; }

            [Required(ErrorMessage = "Display name is required.")]
            public required string DisplayName { get; set; }

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public required string Email { get; set; }

            [DataType(DataType.Password)]
            [Required(ErrorMessage = "Password is required.")]
            [StringLength(100, ErrorMessage = "{0} must be between {2} and {1} characters long.", MinimumLength = 6)]
            public required string Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public required string ConfirmPassword { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingUserName = await _userManager.FindByNameAsync(Input!.UserName);
            if (existingUserName != null)
            {
                ModelState.AddModelError("Input.UserName", "A user with this username already exists.");
                return Page();
            }

            var existingUserEmail = await _userManager.FindByEmailAsync(Input!.Email);
            if (existingUserEmail != null)
            {
                ModelState.AddModelError("Input.Email", "A user with this email address already exists.");
                return Page();
            }

            var tempUser = new ApplicationUser { UserName = Input.UserName, Email = Input.Email };
            var passwordValidationErrors = new List<string>();
            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, tempUser, Input.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        passwordValidationErrors.Add(error.Description);
                    }
                }
            }

            if (passwordValidationErrors.Any())
            {
                foreach (var errorDescription in passwordValidationErrors)
                {
                    ModelState.AddModelError("Input.Password", errorDescription);
                }
                return Page();
            }


            // Temporarily store the new user's data to pass to the next step.
            TempData["NewUserData"] = JsonSerializer.Serialize(Input);

            // Redirect to the organization creation page.
            return LocalRedirect("/Organization/CreateOrganization");
        }
    }
}