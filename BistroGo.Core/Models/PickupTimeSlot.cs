using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Core.Models
{
    [Table("PickupTimeSlots")]
    [Index(nameof(SlotDate), nameof(StartTime), IsUnique = true)]   // one slot per date + start time
    public class PickupTimeSlot
    {
        [Key]
        public int Id { get; set; }

        public DateOnly SlotDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxOrders { get; set; } = 10;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
