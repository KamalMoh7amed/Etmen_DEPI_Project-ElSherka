

namespace Etmen_DAL.Repositories.Implementations
{
    public class DoctorProfileRepository : GenericRepository<DoctorProfile>, IDoctorProfileRepository
    {
        public DoctorProfileRepository(EtmenDbContext context) : base(context) { }

        public async Task<DoctorProfile?> GetByUserIdAsync(string userId)
            => await _dbSet.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);

        public async Task<DoctorProfile?> GetWithAppointmentsAsync(string userId)
            => await _dbSet.Include(d => d.Appointments.Where(a => a.Status ==AppointmentStatus.Scheduled))
                           .ThenInclude(a => a.PatientProfile).FirstOrDefaultAsync(d => d.ApplicationUserId == userId);

        public async Task<DoctorProfile?> GetWithAvailableSlotsAsync(int doctorId)
            => await _dbSet.Include(d => d.AvailableSlots.Where(s => !s.IsBooked && s.SlotDate >= DateTime.UtcNow.Date))
                           .FirstOrDefaultAsync(d => d.Id == doctorId);

        public async Task<IEnumerable<DoctorProfile>> GetAvailableDoctorsAsync()
            => await _dbSet.Where(d => d.IsAvailable).Include(d => d.ApplicationUser).ToListAsync();

        public async Task<IEnumerable<DoctorProfile>> GetBySpecializationAsync(string specialization)
            => await _dbSet.Where(d => d.Specialization!.Contains(specialization) && d.IsAvailable)
                           .Include(d => d.ApplicationUser).ToListAsync();

        public async Task<IEnumerable<DoctorProfile>> SearchDoctorsAsync(string searchTerm)
            => await _dbSet.Where(d => d.FullName!.Contains(searchTerm) || d.Specialization!.Contains(searchTerm) || d.Bio!.Contains(searchTerm))
                           .Include(d => d.ApplicationUser).ToListAsync();
    }
}