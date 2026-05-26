using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using System.Text.Json;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class RiskService : IRiskService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICriticalCareEscalationService _criticalCareEscalationService;

        public RiskService(
            IUnitOfWork uow,
            ICriticalCareEscalationService criticalCareEscalationService)
        {
            _uow = uow;
            _criticalCareEscalationService = criticalCareEscalationService;
        }

        public async Task<ServiceResult<RiskResultDto>> CalculateRiskAsync(RiskInputDto dto)
        {
            if (dto is null)
                return ServiceResult<RiskResultDto>.Failure("Risk input data is required.");

            var (riskScore, isEmergency, triggeredFactors) = RiskCalculatorHelper.Calculate(
                dto.SystolicBP,
                dto.DiastolicBP,
                dto.HeartRate,
                dto.Temperature,
                dto.OxygenSaturation,
                dto.BloodSugar,
                dto.Symptoms);

            var riskLevel = RiskCalculatorHelper.GetRiskLevel(riskScore);
            return ServiceResult<RiskResultDto>.Success(new RiskResultDto
            {
                RiskScore = riskScore,
                RiskLevel = riskLevel,
                RiskColor = RiskLevelMapper.ToColor(riskLevel),
                RiskLabel = RiskLevelMapper.ToArabicLabel(riskLevel),
                IsEmergency = isEmergency,
                Recommendations = RiskCalculatorHelper.GenerateRecommendations(riskLevel, triggeredFactors),
                TriggeredSymptoms = triggeredFactors
            });
        }

        public async Task<ServiceResult<List<RiskResultDto>>> GetPatientRiskHistoryAsync(int patientProfileId)
        {
            if (patientProfileId <= 0)
                return ServiceResult<List<RiskResultDto>>.Failure("Patient profile id is required.");

            if (!await _uow.PatientProfiles.AnyAsync(p => p.Id == patientProfileId))
                return ServiceResult<List<RiskResultDto>>.NotFound("Patient profile was not found.");

            var assessments = await _uow.RiskAssessments.GetByPatientIdAsync(patientProfileId);
            return ServiceResult<List<RiskResultDto>>.Success(assessments.Select(Map).ToList());
        }

        public async Task<ServiceResult> SaveRiskAssessmentAsync(int patientProfileId, RiskResultDto riskResult)
        {
            if (patientProfileId <= 0)
                return ServiceResult.Failure("Patient profile id is required.");
            if (riskResult is null)
                return ServiceResult.Failure("Risk result is required.");

            var patient = await _uow.PatientProfiles.GetByIdAsync(patientProfileId);
            if (patient is null)
                return ServiceResult.NotFound("Patient profile was not found.");

            var assessment = new RiskAssessment
            {
                PatientProfileId = patientProfileId,
                AssessmentDate = DateTime.UtcNow,
                RiskScore = Math.Clamp(riskResult.RiskScore, 0, 1),
                RiskLevel = riskResult.RiskLevel,
                Symptoms = riskResult.TriggeredSymptoms.Count > 0
                    ? string.Join(", ", riskResult.TriggeredSymptoms)
                    : null,
                RecommendationsJson = JsonSerializer.Serialize(riskResult.Recommendations),
                IsEmergency = riskResult.IsEmergency,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.RiskAssessments.AddAsync(assessment);
            await _uow.CompleteAsync();

            var escalationResult = await _criticalCareEscalationService.EscalateIfNeededAsync(patient, assessment, new RiskInputDto
            {
                Symptoms = assessment.Symptoms
            });

            return escalationResult.IsSuccess
                ? ServiceResult.Success(201)
                : ServiceResult.Failure(escalationResult.ErrorMessage ?? "Risk was saved, but automatic escalation failed.");
        }

        private static RiskResultDto Map(RiskAssessment assessment)
        {
            var recommendations = string.IsNullOrWhiteSpace(assessment.RecommendationsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(assessment.RecommendationsJson) ?? new List<string>();

            return new RiskResultDto
            {
                RiskScore = assessment.RiskScore,
                RiskLevel = assessment.RiskLevel,
                RiskColor = RiskLevelMapper.ToColor(assessment.RiskLevel),
                RiskLabel = RiskLevelMapper.ToArabicLabel(assessment.RiskLevel),
                IsEmergency = assessment.IsEmergency,
                Recommendations = recommendations,
                TriggeredSymptoms = string.IsNullOrWhiteSpace(assessment.Symptoms)
                    ? new List<string>()
                    : assessment.Symptoms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            };
        }
    }
}
