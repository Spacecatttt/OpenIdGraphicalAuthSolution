// In Pages/Account/Register.cshtml.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json; // Required for serialization

namespace OpenIdProvider.Web.Pages.Account
{
    public class RegisterModel : PageModel
    {
        // ... (Keep existing properties and constructor)

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

        public Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Task.FromResult<IActionResult>(Page());
            }

            // Temporarily store the new user's data to pass to the next step.
            TempData["NewUserData"] = JsonSerializer.Serialize(Input);

            // Redirect to the organization creation page.
            return Task.FromResult<IActionResult>(RedirectToPage("/Organization/CreateOrganization"));
        }
    }
}