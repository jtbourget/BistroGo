using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BistroGo.Data.Identity
{
    /// <summary>
    /// The "User" table (AspNetUsers). Roles live in AspNetRoles and are linked
    /// many-to-many through AspNetUserRoles, so one user can be KitchenStaff AND Customer.
    /// Id, Email, PasswordHash etc. are inherited from IdentityUser and already configured by Identity.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        // Carried over from the old staff_user.created_at
        public DateTime CreatedAt { get; set; }
    }
}
