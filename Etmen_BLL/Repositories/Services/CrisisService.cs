using Etmen_BLL.DTOs.Crisis;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class CrisisService : ICrisisService
    {
        private readonly IUnitOfWork _uow;

        public CrisisService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<CrisisConfigurationDto>> GetActiveCrisisAsync()
        {
            throw new NotImplementedException("GetActiveCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<CrisisConfigurationDto>>> GetAllCrisesAsync()
        {
            throw new NotImplementedException("GetAllCrisesAsync is not implemented yet.");
        }

        public Task<ServiceResult<CrisisConfigurationDto>> GetCrisisByIdAsync(int crisisId)
        {
            throw new NotImplementedException("GetCrisisByIdAsync is not implemented yet.");
        }

        public Task<ServiceResult<CrisisStatsDto>> GetCrisisStatsAsync(int crisisId)
        {
            throw new NotImplementedException("GetCrisisStatsAsync is not implemented yet.");
        }

        public Task<ServiceResult<CrisisConfigurationDto>> CreateCrisisAsync(CreateCrisisDto dto)
        {
            throw new NotImplementedException("CreateCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult<CrisisConfigurationDto>> UpdateCrisisAsync(int crisisId, EditCrisisDto dto)
        {
            throw new NotImplementedException("UpdateCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult> ActivateCrisisAsync(int crisisId)
        {
            throw new NotImplementedException("ActivateCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult> DeactivateCrisisAsync(int crisisId)
        {
            throw new NotImplementedException("DeactivateCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult> DeleteCrisisAsync(int crisisId)
        {
            throw new NotImplementedException("DeleteCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult> AddSymptomAsync(int crisisId, SymptomWeightDto symptomDto)
        {
            throw new NotImplementedException("AddSymptomAsync is not implemented yet.");
        }

        public Task<ServiceResult> AddMultipleSymptomsAsync(int crisisId, List<SymptomWeightDto> symptomsDto)
        {
            throw new NotImplementedException("AddMultipleSymptomsAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateSymptomAsync(int crisisId, string symptomName, SymptomWeightDto updatedSymptomDto)
        {
            throw new NotImplementedException("UpdateSymptomAsync is not implemented yet.");
        }

        public Task<ServiceResult> RemoveSymptomAsync(int crisisId, string symptomName)
        {
            throw new NotImplementedException("RemoveSymptomAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<SymptomWeightDto>>> GetSymptomsByCrisisAsync(int crisisId)
        {
            throw new NotImplementedException("GetSymptomsByCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateRiskThresholdsAsync(int crisisId, decimal? emergencyThreshold, decimal? highRiskThreshold, decimal? mediumRiskThreshold)
        {
            throw new NotImplementedException("UpdateRiskThresholdsAsync is not implemented yet.");
        }

    }
}