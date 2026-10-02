using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace BistroGo.Data.Identity
{
    internal class ApplicationUser : IdentityUser
    {
        // Additional properties for the application user
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
