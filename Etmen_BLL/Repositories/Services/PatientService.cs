

namespace Etmen_BLL.Repositories.Services
{
    public sealed class PatientService : IPatientService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICriticalCareEscalationService _criticalCareEscalationService;

        public PatientService(
            IUnitOfWork uow,
            ICriticalCareEscalationService criticalCareEscalationService)
        {
            _uow = uow;
            _criticalCareEscalationService = criticalCareEscalationService;
        }

        public async Task<ServiceResult<ProfileDto>> GetProfileAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<ProfileDto>.Failure("User ID is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<ProfileDto>.Failure("Patient profile not found.");

                var profileDto = patient.Adapt<ProfileDto>();
                return ServiceResult<ProfileDto>.Success(profileDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProfileDto>.Failure($"Failed to retrieve patient profile: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ProfileDto>> GetProfileByIdAsync(int patientProfileId)
        {
            try
            {
                if (patientProfileId <= 0)
                    return ServiceResult<ProfileDto>.Failure("Patient Profile ID is invalid.");

                var patient = await _uow.PatientProfiles.GetByIdAsync(patientProfileId);
                if (patient == null)
                    return ServiceResult<ProfileDto>.Failure("Patient profile not found.");

                var profileDto = patient.Adapt<ProfileDto>();
                return ServiceResult<ProfileDto>.Success(profileDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProfileDto>.Failure($"Failed to retrieve patient profile: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ProfileDto>> UpdateProfileAsync(string userId, ProfileDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<ProfileDto>.Failure("User ID is required.");

                if (dto == null)
                    return ServiceResult<ProfileDto>.Failure("Profile data is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<ProfileDto>.Failure("Patient profile not found.");

                // Update profile fields
                if (!string.IsNullOrWhiteSpace(dto.FullName))
                    patient.FullName = dto.FullName;

                if (dto.DateOfBirth.HasValue)
                    patient.DateOfBirth = dto.DateOfBirth;

                if (!string.IsNullOrWhiteSpace(dto.Gender))
                    patient.Gender = dto.Gender;

                if (dto.Height.HasValue && dto.Height > 0)
                    patient.Height = dto.Height;

                if (dto.Weight.HasValue && dto.Weight > 0)
                    patient.Weight = dto.Weight;

                patient.ActivityLevel = dto.ActivityLevel;

                if (!string.IsNullOrWhiteSpace(dto.BloodType))
                    patient.BloodType = dto.BloodType;

                patient.HasChronicDiseases = dto.HasChronicDiseases;

                if (!string.IsNullOrWhiteSpace(dto.ChronicDiseasesNotes))
                    patient.ChronicDiseasesNotes = dto.ChronicDiseasesNotes;

                if (!string.IsNullOrWhiteSpace(dto.Allergies))
                    patient.Allergies = dto.Allergies;

                if (!string.IsNullOrWhiteSpace(dto.CurrentMedications))
                    patient.CurrentMedications = dto.CurrentMedications;

                patient.UpdatedAt = DateTime.UtcNow;

                _uow.PatientProfiles.Update(patient);
                await _uow.CompleteAsync();

                var result = patient.Adapt<ProfileDto>();
                return ServiceResult<ProfileDto>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProfileDto>.Failure($"Failed to update patient profile: {ex.Message}");
            }
        }

        public async Task<ServiceResult<DashboardDto>> GetDashboardAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<DashboardDto>.Failure("User ID is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<DashboardDto>.Failure("Patient profile not found.");

                return await GetDashboardForPatientAsync(patient);
            }
            catch (Exception ex)
            {
                return ServiceResult<DashboardDto>.Failure($"Failed to retrieve dashboard: {ex.Message}");
            }
        }

        public async Task<ServiceResult<DashboardDto>> GetDashboardByProfileIdAsync(int patientProfileId)
        {
            try
            {
                if (patientProfileId <= 0)
                    return ServiceResult<DashboardDto>.Failure("Patient Profile ID is required.");

                var patient = await _uow.PatientProfiles.GetByIdAsync(patientProfileId);
                if (patient == null)
                    return ServiceResult<DashboardDto>.Failure("Patient profile not found.");

                return await GetDashboardForPatientAsync(patient);
            }
            catch (Exception ex)
            {
                return ServiceResult<DashboardDto>.Failure($"Failed to retrieve dashboard: {ex.Message}");
            }
        }

        private async Task<ServiceResult<DashboardDto>> GetDashboardForPatientAsync(PatientProfile patient)
        {
            var userId = patient.ApplicationUserId;
            // Get latest risk assessment
            var latestRisk = await _uow.RiskAssessments.GetLatestByPatientIdAsync(patient.Id);
            RiskResultDto? latestRiskDto = null;
            if (latestRisk != null)
            {
                latestRiskDto = new RiskResultDto
                {
                    RiskScore = latestRisk.RiskScore,
                    RiskLevel = latestRisk.RiskLevel,
                    RiskColor = RiskLevelMapper.ToColor(latestRisk.RiskLevel),
                    RiskLabel = RiskLevelMapper.ToArabicLabel(latestRisk.RiskLevel),
                    IsEmergency = latestRisk.IsEmergency,
                    Recommendations = latestRisk.RecommendationsJson != null
                        ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(latestRisk.RecommendationsJson) ?? new List<string>()
                        : new List<string>(),
                    TriggeredSymptoms = latestRisk.Symptoms != null
                        ? latestRisk.Symptoms.Split(',').Select(s => s.Trim()).ToList()
                        : new List<string>(),
                    AssessmentDate = latestRisk.AssessmentDate
                };
            }

            // Get unread alerts count
            var unreadAlertsCount = await _uow.Alerts.GetUnreadCountAsync(userId);

            // Get upcoming appointments
            var upcomingAppointments = await _uow.Appointments.GetUpcomingAppointmentsAsync(patient.Id);
            var appointmentDtos = upcomingAppointments
                .Select(a => new RecentAppointmentDto
                {
                    Id = a.Id,
                    DoctorName = !string.IsNullOrWhiteSpace(a.DoctorProfile?.ApplicationUser?.FirstName)
                        ? $"{a.DoctorProfile.ApplicationUser.FirstName} {a.DoctorProfile.ApplicationUser.LastName}".Trim()
                        : a.DoctorProfile?.FullName ?? "Unknown",
                    Date = a.AppointmentDate,
                    Status = a.Status.ToString()
                })
                .Take(5)
                .ToList();

            var upcomingAppointmentsCount = upcomingAppointments.Count();

            // Get latest BMI and category
            decimal? latestBmi = null;
            if (patient.Height.HasValue && patient.Weight.HasValue && patient.Height > 0)
            {
                // Height is stored in centimeters and weight in kilograms.
                latestBmi = patient.Weight.Value / ((patient.Height.Value / 100) * (patient.Height.Value / 100));
            }

            string? latestBmiCategory = null;
            if (latestBmi.HasValue)
            {
                latestBmiCategory = latestBmi.Value switch
                {
                    < 18.5m => "Underweight",
                    < 25m => "Normal",
                    < 30m => "Overweight",
                    _ => "Obese"
                };
            }

            // Get recent alerts
            var recentAlerts = await _uow.Alerts.GetByUserIdAsync(userId);
            var alertDtos = recentAlerts
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new RecentAlertDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    CreatedAt = a.CreatedAt,
                    IsRead = a.Status == AlertStatus.Read
                })
                .ToList();

            var medicalRecords = await _uow.MedicalRecords.GetByPatientIdAsync(patient.Id);
            var orderedMedicalRecords = medicalRecords
                .OrderByDescending(r => r.RecordDate)
                .ToList();
            var latestMedicalRecord = orderedMedicalRecords.FirstOrDefault();

            // Get active emergency request (Pending, Accepted, Escalated)
            var activeEmergency = await _uow.EmergencyRequests.Table
                .Include(r => r.AssignedDoctor)
                .Include(r => r.HealthcareProvider)
                .Where(r => r.PatientProfileId == patient.Id && 
                            (r.Status == EmergencyRequestStatus.Pending || 
                             r.Status == EmergencyRequestStatus.Accepted || 
                             r.Status == EmergencyRequestStatus.Escalated))
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();

            var dashboard = new DashboardDto
            {
                PatientName = patient.FullName ?? "Unknown",
                LatestRiskAssessment = latestRiskDto,
                UnreadAlertsCount = unreadAlertsCount,
                UpcomingAppointmentsCount = upcomingAppointmentsCount,
                LatestBmi = latestBmi,
                LatestBmiCategory = latestBmiCategory,
                MedicalRecordsCount = orderedMedicalRecords.Count,
                LatestMedicalRecord = latestMedicalRecord == null ? null : new MedicalRecordDto
                {
                    Id = latestMedicalRecord.Id,
                    RecordDate = latestMedicalRecord.RecordDate,
                    SystolicBP = latestMedicalRecord.SystolicBP,
                    DiastolicBP = latestMedicalRecord.DiastolicBP,
                    BloodSugar = latestMedicalRecord.BloodSugar,
                    HeartRate = latestMedicalRecord.HeartRate,
                    Temperature = latestMedicalRecord.Temperature,
                    OxygenSaturation = latestMedicalRecord.OxygenSaturation,
                    Symptoms = latestMedicalRecord.Symptoms,
                    Notes = latestMedicalRecord.Notes
                },
                UpcomingAppointments = appointmentDtos,
                RecentAlerts = alertDtos
            };

            if (activeEmergency != null)
            {
                dashboard.HasActiveEmergency = true;
                dashboard.ActiveEmergencyId = activeEmergency.Id;
                
                if (activeEmergency.AssignedDoctor != null)
                {
                    dashboard.ActiveEmergencyDoctorUserId = activeEmergency.AssignedDoctorUserId;
                    
                    var docProfile = await _uow.DoctorProfiles.Table
                        .FirstOrDefaultAsync(d => d.ApplicationUserId == activeEmergency.AssignedDoctorUserId);
                        
                    dashboard.ActiveEmergencyDoctorName = docProfile?.FullName ?? 
                        $"{activeEmergency.AssignedDoctor.FirstName} {activeEmergency.AssignedDoctor.LastName}".Trim();
                }
                
                dashboard.ActiveEmergencyHospitalName = activeEmergency.HealthcareProvider?.Name;
                dashboard.ActiveEmergencyPatientRecommendations = activeEmergency.PatientRecommendations;
                dashboard.ActiveEmergencyFamilyRecommendations = activeEmergency.FamilyRecommendations;
                dashboard.ActiveEmergencyPrescribedMedications = activeEmergency.PrescribedMedications;
            }

            return ServiceResult<DashboardDto>.Success(dashboard);
        }

        public async Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetMedicalRecordsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<IEnumerable<MedicalRecordDto>>.Failure("User ID is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<IEnumerable<MedicalRecordDto>>.Failure("Patient profile not found.");

                var records = await _uow.MedicalRecords.GetByPatientIdAsync(patient.Id);
                var recordDtos = records
                    .OrderByDescending(r => r.RecordDate)
                    .Select(r => r.Adapt<MedicalRecordDto>())
                    .ToList();

                return ServiceResult<IEnumerable<MedicalRecordDto>>.Success(recordDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<MedicalRecordDto>>.Failure($"Failed to retrieve medical records: {ex.Message}");
            }
        }

        public async Task<ServiceResult<MedicalRecordDto>> GetLatestMedicalRecordAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<MedicalRecordDto>.Failure("User ID is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<MedicalRecordDto>.Failure("Patient profile not found.");

                var record = await _uow.MedicalRecords.GetLatestByPatientIdAsync(patient.Id);
                if (record == null)
                    return ServiceResult<MedicalRecordDto>.Failure("No medical records found.");

                var recordDto = record.Adapt<MedicalRecordDto>();
                return ServiceResult<MedicalRecordDto>.Success(recordDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<MedicalRecordDto>.Failure($"Failed to retrieve latest medical record: {ex.Message}");
            }
        }

        public async Task<ServiceResult<MedicalRecordDto>> AddMedicalRecordAsync(string userId, MedicalRecordCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<MedicalRecordDto>.Failure("User ID is required.");

                if (dto == null)
                    return ServiceResult<MedicalRecordDto>.Failure("Medical record data is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<MedicalRecordDto>.Failure("Patient profile not found.");

                var record = new MedicalRecord
                {
                    PatientProfileId = patient.Id,
                    RecordDate = DateTime.UtcNow,
                    SystolicBP = dto.SystolicBP,
                    DiastolicBP = dto.DiastolicBP,
                    BloodSugar = dto.BloodSugar,
                    HeartRate = dto.HeartRate,
                    Temperature = dto.Temperature,
                    OxygenSaturation = dto.OxygenSaturation,
                    Symptoms = dto.Symptoms,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.MedicalRecords.AddAsync(record);
                await _uow.CompleteAsync();

                var recordDto = record.Adapt<MedicalRecordDto>();
                return ServiceResult<MedicalRecordDto>.Success(recordDto, 201);
            }
            catch (Exception ex)
            {
                return ServiceResult<MedicalRecordDto>.Failure($"Failed to add medical record: {ex.Message}");
            }
        }

        public async Task<ServiceResult<RiskResultDto>> AssessRiskAsync(string userId, RiskInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<RiskResultDto>.Failure("User ID is required.");

                if (input == null)
                    return ServiceResult<RiskResultDto>.Failure("Risk input data is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<RiskResultDto>.Failure("Patient profile not found.");

                // Calculate risk using RiskCalculatorHelper
                var (riskScore, isEmergency, triggeredFactors) = RiskCalculatorHelper.Calculate(
                    input.SystolicBP,
                    input.DiastolicBP,
                    input.HeartRate,
                    input.Temperature,
                    input.OxygenSaturation,
                    input.BloodSugar,
                    input.Symptoms
                );

                var riskLevel = RiskCalculatorHelper.GetRiskLevel(riskScore);
                var riskColor = RiskLevelMapper.ToColor(riskLevel);
                var riskLabel = RiskLevelMapper.ToArabicLabel(riskLevel);
                var recommendations = RiskCalculatorHelper.GenerateRecommendations(riskLevel, triggeredFactors);

                // Create risk assessment entity
                var riskAssessment = new RiskAssessment
                {
                    PatientProfileId = patient.Id,
                    AssessmentDate = DateTime.UtcNow,
                    RiskScore = riskScore,
                    RiskLevel = riskLevel,
                    Symptoms = input.Symptoms,
                    RecommendationsJson = System.Text.Json.JsonSerializer.Serialize(recommendations),
                    IsEmergency = isEmergency,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.RiskAssessments.AddAsync(riskAssessment);
                await _uow.CompleteAsync();

                var escalationResult = await _criticalCareEscalationService.EscalateIfNeededAsync(patient, riskAssessment, input);
                if (!escalationResult.IsSuccess)
                    return ServiceResult<RiskResultDto>.Failure(escalationResult.ErrorMessage ?? "Risk was saved, but automatic escalation failed.");

                var result = new RiskResultDto
                {
                    RiskScore = riskScore,
                    RiskLevel = riskLevel,
                    RiskColor = riskColor,
                    RiskLabel = riskLabel,
                    IsEmergency = isEmergency,
                    Recommendations = recommendations,
                    TriggeredSymptoms = triggeredFactors,
                    EmergencyRequestId = escalationResult.Data?.EmergencyRequestId,
                    WasAutoEscalated = escalationResult.Data?.WasEscalated ?? false,
                    EscalationMessage = escalationResult.Data?.Message,
                    AssessmentDate = riskAssessment.AssessmentDate
                };

                return ServiceResult<RiskResultDto>.Success(result, 201);
            }
            catch (Exception ex)
            {
                return ServiceResult<RiskResultDto>.Failure($"Failed to assess risk: {ex.Message}");
            }
        }

        public async Task<ServiceResult<RiskResultDto>> GetLatestRiskAssessmentAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<RiskResultDto>.Failure("User ID is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<RiskResultDto>.Failure("Patient profile not found.");

                var assessment = await _uow.RiskAssessments.GetLatestByPatientIdAsync(patient.Id);
                if (assessment == null)
                    return ServiceResult<RiskResultDto>.Failure("No risk assessments found.");

                var result = new RiskResultDto
                {
                    RiskScore = assessment.RiskScore,
                    RiskLevel = assessment.RiskLevel,
                    RiskColor = RiskLevelMapper.ToColor(assessment.RiskLevel),
                    RiskLabel = RiskLevelMapper.ToArabicLabel(assessment.RiskLevel),
                    IsEmergency = assessment.IsEmergency,
                    Recommendations = assessment.RecommendationsJson != null
                        ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(assessment.RecommendationsJson) ?? new List<string>()
                        : new List<string>(),
                    TriggeredSymptoms = assessment.Symptoms != null
                        ? assessment.Symptoms.Split(',').Select(s => s.Trim()).ToList()
                        : new List<string>(),
                    AssessmentDate = assessment.AssessmentDate
                };

                return ServiceResult<RiskResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<RiskResultDto>.Failure($"Failed to retrieve latest risk assessment: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<RiskResultDto>>> GetRiskHistoryAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<IEnumerable<RiskResultDto>>.Failure("User ID is required.");

                var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patient == null)
                    return ServiceResult<IEnumerable<RiskResultDto>>.Failure("Patient profile not found.");

                var assessments = await _uow.RiskAssessments.GetByPatientIdAsync(patient.Id);
                var results = assessments
                    .OrderByDescending(a => a.AssessmentDate)
                    .Select(a => new RiskResultDto
                    {
                        RiskScore = a.RiskScore,
                        RiskLevel = a.RiskLevel,
                        RiskColor = RiskLevelMapper.ToColor(a.RiskLevel),
                        RiskLabel = RiskLevelMapper.ToArabicLabel(a.RiskLevel),
                        IsEmergency = a.IsEmergency,
                        Recommendations = a.RecommendationsJson != null
                            ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(a.RecommendationsJson) ?? new List<string>()
                            : new List<string>(),
                        TriggeredSymptoms = a.Symptoms != null
                            ? a.Symptoms.Split(',').Select(s => s.Trim()).ToList()
                            : new List<string>(),
                        AssessmentDate = a.AssessmentDate
                    })
                    .ToList();

                return ServiceResult<IEnumerable<RiskResultDto>>.Success(results);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<RiskResultDto>>.Failure($"Failed to retrieve risk history: {ex.Message}");
            }
        }

    }
}
