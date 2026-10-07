using System;
using System.Collections.Generic;
using System.Text;

namespace BistroGo.Data.Identity
{
    /// <summary>
    /// Defines the roles used in the application for authorization purposes.
    /// Not an enum because roles are typically represented as strings in ASP.NET Core Identity.
    /// </summary>
    public static class Roles
    {
        public const string Customer = "Customer";
        public const string KitchenStaff = "KitchenStaff";
        public const string Manager = "Manager";
    }
}
