using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Reviews
{
    /// <summary>
    /// PageModel for the dedicated customer reviews and feedback page.
    /// Provides initial user context for pre-populating reviewer identity.
    /// Accessible only to customers and logged-out users; managers will have a dedicated review portal.
    /// </summary>
    public class IndexModel : PageModel
    {
        /// <summary>
        /// Display name for the logged-in customer, if authenticated.
        /// </summary>
        public string? CurrentUserName { get; set; }

        public IActionResult OnGet()
        {
            // Only customers and logged-out users should access the customer review page,
            // assign the current user's name if authenticated, otherwise redirect managers
            // to the main page.
            if (User.Identity?.IsAuthenticated == true)
            {
                CurrentUserName = User.Identity.Name;

                if (!User.IsInRole(Roles.Customer))
                {
                    return RedirectToPage("/Index");
                }
            }

            return Page();
        }
    }
}
