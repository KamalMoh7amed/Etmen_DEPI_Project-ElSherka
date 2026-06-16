using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class RiskService : IRiskService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICriticalCareEscalationService _criticalCareEscalationService;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;
        private readonly ILogger<RiskService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;

        public RiskService(
            IUnitOfWork uow,
            ICriticalCareEscalationService criticalCareEscalationService,
            IEmailService emailService,
            IPdfReportService pdfService,
            ILogger<RiskService> logger,
            IBackgroundTaskQueue taskQueue)
        {
            _uow = uow;
            _criticalCareEscalationService = criticalCareEscalationService;
            _emailService = emailService;
            _pdfService   = pdfService;
            _logger       = logger;
            _taskQueue    = taskQueue;
        }

        public async Task<ServiceResult<RiskResultDto>> CalculateRiskAsync(RiskInputDto dto)
        {
            if (dto is null)
                return ServiceResult<RiskResultDto>.Failure("بيانات الإدخال مطلوبة.");

            // Check if crisis mode is active
            var activeCrisis = await _uow.CrisisConfigurations.Table
                .Include(c => c.SymptomWeights)
                .FirstOrDefaultAsync(c => c.IsActive && c.SystemMode == SystemMode.Crisis);

            if (activeCrisis != null)
            {
                // Crisis Mode Risk Calculation: Calculate purely based on symptoms configured for this crisis
                decimal riskScore = 0;
                bool isEmergency = false;
                var triggeredFactors = new List<string>();

                // Parse selected symptoms
                var selectedSymptoms = dto.Symptoms?.Split(new[] { ',', '،', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(s => s.Trim().ToLower())
                                                   .ToList() ?? new List<string>();

                foreach (var symptomWeight in activeCrisis.SymptomWeights)
                {
                    if (selectedSymptoms.Any(s => s.Contains(symptomWeight.SymptomName.ToLower()) || symptomWeight.SymptomName.ToLower().Contains(s)))
                    {
                        riskScore += symptomWeight.Weight;
                        triggeredFactors.Add($"عرض الأزمة: {symptomWeight.SymptomName} (الوزن: {symptomWeight.Weight})");
                        if (symptomWeight.IsEmergencySymptom)
                        {
                            isEmergency = true;
                        }
                    }
                }

                riskScore = Math.Min(riskScore, 1.0m);
                if (isEmergency && riskScore < activeCrisis.EmergencyThreshold)
                {
                    riskScore = Math.Max(riskScore, activeCrisis.EmergencyThreshold);
                }

                var riskLevel = riskScore >= activeCrisis.EmergencyThreshold
                    ? RiskLevel.Emergency
                    : riskScore >= activeCrisis.HighRiskThreshold
                        ? RiskLevel.High
                        : riskScore >= activeCrisis.MediumRiskThreshold
                            ? RiskLevel.Medium
                            : RiskLevel.Low;

                var recs = RiskCalculatorHelper.GenerateRecommendations(riskLevel, triggeredFactors);

                return ServiceResult<RiskResultDto>.Success(new RiskResultDto
                {
                    RiskScore = riskScore,
                    RiskLevel = riskLevel,
                    RiskColor = RiskLevelMapper.ToColor(riskLevel),
                    RiskLabel = RiskLevelMapper.ToArabicLabel(riskLevel),
                    IsEmergency = isEmergency || riskLevel == RiskLevel.Emergency,
                    Recommendations = recs,
                    TriggeredSymptoms = triggeredFactors
                });
            }

            // --- Normal Mode Risk Calculation ---
            // Validate required vital signs
            var missingFields = new List<string>();
            if (!dto.HeartRate.HasValue)
                missingFields.Add("معدل نبضات القلب");
            if (!dto.SystolicBP.HasValue)
                missingFields.Add("ضغط الدم الانقباضي");
            if (!dto.DiastolicBP.HasValue)
                missingFields.Add("ضغط الدم الانبساطي");
            if (!dto.Temperature.HasValue)
                missingFields.Add("درجة الحرارة");
            if (!dto.OxygenSaturation.HasValue)
                missingFields.Add("نسبة تشبّع الأكسجين");
            if (!dto.BloodSugar.HasValue)
                missingFields.Add("مستوى السكر في الدم");

            if (missingFields.Count > 0)
            {
                var fields = string.Join("، ", missingFields);
                return ServiceResult<RiskResultDto>.Failure($"يرجى إدخال الحقول التالية: {fields}");
            }

            // Validate vital sign ranges
            var hr = dto.HeartRate.GetValueOrDefault();
            var sbp = dto.SystolicBP.GetValueOrDefault();
            var dbp = dto.DiastolicBP.GetValueOrDefault();
            var temp = dto.Temperature.GetValueOrDefault();
            var spo2 = dto.OxygenSaturation.GetValueOrDefault();
            var bs = dto.BloodSugar.GetValueOrDefault();

            var invalidFields = new List<string>();
            if (hr < 20 || hr > 300)
                invalidFields.Add("معدل نبضات القلب غير صحيح (يجب أن يكون بين 20 و 300)");
            if (sbp < 40 || sbp > 300)
                invalidFields.Add("ضغط الدم الانقباضي غير صحيح (يجب أن يكون بين 40 و 300)");
            if (dbp < 20 || dbp > 200)
                invalidFields.Add("ضغط الدم الانبساطي غير صحيح (يجب أن يكون بين 20 و 200)");
            if (temp < 30 || temp > 45)
                invalidFields.Add("درجة الحرارة غير صحيحة (يجب أن تكون بين 30 و 45 درجة مئوية)");
            if (spo2 < 30 || spo2 > 100)
                invalidFields.Add("نسبة الأكسجين غير صحيحة (يجب أن تكون بين 30% و 100%)");
            if (bs < 20 || bs > 600)
                invalidFields.Add("مستوى السكر غير صحيح (يجب أن يكون بين 20 و 600 mg/dL)");

            if (invalidFields.Count > 0)
            {
                var fields = string.Join("، ", invalidFields);
                return ServiceResult<RiskResultDto>.Failure($"بيانات غير صالحة: {fields}");
            }

            // Validate blood pressure logic (systolic must be higher than diastolic)
            if (sbp <= dbp)
            {
                return ServiceResult<RiskResultDto>.Failure("خطأ في بيانات ضغط الدم: الضغط الانقباضي (العليا) يجب أن يكون أعلى من الانبساطي (السفلي).");
            }

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating risk score");
                return ServiceResult<RiskResultDto>.Failure("حدث خطأ أثناء حساب تقييم المخاطر. يرجى التحقق من صحة البيانات المدخلة.");
            }
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

            var patient = await _uow.PatientProfiles.Table
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == patientProfileId);
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

            riskResult.Id = assessment.Id;

            var escalationResult = await _criticalCareEscalationService.EscalateIfNeededAsync(patient, assessment, new RiskInputDto
            {
                Symptoms = assessment.Symptoms
            });

            // ── Send risk alert emails for High / Emergency levels (queued background task) ─────
            var alertLevels = new[] { RiskLevel.High, RiskLevel.Emergency };
            if (alertLevels.Contains(assessment.RiskLevel))
            {
                await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
                {
                    try
                    {
                        var patientEmail = patient.ApplicationUser?.Email;
                        var patientName  = patient.FullName ?? "المريض";

                        // Generate risk PDF report
                        var pdfBytes = await _pdfService.GenerateRiskReportPdfAsync(
                            patientName, riskResult.RiskLevel.ToString(),
                            riskResult.RiskScore, riskResult.Recommendations,
                            riskResult.TriggeredSymptoms,
                            assessment.AssessmentDate, assessment.IsEmergency);

                        // Alert the patient
                        if (!string.IsNullOrWhiteSpace(patientEmail))
                        {
                            await _emailService.SendRiskAlertEmailAsync(
                                patientEmail, patientName, patientName,
                                riskResult.RiskLabel ?? riskResult.RiskLevel.ToString(),
                                riskResult.RiskScore, riskResult.Recommendations,
                                isFamilyMember: false, pdfBytes);
                        }

                        // Alert all linked family members who have CanViewRisk permission
                        var familyLinks = await _uow.FamilyLinks.GetByPrimaryPatientIdAsync(patientProfileId);
                        foreach (var link in familyLinks.Where(fl => fl.IsAccepted && fl.CanViewRisk))
                        {
                            var linkedPatient      = await _uow.PatientProfiles.GetByIdAsync(link.LinkedPatientId);
                            var familyMemberEmail  = linkedPatient?.ApplicationUser?.Email;
                            var familyMemberName   = linkedPatient?.FullName ?? "فرد من العائلة";

                            if (!string.IsNullOrWhiteSpace(familyMemberEmail))
                            {
                                await _emailService.SendRiskAlertEmailAsync(
                                    familyMemberEmail, familyMemberName, patientName,
                                    riskResult.RiskLabel ?? riskResult.RiskLevel.ToString(),
                                    riskResult.RiskScore, riskResult.Recommendations,
                                    isFamilyMember: true, pdfBytes);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send risk alert emails for patient {PatientId}", patientProfileId);
                    }
                });
            }

            return escalationResult.IsSuccess
                ? ServiceResult.Success(201)
                : ServiceResult.Failure(escalationResult.ErrorMessage ?? "Risk was saved, but automatic escalation failed.");
        }

        public async Task<ServiceResult<RiskResultDto>> GetRiskAssessmentByIdAsync(int assessmentId)
        {
            if (assessmentId <= 0)
                return ServiceResult<RiskResultDto>.Failure("Invalid assessment ID.");

            var assessment = await _uow.RiskAssessments.GetByIdAsync(assessmentId);
            if (assessment == null)
                return ServiceResult<RiskResultDto>.NotFound("Risk assessment not found.");

            return ServiceResult<RiskResultDto>.Success(Map(assessment));
        }

        private static RiskResultDto Map(RiskAssessment assessment)
        {
            var recommendations = string.IsNullOrWhiteSpace(assessment.RecommendationsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(assessment.RecommendationsJson) ?? new List<string>();

            return new RiskResultDto
            {
                Id = assessment.Id,
                RiskScore = assessment.RiskScore,
                RiskLevel = assessment.RiskLevel,
                RiskColor = RiskLevelMapper.ToColor(assessment.RiskLevel),
                RiskLabel = RiskLevelMapper.ToArabicLabel(assessment.RiskLevel),
                IsEmergency = assessment.IsEmergency,
                Recommendations = recommendations,
                TriggeredSymptoms = string.IsNullOrWhiteSpace(assessment.Symptoms)
                    ? new List<string>()
                    : assessment.Symptoms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                AssessmentDate = assessment.AssessmentDate
            };
        }
    }
}
