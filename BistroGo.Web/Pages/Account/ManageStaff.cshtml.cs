using System.ComponentModel.DataAnnotations;
using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BistroGo.Web.Pages.Account
{
    /// <summary>
    /// Page model for the Manage Staff page.
    /// This page allows a Manager to generate new worker logins (KitchenStaff).
    /// Requires the user to have the Manager role.
    /// </summary>
    [Authorize(Roles = Roles.Manager)]
    public class ManageStaffModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Constructor for ManageStaffModel.
        /// </summary>
        /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
        public ManageStaffModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? StatusMessage { get; set; }

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
        }

        /// <summary>
        /// Handles the POST request to create a new worker (KitchenStaff) account.
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
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
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Add the new user to the KitchenStaff role (the 'worker')
            await _userManager.AddToRoleAsync(user, Roles.KitchenStaff);

            StatusMessage = "Worker account successfully created.";
            
            // Clear the form
            ModelState.Clear();
            Input = new InputModel();

            return Page();
        }
    }
}
