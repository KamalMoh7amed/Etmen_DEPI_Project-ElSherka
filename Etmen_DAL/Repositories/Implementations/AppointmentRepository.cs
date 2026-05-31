using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId)
            => await _dbSet.Include(a => a.DoctorProfile).ThenInclude(d => d.ApplicationUser)
                           .Where(a => a.PatientProfileId == patientId).OrderByDescending(a => a.AppointmentDate).ToListAsync();

        public async Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId)
            => await _dbSet.Include(a => a.PatientProfile).ThenInclude(p => p.ApplicationUser)
                           .Where(a => a.DoctorProfileId == doctorId).OrderByDescending(a => a.AppointmentDate).ToListAsync();

        public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(int patientId)
            => await _dbSet.Include(a => a.DoctorProfile).ThenInclude(d => d.ApplicationUser)
                           .Where(a => a.PatientProfileId == patientId && a.Status == AppointmentStatus.Scheduled && a.AppointmentDate >= DateTime.UtcNow.Date)
                           .OrderBy(a => a.AppointmentDate).ToListAsync();

        public async Task<IEnumerable<Appointment>> GetByDateAsync(DateTime date)
            => await _dbSet.Include(a => a.PatientProfile).Include(a => a.DoctorProfile)
                           .Where(a => a.AppointmentDate == date).OrderBy(a => a.StartTime).ToListAsync();

        public async Task<Appointment?> GetWithDetailsAsync(int appointmentId)
            => await _dbSet.Include(a => a.PatientProfile).ThenInclude(p => p.ApplicationUser)
                           .Include(a => a.DoctorProfile).ThenInclude(d => d.ApplicationUser)
                           .FirstOrDefaultAsync(a => a.Id == appointmentId);

        public async Task<IEnumerable<Appointment>> GetByStatusAsync(AppointmentStatus status)
            => await _dbSet.Where(a => a.Status == status).OrderByDescending(a => a.AppointmentDate).ToListAsync();

        public async Task<int> GetAppointmentsCountByDateAsync(DateTime date, int? doctorId = null)
            => doctorId.HasValue
                ? await _dbSet.CountAsync(a => a.AppointmentDate == date && a.DoctorProfileId == doctorId)
                : await _dbSet.CountAsync(a => a.AppointmentDate == date);

        public async Task CancelAppointmentAsync(int appointmentId, string reason)
        {
            var appt = await _dbSet.FindAsync(appointmentId);
            if (appt != null) { appt.Status = AppointmentStatus.Cancelled; appt.Notes = reason; _dbSet.Update(appt); }
        }
    }
}