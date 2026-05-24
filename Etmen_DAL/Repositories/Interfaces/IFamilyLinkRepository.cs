using Etmen_Domain.Entities;

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IFamilyLinkRepository : IGenericRepository<FamilyLink>
    {
        Task<IEnumerable<FamilyLink>> GetByPrimaryPatientIdAsync(int primaryPatientId);
        Task<IEnumerable<FamilyLink>> GetByLinkedPatientIdAsync(int linkedPatientId);
        Task<IEnumerable<PatientProfile>> GetFamilyMembersAsync(int patientId);
        Task<FamilyLink?> GetByInviteTokenAsync(string inviteToken);
        Task<bool> IsFamilyLinkExistsAsync(int primaryPatientId, int linkedPatientId);
        Task UpdatePermissionsAsync(int familyLinkId, bool canViewRecords, bool canViewRisk, bool canBookAppointments);
        Task AcceptInviteAsync(string inviteToken, int linkedPatientId);
    }
}