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
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ICartService _cart;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, 
            ICartService cart,
            ILogger<RegisterModel>? logger = null)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cart = cart;
            _logger = logger ?? NullLogger<RegisterModel>.Instance;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(Password), ErrorMessage = "Passwords don't match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            _logger.LogInformation("Registration page accessed");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", Input.Email);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Registration form validation failed for {Email}", Input.Email);
                    return Page();
                }

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Registration failed for {Email}: {Errors}",
                        Input.Email, string.Join("; ", result.Errors.Select(e => e.Description)));

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }

                _logger.LogInformation("User created successfully with id {UserId}. Assigning Customer role.", user.Id);
                await _userManager.AddToRoleAsync(user, RoleNames.Customer);

                await _signInManager.SignInAsync(user, isPersistent: false);

                // Merge guest cart with the new user cart if a guest token exists
                var guestToken = CartOwnerResolver.ReadGuestToken(HttpContext);
                if (guestToken != null)
                {
                    _logger.LogInformation("Merging guest cart {GuestToken} into newly registered user {UserId}", guestToken.Value, user.Id);
                    await _cart.MergeGuestCartAsync(guestToken.Value, user.Id);
                    CartOwnerResolver.ClearGuestToken(HttpContext);
                }

                if (returnUrl != null && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration for {Email}", Input.Email);
                throw;
            }
        }
    }
}
