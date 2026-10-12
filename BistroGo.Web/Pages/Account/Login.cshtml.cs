using System.ComponentModel.DataAnnotations;
using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (returnUrl != null && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                return RedirectToPage("/Index");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Too many failed attempts. Please try again in a few minutes.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }

            return Page();
        }

        public IActionResult OnPostForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ResetMessage"] = "Please enter an email address.";
                return RedirectToPage();
            }

            // ═══════════════════════════════════════════════════════════════
            // TODO: Replace with real reset-password logic:
            //   1. Look up the user by email (via Api or UserManager)
            //   2. If found, generate a reset token
            //   3. Send an email with a link like:
            //        /Account/ResetPassword?token=...&email=...
            //   4. If not found, do nothing (but still show the same confirmation
            //      message, so attackers can't tell which emails exist)
            // ═══════════════════════════════════════════════════════════════

            // Always show the same confirmation message — good security practice,
            // avoids leaking whether an email exists in the system.
            TempData["ResetMessage"] = $"If an account exists for {email}, a reset link has been sent.";
            return RedirectToPage();
        }
    }
}