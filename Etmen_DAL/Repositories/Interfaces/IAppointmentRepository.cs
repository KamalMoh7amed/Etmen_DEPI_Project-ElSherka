using Etmen_Domain.Entities;
using Etmen_Domain.Enums;

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IAppointmentRepository : IGenericRepository<Appointment>
    {
        Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId);
        Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(int patientId);
        Task<IEnumerable<Appointment>> GetByDateAsync(DateTime date);
        Task<Appointment?> GetWithDetailsAsync(int appointmentId);
        Task<IEnumerable<Appointment>> GetByStatusAsync(AppointmentStatus status);
        Task<int> GetAppointmentsCountByDateAsync(DateTime date, int? doctorId = null);
        Task CancelAppointmentAsync(int appointmentId, string cancellationReason);
    }
}