using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Core.Models
{
    /// <summary>
    /// An order. While IsFinalized is false this row IS the shopping cart.
    /// When the customer checks out: IsFinalized = true, FinalizedAt, StatusId,
    /// PickupSlotId and ConfirmationCode all get set.
    /// Orders are archived (IsArchived = true), not deleted, once they're done.
    /// </summary>
    [Table("Orders")]
    [Index(nameof(GuestToken), IsUnique = true)]
    [Index(nameof(ConfirmationCode), IsUnique = true)]
    [Index(nameof(UserId), nameof(IsFinalized))]       // fast "find my open cart" lookup
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Owner: a logged-in user (UserId) OR a guest (Guest* fields)
        // FK -> AspNetUsers.Id, wired up in the DbContext (step 3)
        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(100)]
        public string? GuestName { get; set; }

        [MaxLength(20)]
        public string? GuestPhone { get; set; }

        [MaxLength(256), EmailAddress]
        public string? GuestEmail { get; set; }

        // Random, unguessable id stored in a guest's browser cookie so we can find
        // their cart again (UserId is NULL for every guest, so it can't do that job).
        // Null for logged-in users' orders.
        public Guid? GuestToken { get; set; }

        [ForeignKey(nameof(PickupSlot))]
        public int? PickupSlotId { get; set; }

        [DeleteBehavior(DeleteBehavior.Restrict)]
        public PickupTimeSlot? PickupSlot { get; set; }

        [ForeignKey(nameof(Status))]
        public int? StatusId { get; set; }

        [DeleteBehavior(DeleteBehavior.Restrict)]
        public OrderStatus? Status { get; set; }

        public bool IsFinalized { get; set; }

        [Precision(10, 2)]
        public decimal Subtotal { get; set; }

        [Precision(10, 2)]
        public decimal Tax { get; set; }

        [Precision(10, 2)]
        public decimal Total { get; set; }

        [MaxLength(12)]
        public string? ConfirmationCode { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? FinalizedAt { get; set; }

        // Soft delete: archived orders are hidden from the live kitchen queue
        // but stay in the database so kitchen staff can review past orders.
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}
