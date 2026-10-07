using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Core.Models
{
    [Table("Expenses")]
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        // FK -> AspNetUsers.Id (was recorded_by_staff_id), wired up in the DbContext (step 3)
        [Required, MaxLength(450)]
        public string RecordedByUserId { get; set; } = "";

        // Optional FK -> MenuItems.Id
        [ForeignKey(nameof(MenuItem))]
        public int? MenuItemId { get; set; }

        // SetNull: deleting a menu item keeps the expense, just unlinks it
        [DeleteBehavior(DeleteBehavior.SetNull)]
        public MenuItem? MenuItem { get; set; }

        [Required, MaxLength(250)]
        public string Description { get; set; } = "";

        [Precision(10, 2)]
        [Range(0, 99999999.99)]
        public decimal Amount { get; set; }

        [Required, MaxLength(50)]
        public string Category { get; set; } = "";

        public DateOnly ExpenseDate { get; set; }
    }
}
