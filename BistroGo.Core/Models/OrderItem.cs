using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Core.Models
{
    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }

        // Cascade: deleting an order deletes its line items
        [DeleteBehavior(DeleteBehavior.Cascade)]
        public Order? Order { get; set; }

        [ForeignKey(nameof(MenuItem))]
        public int MenuItemId { get; set; }

        // Restrict: a menu item that appears on any order can't be deleted
        // (mark it IsAvailable = false instead, so sales history survives)
        [DeleteBehavior(DeleteBehavior.Restrict)]
        public MenuItem? MenuItem { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        // Snapshot of MenuItem.Price when added, so later price changes
        // don't rewrite history.
        [Precision(10, 2)]
        public decimal UnitPrice { get; set; }

        [MaxLength(250)]
        public string? SpecialInstructions { get; set; }
    }
}
