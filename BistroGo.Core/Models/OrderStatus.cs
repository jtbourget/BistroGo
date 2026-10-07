using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Core.Models
{
    /// <summary>
    /// Lookup table for the lifecycle of a placed order.
    /// Rows are seeded by the migration; use <see cref="OrderStatusIds"/> in code.
    /// </summary>
    [Table("OrderStatuses")]
    [Index(nameof(Name), IsUnique = true)]
    public class OrderStatus
    {
        // We choose the ids ourselves (seed data), so the database must NOT auto-number them
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string Name { get; set; } = "";

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    /// <summary>
    /// Known OrderStatus ids, so code says OrderStatusIds.Ready instead of a magic 3.
    /// Must stay in sync with the seed data.
    /// </summary>
    public static class OrderStatusIds
    {
        public const int Received  = 1;
        public const int Preparing = 2;
        public const int Ready     = 3;
        public const int PickedUp  = 4;
        public const int Cancelled = 5;
    }
}
