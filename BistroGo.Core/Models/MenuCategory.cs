using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Core.Models
{
    [Table("MenuCategories")]
    [Index(nameof(Name), IsUnique = true)]          // no two categories with the same name
    public class MenuCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = "";

        public int DisplayOrder { get; set; }

        // Navigation: one category has many menu items
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
