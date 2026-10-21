using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    /// <summary>
    /// Contract for managing scheduled customer pickup time slots.
    /// </summary>
    public interface IPickupTimeSlotRepository
    {
        /// <summary>Retrieves all available pickup time slots sorted chronologically.</summary>
        Task<List<PickupTimeSlot>> GetAllAsync();

        /// <summary>Retrieves a single time slot by identifier.</summary>
        Task<PickupTimeSlot?> GetByIdAsync(int id);

        /// <summary>Retrieves all slots for a given date sorted by start time.</summary>
        Task<List<PickupTimeSlot>> GetByDateAsync(DateOnly date);

        /// <summary>Creates a new pickup time slot.</summary>
        Task<PickupTimeSlot> AddAsync(PickupTimeSlot slot);

        /// <summary>Updates date, times, or maximum order capacity for a slot.</summary>
        Task<bool> UpdateAsync(PickupTimeSlot slot);

        /// <summary>Soft-deletes a pickup slot by setting IsArchived = true.</summary>
        Task<bool> DeleteAsync(int id);
    }
}
