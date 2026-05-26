using Etmen_BLL.DTOs.CriticalIntelligence;
using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    public interface ICriticalIntelligenceService
    {
        Task<ServiceResult<CriticalCommandCenterDto>> GetCommandCenterAsync(bool includeResolved = false, int take = 100);
        Task<ServiceResult<DoctorPanicInboxDto>> GetDoctorPanicInboxAsync(string doctorUserId);
        Task<ServiceResult<DoctorAssignmentDto>> AssignBestDoctorAsync(int emergencyRequestId);
        Task<ServiceResult<DeteriorationPredictionDto>> PredictDeteriorationAsync(int patientProfileId, int hoursWindow = 24);
        Task<ServiceResult<FamilyBroadcastDto>> BroadcastFamilyEmergencyAsync(int emergencyRequestId);
        Task<ServiceResult<CrisisHeatmapDto>> GetCrisisHeatmapAsync(int? crisisId = null);
        Task<ServiceResult<AiMedicalSummaryDto>> GenerateMedicalSummaryAsync(int patientProfileId);
        Task<ServiceResult<ExplainableRiskDto>> ExplainRiskAsync(RiskInputDto input);
        Task<ServiceResult<ExplainableRiskDto>> ExplainRiskAssessmentAsync(int riskAssessmentId);
    }
}
