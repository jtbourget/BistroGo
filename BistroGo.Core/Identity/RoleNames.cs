namespace BistroGo.Core.Identity
{
    /// <summary>
    /// Defines the roles used in the application for authorization purposes.
    /// These are core domain concepts, not infrastructure-specific.
    /// Not an enum because roles are typically represented as strings in ASP.NET Core Identity.
    /// </summary>
    public static class RoleNames
    {
        public const string Customer = "Customer";
        public const string KitchenStaff = "KitchenStaff";
        public const string Manager = "Manager";
    }
}
