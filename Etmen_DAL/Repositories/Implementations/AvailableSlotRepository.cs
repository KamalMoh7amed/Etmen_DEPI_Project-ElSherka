
namespace Etmen_DAL.Repositories.Implementations
{
    public class AvailableSlotRepository : GenericRepository<AvailableSlot>, IAvailableSlotRepository
    {
        public AvailableSlotRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<AvailableSlot>> GetByDoctorIdAndDateAsync(int doctorId, DateTime date)
            => await _dbSet.Where(s => s.DoctorProfileId == doctorId && s.SlotDate.Date == date.Date && !s.IsBooked)
                           .OrderBy(s => s.SlotStart).ToListAsync();

        public async Task<IEnumerable<AvailableSlot>> GetAvailableSlotsAsync(int doctorId, DateTime from, DateTime to)
            => await _dbSet.Where(s => s.DoctorProfileId == doctorId && !s.IsBooked && s.SlotDate >= from && s.SlotDate <= to)
                           .OrderBy(s => s.SlotDate).ThenBy(s => s.SlotStart).ToListAsync();

        public async Task<AvailableSlot?> GetNextAvailableSlotAsync(int doctorId, DateTime from)
            => await _dbSet.Where(s => s.DoctorProfileId == doctorId && !s.IsBooked && s.SlotDate >= from)
                           .OrderBy(s => s.SlotDate).ThenBy(s => s.SlotStart).FirstOrDefaultAsync();

        public async Task MarkSlotAsBookedAsync(int slotId)
        {
            var slot = await _dbSet.FindAsync(slotId);
            if (slot != null) { slot.IsBooked = true; _dbSet.Update(slot); }
        }

        public async Task MarkSlotAsAvailableAsync(int slotId)
        {
            var slot = await _dbSet.FindAsync(slotId);
            if (slot != null) { slot.IsBooked = false; _dbSet.Update(slot); }
        }

        public async Task<IEnumerable<AvailableSlot>> GetSlotsByDateRangeAsync(int doctorId, DateTime start, DateTime end)
            => await _dbSet.Where(s => s.DoctorProfileId == doctorId && s.SlotDate >= start && s.SlotDate <= end)
                           .OrderBy(s => s.SlotDate).ThenBy(s => s.SlotStart).ToListAsync();
    }
}