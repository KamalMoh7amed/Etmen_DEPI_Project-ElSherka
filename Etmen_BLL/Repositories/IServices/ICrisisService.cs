using Etmen_BLL.DTOs.Crisis;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    /// <summary>
    /// Manages all crisis-related operations including creation, modification, deletion, and symptom management.
    /// Only Admins can perform administrative operations (Create, Update, Delete, Manage Symptoms, Activate/Deactivate).
    /// </summary>
    public interface ICrisisService
    {
        // ========== READ OPERATIONS (جميع المستخدمين) ==========
        Task<ServiceResult<CrisisConfigurationDto>> GetActiveCrisisAsync();
        Task<ServiceResult<List<CrisisConfigurationDto>>> GetAllCrisesAsync();
        Task<ServiceResult<CrisisConfigurationDto>> GetCrisisByIdAsync(int crisisId);
        Task<ServiceResult<CrisisStatsDto>> GetCrisisStatsAsync(int crisisId);

        // ========== ADMIN OPERATIONS (الادمن فقط) ==========
        // Crisis Management
        Task<ServiceResult<CrisisConfigurationDto>> CreateCrisisAsync(CreateCrisisDto dto);
        Task<ServiceResult<CrisisConfigurationDto>> UpdateCrisisAsync(int crisisId, EditCrisisDto dto);
        Task<ServiceResult> ActivateCrisisAsync(int crisisId);
        Task<ServiceResult> DeactivateCrisisAsync(int crisisId);
        Task<ServiceResult> DeleteCrisisAsync(int crisisId);

        // Symptom Management
        Task<ServiceResult> AddSymptomAsync(int crisisId, SymptomWeightDto symptomDto);
        Task<ServiceResult> AddMultipleSymptomsAsync(int crisisId, List<SymptomWeightDto> symptomsDto);
        Task<ServiceResult> UpdateSymptomAsync(int crisisId, string symptomName, SymptomWeightDto updatedSymptomDto);
        Task<ServiceResult> RemoveSymptomAsync(int crisisId, string symptomName);
        Task<ServiceResult<List<SymptomWeightDto>>> GetSymptomsByCrisisAsync(int crisisId);

        // Risk Thresholds Management
        Task<ServiceResult> UpdateRiskThresholdsAsync(int crisisId, decimal? emergencyThreshold, decimal? highRiskThreshold, decimal? mediumRiskThreshold);
    }
}
