using Etmen_BLL.DTOs.CriticalIntelligence;
using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class CriticalIntelligenceService : ICriticalIntelligenceService
    {
        private static readonly EmergencyRequestStatus[] ActiveStatuses =
        [
            EmergencyRequestStatus.Pending,
            EmergencyRequestStatus.Accepted,
            EmergencyRequestStatus.Escalated
        ];

        private readonly IUnitOfWork _uow;

        public CriticalIntelligenceService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<CriticalCommandCenterDto>> GetCommandCenterAsync(bool includeResolved = false, int take = 100)
        {
            take = Math.Clamp(take, 1, 500);
            var cases = await LoadCriticalCasesQuery(includeResolved)
                .OrderByDescending(e => e.PriorityScore)
                .ThenBy(e => e.RequestedAt)
                .Take(take)
                .ToListAsync();

            var doctorIds = await GetDoctorUserIdsAsync();
            var items = new List<CriticalCommandCenterItemDto>();

            foreach (var item in cases)
            {
                var conversation = await GetDoctorConversationStateAsync(item.PatientProfile.ApplicationUserId, doctorIds);
                items.Add(new CriticalCommandCenterItemDto
                {
                    EmergencyRequestId = item.Id,
                    PatientProfileId = item.PatientProfileId,
                    PatientName = item.PatientProfile.FullName ?? "Unknown patient",
                    PatientPhone = item.PatientProfile.ApplicationUser?.PhoneNumber,
                    RiskScore = item.RiskAssessment?.RiskScore ?? 0,
                    RiskLevel = item.RiskAssessment?.RiskLevel ?? RiskLevel.Emergency,
                    Symptoms = item.RiskAssessment?.Symptoms,
                    EmergencyStatus = item.Status,
                    PriorityScore = item.PriorityScore,
                    WaitingMinutes = WaitingMinutes(item.RequestedAt),
                    HospitalResponded = item.AcceptedAt.HasValue || item.Status is EmergencyRequestStatus.Accepted or EmergencyRequestStatus.Completed,
                    AssignedProviderName = item.HealthcareProvider?.Name,
                    AssignedDoctorUserId = item.AssignedDoctorUserId,
                    AssignedDoctorName = item.AssignedDoctor is null ? null : BuildUserName(item.AssignedDoctor),
                    DoctorAssignedAt = item.DoctorAssignedAt,
                    HasDoctorConversation = conversation.HasConversation,
                    LastDoctorMessageAt = conversation.LastMessageAt,
                    OperationalStatus = BuildOperationalStatus(item, conversation.HasConversation)
                });
            }

            var waitingValues = items.Where(i => !i.HospitalResponded).Select(i => i.WaitingMinutes).ToList();
            var dto = new CriticalCommandCenterDto
            {
                ActiveCriticalCases = items.Count,
                WaitingForHospital = items.Count(i => !i.HospitalResponded),
                HospitalAccepted = items.Count(i => i.HospitalResponded),
                WaitingForDoctor = items.Count(i => string.IsNullOrWhiteSpace(i.AssignedDoctorUserId) && !i.HasDoctorConversation),
                DoctorAssigned = items.Count(i => !string.IsNullOrWhiteSpace(i.AssignedDoctorUserId)),
                AverageWaitingMinutes = waitingValues.Count == 0 ? 0 : Math.Round((decimal)waitingValues.Average(), 2),
                GeneratedAt = DateTime.UtcNow,
                Cases = items
            };

            return ServiceResult<CriticalCommandCenterDto>.Success(dto);
        }

        public async Task<ServiceResult<DoctorPanicInboxDto>> GetDoctorPanicInboxAsync(string doctorUserId)
        {
            if (string.IsNullOrWhiteSpace(doctorUserId))
                return ServiceResult<DoctorPanicInboxDto>.Failure("Doctor user id is required.");

            var doctor = await _uow.DoctorProfiles.Table
                .FirstOrDefaultAsync(d => d.ApplicationUserId == doctorUserId);
            if (doctor is null)
                return ServiceResult<DoctorPanicInboxDto>.NotFound("Doctor profile was not found.");

            var cases = await LoadCriticalCasesQuery(false)
                .Where(e => e.AssignedDoctorUserId == null || e.AssignedDoctorUserId == doctorUserId)
                .OrderByDescending(e => e.AssignedDoctorUserId == doctorUserId)
                .ThenByDescending(e => e.PriorityScore)
                .ThenBy(e => e.RequestedAt)
                .Take(100)
                .ToListAsync();

            var items = new List<DoctorPanicInboxItemDto>();
            foreach (var emergency in cases)
            {
                var hasConversation = await _uow.ChatMessages.AnyAsync(m =>
                    m.SenderId == doctorUserId && m.ReceiverId == emergency.PatientProfile.ApplicationUserId ||
                    m.ReceiverId == doctorUserId && m.SenderId == emergency.PatientProfile.ApplicationUserId);

                items.Add(new DoctorPanicInboxItemDto
                {
                    EmergencyRequestId = emergency.Id,
                    PatientProfileId = emergency.PatientProfileId,
                    PatientName = emergency.PatientProfile.FullName ?? "Unknown patient",
                    RiskScore = emergency.RiskAssessment?.RiskScore ?? 0,
                    RiskLevel = emergency.RiskAssessment?.RiskLevel ?? RiskLevel.Emergency,
                    Symptoms = emergency.RiskAssessment?.Symptoms,
                    PriorityScore = emergency.PriorityScore,
                    IsAssignedToCurrentDoctor = emergency.AssignedDoctorUserId == doctorUserId,
                    HasConversation = hasConversation,
                    RequestedAt = emergency.RequestedAt,
                    SuggestedFirstMessage = BuildSuggestedFirstMessage(emergency)
                });
            }

            var dto = new DoctorPanicInboxDto
            {
                DoctorUserId = doctorUserId,
                DoctorName = doctor.FullName ?? "Doctor",
                TotalCriticalCases = items.Count,
                AssignedToDoctor = items.Count(i => i.IsAssignedToCurrentDoctor),
                UnassignedCriticalCases = items.Count(i => !i.IsAssignedToCurrentDoctor),
                Items = items
            };

            return ServiceResult<DoctorPanicInboxDto>.Success(dto);
        }

        public async Task<ServiceResult<DoctorAssignmentDto>> AssignBestDoctorAsync(int emergencyRequestId)
        {
            var emergency = await LoadCriticalCasesQuery(true)
                .FirstOrDefaultAsync(e => e.Id == emergencyRequestId);
            if (emergency is null)
                return ServiceResult<DoctorAssignmentDto>.NotFound("Critical emergency request was not found.");

            if (!string.IsNullOrWhiteSpace(emergency.AssignedDoctorUserId))
            {
                var assigned = await _uow.DoctorProfiles.Table.FirstOrDefaultAsync(d => d.ApplicationUserId == emergency.AssignedDoctorUserId);
                return ServiceResult<DoctorAssignmentDto>.Success(new DoctorAssignmentDto
                {
                    EmergencyRequestId = emergency.Id,
                    DoctorUserId = emergency.AssignedDoctorUserId,
                    DoctorProfileId = assigned?.Id ?? 0,
                    DoctorName = assigned?.FullName ?? "Assigned doctor",
                    Specialization = assigned?.Specialization,
                    MatchScore = 100,
                    AssignedAt = emergency.DoctorAssignedAt ?? DateTime.UtcNow,
                    Reason = "Doctor was already assigned."
                });
            }

            var doctors = await _uow.DoctorProfiles.Table
                .Include(d => d.ApplicationUser)
                .Where(d => d.IsAvailable)
                .ToListAsync();
            if (doctors.Count == 0)
                return ServiceResult<DoctorAssignmentDto>.NotFound("No available doctors were found.");

            var best = doctors
                .Select(d => new { Doctor = d, Score = ScoreDoctor(d, emergency) })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Doctor.FullName)
                .First();

            emergency.AssignedDoctorUserId = best.Doctor.ApplicationUserId;
            emergency.DoctorAssignedAt = DateTime.UtcNow;
            _uow.EmergencyRequests.Update(emergency);

            await AddNotificationAsync(
                best.Doctor.ApplicationUserId,
                "Critical case assigned",
                $"{emergency.PatientProfile.FullName ?? "A patient"} has been assigned to you for urgent review.",
                $"/Doctor/Patients/Detail/{emergency.PatientProfileId}");

            await AddAlertAsync(
                best.Doctor.ApplicationUserId,
                "Critical case assigned",
                $"{emergency.PatientProfile.FullName ?? "A patient"} needs urgent review. Risk: {emergency.RiskAssessment?.RiskLevel}.",
                "DoctorAssignment");

            await _uow.CompleteAsync();

            return ServiceResult<DoctorAssignmentDto>.Success(new DoctorAssignmentDto
            {
                EmergencyRequestId = emergency.Id,
                DoctorUserId = best.Doctor.ApplicationUserId,
                DoctorProfileId = best.Doctor.Id,
                DoctorName = best.Doctor.FullName ?? "Doctor",
                Specialization = best.Doctor.Specialization,
                MatchScore = best.Score,
                AssignedAt = emergency.DoctorAssignedAt.Value,
                Reason = BuildDoctorAssignmentReason(best.Doctor, emergency)
            });
        }

        public async Task<ServiceResult<DeteriorationPredictionDto>> PredictDeteriorationAsync(int patientProfileId, int hoursWindow = 24)
        {
            if (patientProfileId <= 0)
                return ServiceResult<DeteriorationPredictionDto>.Failure("Patient profile id is required.");

            hoursWindow = Math.Clamp(hoursWindow, 1, 72);
            var patientExists = await _uow.PatientProfiles.AnyAsync(p => p.Id == patientProfileId);
            if (!patientExists)
                return ServiceResult<DeteriorationPredictionDto>.NotFound("Patient profile was not found.");

            var risks = (await _uow.RiskAssessments.GetByPatientIdAsync(patientProfileId)).Take(5).ToList();
            var latestRecord = await _uow.MedicalRecords.GetLatestByPatientIdAsync(patientProfileId);

            decimal probability = risks.FirstOrDefault()?.RiskScore ?? 0.1m;
            var reasons = new List<string>();

            if (risks.Count >= 2)
            {
                var latest = risks[0].RiskScore;
                var previous = risks[1].RiskScore;
                var delta = latest - previous;
                if (delta > 0)
                {
                    probability += Math.Min(0.25m, delta);
                    reasons.Add($"Risk score increased by {delta:P0} compared with the previous assessment.");
                }
                else
                {
                    reasons.Add("Risk score is stable or improving compared with the previous assessment.");
                }
            }

            if (latestRecord?.OxygenSaturation < 94)
            {
                probability += 0.15m;
                reasons.Add("Oxygen saturation is below the preferred safety range.");
            }

            if (latestRecord?.SystolicBP > 160 || latestRecord?.DiastolicBP > 100)
            {
                probability += 0.12m;
                reasons.Add("Blood pressure readings are materially elevated.");
            }

            if (latestRecord?.Temperature >= 38)
            {
                probability += 0.08m;
                reasons.Add("Fever is present in the latest medical record.");
            }

            probability = Math.Clamp(Math.Round(probability, 2), 0, 1);
            var predicted = RiskCalculatorHelper.GetRiskLevel(probability);

            return ServiceResult<DeteriorationPredictionDto>.Success(new DeteriorationPredictionDto
            {
                PatientProfileId = patientProfileId,
                Probability = probability,
                PredictedRiskLevel = predicted,
                HoursWindow = hoursWindow,
                Trend = risks.Count >= 2 && risks[0].RiskScore > risks[1].RiskScore ? "Worsening" : "Stable",
                Reasons = reasons.Count == 0 ? ["Insufficient recent trend data; prediction is based on latest risk only."] : reasons,
                RecommendedActions = BuildDeteriorationActions(predicted)
            });
        }

        public async Task<ServiceResult<FamilyBroadcastDto>> BroadcastFamilyEmergencyAsync(int emergencyRequestId)
        {
            var emergency = await _uow.EmergencyRequests.Table
                .Include(e => e.PatientProfile)
                .FirstOrDefaultAsync(e => e.Id == emergencyRequestId);
            if (emergency is null)
                return ServiceResult<FamilyBroadcastDto>.NotFound("Emergency request was not found.");

            var links = await _uow.FamilyLinks.Table
                .Include(f => f.PrimaryPatient)
                .Include(f => f.LinkedPatient)
                .Where(f =>
                    f.IsAccepted &&
                    (f.CanViewRisk || f.CanViewRecords) &&
                    (f.PrimaryPatientId == emergency.PatientProfileId || f.LinkedPatientId == emergency.PatientProfileId))
                .ToListAsync();

            var notified = new List<string>();
            foreach (var link in links)
            {
                var familyPatient = link.PrimaryPatientId == emergency.PatientProfileId
                    ? link.LinkedPatient
                    : link.PrimaryPatient;

                if (string.IsNullOrWhiteSpace(familyPatient.ApplicationUserId))
                    continue;

                await AddNotificationAsync(
                    familyPatient.ApplicationUserId,
                    "Family emergency update",
                    $"{emergency.PatientProfile.FullName ?? "A family member"} has a critical emergency escalation. Status: {emergency.Status}.",
                    $"/Patient/Emergency/Track/{emergency.Id}");

                notified.Add(familyPatient.ApplicationUserId);
            }

            await _uow.CompleteAsync();
            return ServiceResult<FamilyBroadcastDto>.Success(new FamilyBroadcastDto
            {
                EmergencyRequestId = emergency.Id,
                FamilyMembersNotified = notified.Distinct().Count(),
                NotifiedUserIds = notified.Distinct().ToList(),
                Message = "Family emergency broadcast completed."
            });
        }

        public async Task<ServiceResult<CrisisHeatmapDto>> GetCrisisHeatmapAsync(int? crisisId = null)
        {
            var query = _uow.EmergencyRequests.Table
                .Include(e => e.RiskAssessment)
                .Where(e => e.IsAutoGenerated && e.Latitude.HasValue && e.Longitude.HasValue);

            var emergencies = await query.ToListAsync();
            var zones = crisisId.HasValue
                ? (await _uow.OutbreakZones.GetByCrisisIdAsync(crisisId.Value)).ToList()
                : (await _uow.OutbreakZones.GetAllAsync()).ToList();

            var zoneDtos = zones.Select(zone => new CrisisHeatmapZoneDto
            {
                ZoneId = zone.Id,
                ZoneName = zone.ZoneName,
                CenterLatitude = zone.CenterLatitude,
                CenterLongitude = zone.CenterLongitude,
                RadiusInKm = zone.RadiusInKm,
                RiskLevel = zone.RiskLevel,
                CriticalCasesInside = emergencies.Count(e => IsInsideZone(e, zone))
            }).ToList();

            var dto = new CrisisHeatmapDto
            {
                CrisisId = crisisId,
                GeneratedAt = DateTime.UtcNow,
                TotalGeoTaggedCriticalCases = emergencies.Count,
                Points = emergencies.Select(e => new CrisisHeatmapPointDto
                {
                    EmergencyRequestId = e.Id,
                    Latitude = e.Latitude!.Value,
                    Longitude = e.Longitude!.Value,
                    Intensity = Math.Clamp(e.PriorityScore / 10, 1, 100),
                    Label = $"{e.RiskAssessment?.RiskLevel.ToString() ?? "Critical"} - {e.Status}"
                }).ToList(),
                Zones = zoneDtos
            };

            return ServiceResult<CrisisHeatmapDto>.Success(dto);
        }

        public async Task<ServiceResult<AiMedicalSummaryDto>> GenerateMedicalSummaryAsync(int patientProfileId)
        {
            var patient = await _uow.PatientProfiles.Table
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == patientProfileId);
            if (patient is null)
                return ServiceResult<AiMedicalSummaryDto>.NotFound("Patient profile was not found.");

            var latestRisk = await _uow.RiskAssessments.GetLatestByPatientIdAsync(patientProfileId);
            var latestRecord = await _uow.MedicalRecords.GetLatestByPatientIdAsync(patientProfileId);
            var latestLabs = (await _uow.LabResults.GetByPatientIdAsync(patientProfileId)).Take(3).ToList();

            var findings = new List<string>();
            if (latestRisk is not null)
                findings.Add($"Latest risk is {latestRisk.RiskLevel} with score {latestRisk.RiskScore:P0}.");
            if (latestRecord?.OxygenSaturation < 94)
                findings.Add($"Oxygen saturation is low at {latestRecord.OxygenSaturation}%.");
            if (latestRecord?.SystolicBP > 140 || latestRecord?.DiastolicBP > 90)
                findings.Add($"Blood pressure is elevated: {latestRecord.SystolicBP}/{latestRecord.DiastolicBP}.");
            if (patient.HasChronicDiseases)
                findings.Add($"Chronic disease notes: {patient.ChronicDiseasesNotes ?? "not specified"}.");
            if (!string.IsNullOrWhiteSpace(patient.Allergies))
                findings.Add($"Allergies: {patient.Allergies}.");

            var missing = new List<string>();
            if (latestRecord is null) missing.Add("No recent medical record found.");
            if (latestRisk is null) missing.Add("No risk assessment found.");
            if (latestLabs.Count == 0) missing.Add("No lab results found.");
            if (string.IsNullOrWhiteSpace(patient.CurrentMedications)) missing.Add("Current medications are not documented.");

            var summary = $"Patient {patient.FullName ?? "Unknown"} has " +
                          $"{(latestRisk is null ? "no recorded risk assessment" : $"{latestRisk.RiskLevel} risk ({latestRisk.RiskScore:P0})")}." +
                          $" Latest symptoms: {latestRisk?.Symptoms ?? latestRecord?.Symptoms ?? "N/A"}.";

            return ServiceResult<AiMedicalSummaryDto>.Success(new AiMedicalSummaryDto
            {
                PatientProfileId = patient.Id,
                PatientName = patient.FullName ?? "Unknown patient",
                Summary = summary,
                CriticalFindings = findings,
                MissingInformation = missing,
                SuggestedDoctorQuestions = BuildDoctorQuestions(patient, latestRisk, latestRecord),
                GeneratedAt = DateTime.UtcNow
            });
        }

        public Task<ServiceResult<ExplainableRiskDto>> ExplainRiskAsync(RiskInputDto input)
        {
            if (input is null)
                return Task.FromResult(ServiceResult<ExplainableRiskDto>.Failure("Risk input is required."));

            var (score, isEmergency, triggered) = RiskCalculatorHelper.Calculate(
                input.SystolicBP,
                input.DiastolicBP,
                input.HeartRate,
                input.Temperature,
                input.OxygenSaturation,
                input.BloodSugar,
                input.Symptoms);

            var level = RiskCalculatorHelper.GetRiskLevel(score);
            var dto = BuildExplainableRisk(input, score, level, triggered, isEmergency);
            return Task.FromResult(ServiceResult<ExplainableRiskDto>.Success(dto));
        }

        public async Task<ServiceResult<ExplainableRiskDto>> ExplainRiskAssessmentAsync(int riskAssessmentId)
        {
            var assessment = await _uow.RiskAssessments.GetByIdAsync(riskAssessmentId);
            if (assessment is null)
                return ServiceResult<ExplainableRiskDto>.NotFound("Risk assessment was not found.");

            return ServiceResult<ExplainableRiskDto>.Success(new ExplainableRiskDto
            {
                RiskScore = assessment.RiskScore,
                RiskLevel = assessment.RiskLevel,
                PlainLanguageSummary = $"The patient is classified as {assessment.RiskLevel} because the assessment score is {assessment.RiskScore:P0}.",
                Contributions = string.IsNullOrWhiteSpace(assessment.Symptoms)
                    ? []
                    : assessment.Symptoms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => new RiskContributionDto
                        {
                            Factor = "Symptom",
                            Value = s,
                            ImpactPercent = assessment.RiskLevel >= RiskLevel.High ? 30 : 15,
                            Severity = assessment.RiskLevel.ToString(),
                            Explanation = $"Reported symptom contributed to the {assessment.RiskLevel} classification."
                        })
                        .ToList(),
                ImmediateActions = RiskCalculatorHelper.GenerateRecommendations(assessment.RiskLevel, [])
            });
        }

        private IQueryable<EmergencyRequest> LoadCriticalCasesQuery(bool includeResolved)
        {
            var query = _uow.EmergencyRequests.Table
                .Include(e => e.PatientProfile)
                    .ThenInclude(p => p.ApplicationUser)
                .Include(e => e.HealthcareProvider)
                .Include(e => e.RiskAssessment)
                .Include(e => e.AssignedDoctor)
                .Where(e => e.IsAutoGenerated || e.PriorityScore > 0);

            return includeResolved
                ? query
                : query.Where(e => e.Status != EmergencyRequestStatus.Completed && e.Status != EmergencyRequestStatus.Cancelled);
        }

        private async Task<List<string>> GetDoctorUserIdsAsync()
            => await _uow.DoctorProfiles.Table.Select(d => d.ApplicationUserId).ToListAsync();

        private async Task<(bool HasConversation, DateTime? LastMessageAt)> GetDoctorConversationStateAsync(string patientUserId, List<string> doctorUserIds)
        {
            var message = await _uow.ChatMessages.Table
                .Where(m =>
                    doctorUserIds.Contains(m.SenderId) && m.ReceiverId == patientUserId ||
                    doctorUserIds.Contains(m.ReceiverId) && m.SenderId == patientUserId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            return (message is not null, message?.SentAt);
        }

        private static int ScoreDoctor(DoctorProfile doctor, EmergencyRequest emergency)
        {
            var score = doctor.IsAvailable ? 50 : 0;
            var specialization = doctor.Specialization?.ToLowerInvariant() ?? string.Empty;
            var symptoms = emergency.RiskAssessment?.Symptoms?.ToLowerInvariant() ?? string.Empty;
            var type = emergency.EmergencyType?.ToLowerInvariant() ?? string.Empty;

            if (specialization.Contains("emergency") || specialization.Contains("critical")) score += 30;
            if (specialization.Contains("cardio") && (symptoms.Contains("chest") || symptoms.Contains("heart"))) score += 25;
            if (specialization.Contains("pulmo") && (symptoms.Contains("breath") || type.Contains("respir"))) score += 25;
            if (doctor.YearsOfExperience >= 5) score += 10;
            if (doctor.YearsOfExperience >= 10) score += 10;

            return Math.Clamp(score, 0, 100);
        }

        private static string BuildDoctorAssignmentReason(DoctorProfile doctor, EmergencyRequest emergency)
            => $"{doctor.FullName ?? "Doctor"} was selected with specialization '{doctor.Specialization ?? "N/A"}' for a {emergency.RiskAssessment?.RiskLevel ?? RiskLevel.Emergency} critical case.";

        private static string BuildOperationalStatus(EmergencyRequest item, bool hasDoctorConversation)
        {
            if (item.Status == EmergencyRequestStatus.Completed) return "Resolved";
            if (!item.AcceptedAt.HasValue && string.IsNullOrWhiteSpace(item.AssignedDoctorUserId)) return "Waiting for hospital and doctor";
            if (!item.AcceptedAt.HasValue) return "Doctor assigned, waiting for hospital";
            if (!hasDoctorConversation && string.IsNullOrWhiteSpace(item.AssignedDoctorUserId)) return "Hospital accepted, waiting for doctor";
            return "Care team engaged";
        }

        private static int WaitingMinutes(DateTime requestedAt)
            => (int)Math.Max(0, (DateTime.UtcNow - requestedAt).TotalMinutes);

        private static string BuildUserName(ApplicationUser user)
            => string.Join(" ", new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim() is var full && !string.IsNullOrWhiteSpace(full)
                ? full
                : user.UserName ?? user.Email ?? user.Id;

        private static string BuildSuggestedFirstMessage(EmergencyRequest emergency)
            => $"Hello, this is Dr. [Name] from Etmen. Your recent assessment showed {emergency.RiskAssessment?.RiskLevel} risk. Are you currently safe, conscious, and able to describe your symptoms?";

        private static List<string> BuildDeteriorationActions(RiskLevel level) => level switch
        {
            RiskLevel.Emergency => ["Immediate emergency review.", "Keep patient under direct observation.", "Confirm oxygen saturation and consciousness."],
            RiskLevel.High => ["Doctor review within hours.", "Repeat vitals soon.", "Prepare emergency escalation if symptoms worsen."],
            RiskLevel.Medium => ["Repeat assessment within 24 hours.", "Monitor symptoms and medication adherence."],
            _ => ["Continue routine monitoring."]
        };

        private static bool IsInsideZone(EmergencyRequest emergency, OutbreakZone zone)
        {
            if (!emergency.Latitude.HasValue || !emergency.Longitude.HasValue || zone.RadiusInKm <= 0)
                return false;

            var distance = GeoHelper.CalculateDistanceKm(
                (double)emergency.Latitude.Value,
                (double)emergency.Longitude.Value,
                (double)zone.CenterLatitude,
                (double)zone.CenterLongitude);

            return distance <= (double)zone.RadiusInKm;
        }

        private static List<string> BuildDoctorQuestions(PatientProfile patient, RiskAssessment? risk, MedicalRecord? record)
        {
            var questions = new List<string>
            {
                "Are symptoms improving, stable, or worsening right now?",
                "When did the current symptoms start?",
                "Is the patient alone or accompanied?"
            };

            if (record?.OxygenSaturation < 94) questions.Add("Can oxygen saturation be repeated and confirmed?");
            if (patient.HasChronicDiseases) questions.Add("Have chronic-disease medications been taken today?");
            if (risk?.RiskLevel >= RiskLevel.High) questions.Add("Does the patient have chest pain, shortness of breath, fainting, or confusion?");
            return questions;
        }

        private static ExplainableRiskDto BuildExplainableRisk(RiskInputDto input, decimal score, RiskLevel level, List<string> triggered, bool isEmergency)
        {
            var contributions = new List<RiskContributionDto>();
            AddVital(contributions, "Systolic BP", input.SystolicBP, 140, true, "Elevated systolic pressure increases cardiovascular risk.");
            AddVital(contributions, "Diastolic BP", input.DiastolicBP, 90, true, "Elevated diastolic pressure increases cardiovascular risk.");
            AddVital(contributions, "Heart Rate", input.HeartRate, 100, true, "High heart rate may indicate distress, fever, dehydration, or cardiac strain.");
            AddVital(contributions, "Temperature", input.Temperature, 37.5m, true, "Fever can indicate infection or inflammatory stress.");
            AddVital(contributions, "Oxygen Saturation", input.OxygenSaturation, 95, false, "Low oxygen saturation is a strong emergency warning sign.");
            AddVital(contributions, "Blood Sugar", input.BloodSugar, 180, true, "Abnormal blood sugar can worsen acute illness.");

            foreach (var factor in triggered.Where(f => f.Contains("عرض") || f.Contains("symptom", StringComparison.OrdinalIgnoreCase)))
            {
                contributions.Add(new RiskContributionDto
                {
                    Factor = "Symptom",
                    Value = factor,
                    ImpactPercent = 25,
                    Severity = level >= RiskLevel.High ? "High" : "Medium",
                    Explanation = "Reported symptom matched a known risk pattern."
                });
            }

            return new ExplainableRiskDto
            {
                RiskScore = score,
                RiskLevel = level,
                PlainLanguageSummary = isEmergency
                    ? "The score is critical because one or more readings or symptoms indicate immediate risk."
                    : $"The score is {level} based on the submitted vitals and symptoms.",
                Contributions = contributions.OrderByDescending(c => c.ImpactPercent).ToList(),
                ImmediateActions = RiskCalculatorHelper.GenerateRecommendations(level, triggered)
            };
        }

        private static void AddVital(List<RiskContributionDto> items, string name, decimal? value, decimal threshold, bool highIsBad, string explanation)
        {
            if (!value.HasValue)
                return;

            var abnormal = highIsBad ? value.Value > threshold : value.Value < threshold;
            if (!abnormal)
                return;

            var deviation = highIsBad
                ? Math.Min(1, (value.Value - threshold) / threshold)
                : Math.Min(1, (threshold - value.Value) / threshold);

            items.Add(new RiskContributionDto
            {
                Factor = name,
                Value = value.Value.ToString("0.##"),
                ImpactPercent = Math.Clamp((int)Math.Round(deviation * 100), 10, 40),
                Severity = deviation > 0.25m ? "High" : "Medium",
                Explanation = explanation
            });
        }

        private async Task AddNotificationAsync(string userId, string title, string message, string link)
            => await _uow.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Link = link,
                CreatedAt = DateTime.UtcNow
            });

        private async Task AddAlertAsync(string userId, string title, string message, string type)
            => await _uow.Alerts.AddAsync(new Alert
            {
                UserId = userId,
                Title = title,
                Message = message,
                AlertType = type,
                Status = AlertStatus.Unread,
                CreatedAt = DateTime.UtcNow
            });
    }
}
