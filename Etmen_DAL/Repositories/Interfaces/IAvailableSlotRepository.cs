

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IAvailableSlotRepository : IGenericRepository<AvailableSlot>
    {
        Task<IEnumerable<AvailableSlot>> GetByDoctorIdAndDateAsync(int doctorId, DateTime date);
        Task<IEnumerable<AvailableSlot>> GetAvailableSlotsAsync(int doctorId, DateTime fromDate, DateTime toDate);
        Task<AvailableSlot?> GetNextAvailableSlotAsync(int doctorId, DateTime fromDateTime);
        Task MarkSlotAsBookedAsync(int slotId);
        Task MarkSlotAsAvailableAsync(int slotId);
        Task<IEnumerable<AvailableSlot>> GetSlotsByDateRangeAsync(int doctorId, DateTime startDate, DateTime endDate);
    }
}