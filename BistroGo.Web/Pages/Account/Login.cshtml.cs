using System.ComponentModel.DataAnnotations;
using BistroGo.Core.Identity;
using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BistroGo.Core.Interfaces;
using BistroGo.Web.Cart;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ICartService _cart;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            ICartService cart,
            ILogger<LoginModel>? logger = null)
        {
            _signInManager = signInManager;
            _cart = cart;
            _logger = logger ?? NullLogger<LoginModel>.Instance;
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
            _logger.LogInformation("Login page accessed");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            _logger.LogInformation("Login attempt for email: {Email}", Input.Email);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login model state invalid for {Email}", Input.Email);
                    return Page();
                }

                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully", Input.Email);

                    // If the user had a guest cart, merge it into their account cart
                    var guestToken = CartOwnerResolver.ReadGuestToken(HttpContext);
                    if (guestToken != null)
                    {
                        var user = await _signInManager.UserManager.FindByNameAsync(Input.Email);
                        if (user != null)
                        {
                            _logger.LogInformation("Merging guest cart {GuestToken} for user {UserId}", guestToken.Value, user.Id);
                            await _cart.MergeGuestCartAsync(guestToken.Value, user.Id);
                        }

                        CartOwnerResolver.ClearGuestToken(HttpContext);
                    }

                    if (returnUrl != null && Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    return RedirectToPage("/Index");
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Account locked out for user {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Too many failed attempts. Please try again in a few minutes.");
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for {Email}", Input.Email);
                throw;
            }
        }

        public IActionResult OnPostForgotPassword(string email)
        {
            _logger.LogInformation("Forgot password requested for email: {Email}", email);

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ResetMessage"] = "Please enter an email address.";
                return RedirectToPage();
            }

            // Always show the same confirmation message — good security practice,
            // avoids leaking whether an email exists in the system.
            TempData["ResetMessage"] = $"If an account exists for {email}, a reset link has been sent.";
            return RedirectToPage();
        }
    }
}
