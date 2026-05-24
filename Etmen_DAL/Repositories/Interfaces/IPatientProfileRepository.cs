using Etmen_Domain.Entities;

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IPatientProfileRepository : IGenericRepository<PatientProfile>
    {
        Task<PatientProfile?> GetByUserIdAsync(string userId);
        Task<PatientProfile?> GetWithMedicalRecordsAsync(string userId);
        Task<PatientProfile?> GetWithRiskAssessmentsAsync(string userId);
        Task<PatientProfile?> GetWithAppointmentsAsync(string userId);
        Task<PatientProfile?> GetWithFamilyLinksAsync(int patientId);
        Task<IEnumerable<PatientProfile>> GetFamilyMembersAsync(int patientId);
        Task<decimal?> GetLatestBmiAsync(string userId);
    }
}