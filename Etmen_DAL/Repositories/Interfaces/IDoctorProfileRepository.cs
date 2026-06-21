
namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IDoctorProfileRepository : IGenericRepository<DoctorProfile>
    {
        Task<DoctorProfile?> GetByUserIdAsync(string userId);
        Task<DoctorProfile?> GetWithAppointmentsAsync(string userId);
        Task<DoctorProfile?> GetWithAvailableSlotsAsync(int doctorId);
        Task<IEnumerable<DoctorProfile>> GetAvailableDoctorsAsync();
        Task<IEnumerable<DoctorProfile>> GetBySpecializationAsync(string specialization);
        Task<IEnumerable<DoctorProfile>> SearchDoctorsAsync(string searchTerm);
    }
}