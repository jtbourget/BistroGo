using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.DataAccess
{
    public class BistroGoDBContext : IdentityDbContext<ApplicationUser>
    {
        public BistroGoDBContext(DbContextOptions<BistroGoDBContext> options) : base(options)
        {
        }
    }
}