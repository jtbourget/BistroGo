using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Core.Models
{
    [Table("MenuItems")]
    public class MenuItem
    {
        [Key]
        public int Id { get; set; }

        // FK -> MenuCategories.Id (replaces the old free-text Category string)
        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        // Restrict: a category that still has items can't be deleted
        [DeleteBehavior(DeleteBehavior.Restrict)]
        public MenuCategory? Category { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(500)]
        public string? Description { get; set; }

        [Precision(10, 2)]
        [Range(0, 99999999.99)]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; } = true;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // FK -> AspNetUsers.Id. String because Identity user ids are GUID strings.
        // No navigation property: Core can't see ApplicationUser (it lives in Data),
        // so this relationship is wired up in the DbContext (step 3).
        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
