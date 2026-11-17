using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel>? logger = null)
        {
            _signInManager = signInManager;
            _logger = logger ?? NullLogger<LogoutModel>.Instance;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userName = User.Identity?.Name;
            _logger.LogInformation("User {UserName} signing out", userName);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {UserName} signed out successfully", userName);
            return RedirectToPage("/Index");
        }
    }
}
