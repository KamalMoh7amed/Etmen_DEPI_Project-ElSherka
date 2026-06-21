

namespace Etmen_BLL.Repositories.IServices
{
    /// <summary>
    /// Contract for patient-profile management, dashboard, and risk assessment operations.
    /// </summary>
    public interface IPatientService
    {
        // ── Profile ───────────────────────────────────────────────────────────────

        Task<ServiceResult<ProfileDto>> GetProfileAsync(string userId);

        Task<ServiceResult<ProfileDto>> GetProfileByIdAsync(int patientProfileId);

        Task<ServiceResult<ProfileDto>> UpdateProfileAsync(string userId, ProfileDto dto);

        // ── Dashboard ─────────────────────────────────────────────────────────────

        Task<ServiceResult<DashboardDto>> GetDashboardAsync(string userId);

        Task<ServiceResult<DashboardDto>> GetDashboardByProfileIdAsync(int patientProfileId);

        // ── Medical Records ───────────────────────────────────────────────────────

        Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetMedicalRecordsAsync(string userId);

        Task<ServiceResult<MedicalRecordDto>> GetLatestMedicalRecordAsync(string userId);

        Task<ServiceResult<MedicalRecordDto>> AddMedicalRecordAsync(string userId, MedicalRecordCreateDto dto);

        // ── Risk Assessment ───────────────────────────────────────────────────────

        Task<ServiceResult<RiskResultDto>> AssessRiskAsync(string userId, RiskInputDto input);

        Task<ServiceResult<RiskResultDto>> GetLatestRiskAssessmentAsync(string userId);

        Task<ServiceResult<IEnumerable<RiskResultDto>>> GetRiskHistoryAsync(string userId);
    }
}
