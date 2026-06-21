
namespace Etmen_BLL.Mapping
{
    
    public class BLLMappingProfile : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            RegisterPatientMappings(config);
            RegisterDoctorMappings(config);
            RegisterAppointmentMappings(config);
            RegisterMedicalRecordMappings(config);
            RegisterAlertMappings(config);
            RegisterLabResultMappings(config);
            RegisterEmergencyMappings(config);
            RegisterFamilyMappings(config);
            RegisterRiskAssessmentMappings(config);
            RegisterSlotAndProviderMappings(config);
            RegisterNotificationMappings(config);
        }

        // ── Patient ───────────────────────────────────────────────────────────────

        private static void RegisterPatientMappings(TypeAdapterConfig config)
        {
            // Entity → DTO
            config.NewConfig<PatientProfile, ProfileDto>()
                .Map(dest => dest.FullName,                src => src.FullName ?? string.Empty)
                .Map(dest => dest.DateOfBirth,             src => src.DateOfBirth)
                .Map(dest => dest.Gender,                  src => src.Gender)
                .Map(dest => dest.Height,                  src => src.Height)
                .Map(dest => dest.Weight,                  src => src.Weight)
                .Map(dest => dest.ActivityLevel,           src => src.ActivityLevel)
                .Map(dest => dest.BloodType,               src => src.BloodType)
                .Map(dest => dest.HasChronicDiseases,      src => src.HasChronicDiseases)
                .Map(dest => dest.ChronicDiseasesNotes,    src => src.ChronicDiseasesNotes)
                .Map(dest => dest.Allergies,               src => src.Allergies)
                .Map(dest => dest.CurrentMedications,      src => src.CurrentMedications);

            // DTO → Entity (for update)
            config.NewConfig<ProfileDto, PatientProfile>()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.ApplicationUserId)
                .Ignore(dest => dest.ApplicationUser)
                .Ignore(dest => dest.MedicalRecords)
                .Ignore(dest => dest.RiskAssessments)
                .Ignore(dest => dest.Appointments)
                .Ignore(dest => dest.LabResults)
                .Ignore(dest => dest.PrimaryLinks)
                .Ignore(dest => dest.LinkedLinks)
                .Ignore(dest => dest.EmergencyRequests)
                .Ignore(dest => dest.CreatedAt);
        }

        //  Doctor 

        private static void RegisterDoctorMappings(TypeAdapterConfig config)
        {
            // Entity → DTO (requires navigation property ApplicationUser to be loaded)
            config.NewConfig<DoctorProfile, DoctorProfileDto>()
                .Map(dest => dest.Id,                  src => src.Id)
                .Map(dest => dest.FullName,            src => src.FullName ?? string.Empty)
                .Map(dest => dest.Specialization,      src => src.Specialization)
                .Map(dest => dest.LicenseNumber,       src => src.LicenseNumber)
                .Map(dest => dest.YearsOfExperience,   src => src.YearsOfExperience)
                .Map(dest => dest.Bio,                 src => src.Bio)
                .Map(dest => dest.ConsultationFee,     src => src.ConsultationFee)
                .Map(dest => dest.IsAvailable,         src => src.IsAvailable)
                .Map(dest => dest.Email,               src => src.ApplicationUser != null ? src.ApplicationUser.Email : null)
                .Map(dest => dest.PhoneNumber,         src => src.ApplicationUser != null ? src.ApplicationUser.PhoneNumber : null)
                .Map(dest => dest.IsOnboarded,         src => src.IsOnboarded)
                .Map(dest => dest.OnboardingDataJson,  src => src.OnboardingDataJson)
                .Map(dest => dest.CreatedAt,           src => src.CreatedAt)
                .Map(dest => dest.UpdatedAt,           src => src.UpdatedAt);
        }

        //  Appointment 

        private static void RegisterAppointmentMappings(TypeAdapterConfig config)
        {
            // Entity → Nearby AppointmentDto
            config.NewConfig<Appointment, AppointmentDto>()
                .Map(dest => dest.Id,          src => src.Id)
                .Map(dest => dest.PatientId,   src => src.PatientProfileId)
                .Map(dest => dest.DoctorId,    src => src.DoctorProfileId ?? 0)
                .Map(dest => dest.Date,        src => src.AppointmentDate)
                .Map(dest => dest.StartTime,   src => src.StartTime)
                .Map(dest => dest.Status,      src => src.Status.ToString())
                .Map(dest => dest.Notes,       src => src.Notes)
                .Map(dest => dest.DoctorName,           src => src.DoctorProfile != null ? src.DoctorProfile.FullName ?? string.Empty : "غير محدد")
                .Map(dest => dest.DoctorSpecialization, src => src.DoctorProfile != null ? src.DoctorProfile.Specialization ?? string.Empty : string.Empty);

            // Entity → DoctorAppointmentDto (requires eager-loaded navigation props)
            config.NewConfig<Appointment, DoctorAppointmentDto>()
                .Map(dest => dest.Id,              src => src.Id)
                .Map(dest => dest.PatientId,       src => src.PatientProfileId)
                .Map(dest => dest.PatientName,     src => src.PatientProfile != null ? src.PatientProfile.FullName ?? string.Empty : string.Empty)
                .Map(dest => dest.PatientPhone,    src => src.PatientProfile != null && src.PatientProfile.ApplicationUser != null ? src.PatientProfile.ApplicationUser.PhoneNumber : null)
                .Map(dest => dest.PatientEmail,    src => src.PatientProfile != null && src.PatientProfile.ApplicationUser != null ? src.PatientProfile.ApplicationUser.Email : null)
                .Map(dest => dest.AppointmentDate, src => src.AppointmentDate)
                .Map(dest => dest.StartTime,       src => src.StartTime)
                .Map(dest => dest.EndTime,         src => src.EndTime)
                .Map(dest => dest.Status,          src => src.Status.ToString())
                .Map(dest => dest.Notes,           src => src.Notes)
                .Map(dest => dest.CreatedAt,       src => src.CreatedAt);

            // Entity → UpcomingAppointmentDto
            config.NewConfig<Appointment, UpcomingAppointmentDto>()
                .Map(dest => dest.Id,              src => src.Id)
                .Map(dest => dest.PatientName,     src => src.PatientProfile != null ? src.PatientProfile.FullName ?? string.Empty : string.Empty)
                .Map(dest => dest.AppointmentDate, src => src.AppointmentDate)
                .Map(dest => dest.StartTime,       src => src.StartTime)
                .Map(dest => dest.EndTime,         src => src.EndTime)
                .Map(dest => dest.Status,          src => src.Status.ToString())
                .Map(dest => dest.Notes,           src => src.Notes);

            // Entity → Patient RecentAppointmentDto
            config.NewConfig<Appointment, RecentAppointmentDto>()
                .Map(dest => dest.Id,         src => src.Id)
                .Map(dest => dest.DoctorName, src => src.DoctorProfile != null ? src.DoctorProfile.FullName ?? string.Empty : "غير محدد")
                .Map(dest => dest.Date,       src => src.AppointmentDate)
                .Map(dest => dest.Status,     src => src.Status.ToString());

            // BookingRequestDto → Appointment (create)
            config.NewConfig<BookingRequestDto, Appointment>()
                .Map(dest => dest.DoctorProfileId,  src => src.DoctorId)
                .Map(dest => dest.AppointmentDate,  src => src.Date)
                .Map(dest => dest.StartTime,        src => src.StartTime)
                .Map(dest => dest.EndTime,          src => src.EndTime)
                .Map(dest => dest.Notes,            src => src.Notes)
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.PatientProfileId)
                .Ignore(dest => dest.PatientProfile)
                .Ignore(dest => dest.DoctorProfile!)
                .Ignore(dest => dest.Status)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.UpdatedAt!);
        }

        //  Medical Records 

        private static void RegisterMedicalRecordMappings(TypeAdapterConfig config)
        {
            // Entity → DTO
            config.NewConfig<MedicalRecord, MedicalRecordDto>()
                .Map(dest => dest.Id,                  src => src.Id)
                .Map(dest => dest.RecordDate,          src => src.RecordDate)
                .Map(dest => dest.SystolicBP,          src => src.SystolicBP)
                .Map(dest => dest.DiastolicBP,         src => src.DiastolicBP)
                .Map(dest => dest.BloodSugar,          src => src.BloodSugar)
                .Map(dest => dest.HeartRate,           src => src.HeartRate)
                .Map(dest => dest.Temperature,         src => src.Temperature)
                .Map(dest => dest.OxygenSaturation,    src => src.OxygenSaturation)
                .Map(dest => dest.Symptoms,            src => src.Symptoms)
                .Map(dest => dest.Notes,               src => src.Notes);

            // CreateDto → Entity
            config.NewConfig<MedicalRecordCreateDto, MedicalRecord>()
                .Map(dest => dest.PatientProfileId,    src => src.PatientId)
                .Map(dest => dest.RecordDate,          src => src.RecordDate)
                .Map(dest => dest.SystolicBP,          src => src.SystolicBP)
                .Map(dest => dest.DiastolicBP,         src => src.DiastolicBP)
                .Map(dest => dest.BloodSugar,          src => src.BloodSugar)
                .Map(dest => dest.HeartRate,           src => src.HeartRate)
                .Map(dest => dest.Temperature,         src => src.Temperature)
                .Map(dest => dest.OxygenSaturation,    src => src.OxygenSaturation)
                .Map(dest => dest.Symptoms,            src => src.Symptoms)
                .Map(dest => dest.Notes,               src => src.Notes)
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.PatientProfile)
                .Ignore(dest => dest.CreatedAt);
        }

        //  Alerts 

        private static void RegisterAlertMappings(TypeAdapterConfig config)
        {
            config.NewConfig<Alert, PatientAlertDto>()
                .Map(dest => dest.Id,         src => src.Id)
                .Map(dest => dest.Title,      src => src.Title)
                .Map(dest => dest.Message,    src => src.Message)
                .Map(dest => dest.Status,     src => src.Status)
                .Map(dest => dest.AlertType,  src => src.AlertType)
                .Map(dest => dest.CreatedAt,  src => src.CreatedAt);

            config.NewConfig<Alert, DTOs.Alert.RecentAlertDto>()
                .Map(dest => dest.Id,         src => src.Id)
                .Map(dest => dest.Title,      src => src.Title)
                .Map(dest => dest.CreatedAt,  src => src.CreatedAt);
        }

        //  Lab Results 

        private static void RegisterLabResultMappings(TypeAdapterConfig config)
        {
            config.NewConfig<LabResult, LabResultDto>()
                .Map(dest => dest.Id,                  src => src.Id)
                .Map(dest => dest.TestName,            src => src.TestName)
                .Map(dest => dest.TestDate,            src => src.TestDate)
                .Map(dest => dest.FilePath,            src => src.FilePath)
                .Map(dest => dest.FileUrl,             src => src.FileUrl)
                .Map(dest => dest.OcrExtractedData,    src => src.OcrExtractedData)
                .Map(dest => dest.Results,             src => src.Results)
                .Map(dest => dest.CreatedAt,           src => src.CreatedAt);
        }

        //  Emergency 

        private static void RegisterEmergencyMappings(TypeAdapterConfig config)
        {
            config.NewConfig<EmergencyRequestDto, EmergencyRequest>()
                .Map(dest => dest.PatientProfileId,    src => src.PatientProfileId)
                .Map(dest => dest.Latitude,            src => src.Latitude)
                .Map(dest => dest.Longitude,           src => src.Longitude)
                .Map(dest => dest.EmergencyType,       src => src.EmergencyType)
                .Map(dest => dest.Description,         src => src.Description)
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.HealthcareProviderId!)
                .Ignore(dest => dest.HealthcareProvider!)
                .Ignore(dest => dest.PatientProfile)
                .Ignore(dest => dest.Status)
                .Ignore(dest => dest.RequestedAt)
                .Ignore(dest => dest.AcceptedAt!)
                .Ignore(dest => dest.CompletedAt!)
                .Ignore(dest => dest.ResponseNotes!);
        }

        //  Family 

        private static void RegisterFamilyMappings(TypeAdapterConfig config)
        {
            config.NewConfig<FamilyLink, FamilyDto>()
                .Map(dest => dest.Id,                   src => src.Id)
                .Map(dest => dest.Relationship,         src => src.Relationship)
                .Map(dest => dest.IsAccepted,           src => src.IsAccepted)
                .Map(dest => dest.CanViewRecords,       src => src.CanViewRecords)
                .Map(dest => dest.CanViewRisk,          src => src.CanViewRisk)
                .Map(dest => dest.CanBookAppointments,  src => src.CanBookAppointments)
                .Map(dest => dest.LinkedPatientName,    src => src.LinkedPatient != null ? src.LinkedPatient.FullName ?? string.Empty : string.Empty)
                .Map(dest => dest.CreatedAt,            src => src.CreatedAt);
        }

        //  Risk Assessment 

        private static void RegisterRiskAssessmentMappings(TypeAdapterConfig config)
        {
            config.NewConfig<RiskAssessment, RiskResultDto>()
                .Map(dest => dest.RiskScore,    src => src.RiskScore)
                .Map(dest => dest.RiskLevel,    src => src.RiskLevel)
                .Map(dest => dest.IsEmergency,  src => src.IsEmergency)
                .Map(dest => dest.RiskColor,    src => RiskCalculatorHelper.GetRiskColor(src.RiskLevel))
                .Map(dest => dest.RiskLabel,    src => RiskCalculatorHelper.GetRiskLabel(src.RiskLevel))
                .Map(dest => dest.Recommendations, src =>
                    src.RecommendationsJson != null
                        ? JsonSerializer.Deserialize<List<string>>(src.RecommendationsJson) ?? new List<string>()
                        : new List<string>())
                .Map(dest => dest.TriggeredSymptoms,    src => new List<string>())
                .Map(dest => dest.NearestEmergencyCenter, src => (string?)null);
        }

        //  Slots & Providers 

        private static void RegisterSlotAndProviderMappings(TypeAdapterConfig config)
        {
            config.NewConfig<AvailableSlot, AvailableSlotDto>()
                .Map(dest => dest.Date,       src => src.SlotDate)
                .Map(dest => dest.StartTime,  src => src.SlotStart)
                .Map(dest => dest.EndTime,    src => src.SlotEnd);

            config.NewConfig<HealthcareProvider, ProviderDto>()
                .Map(dest => dest.Id,        src => src.Id)
                .Map(dest => dest.Name,      src => src.Name)
                .Map(dest => dest.Type,      src => src.Type)
                .Map(dest => dest.Address,   src => src.Address)
                .Map(dest => dest.Phone,     src => src.Phone)
                .Map(dest => dest.Latitude,  src => src.Latitude)
                .Map(dest => dest.Longitude, src => src.Longitude);
        }

        //  Notifications 

        private static void RegisterNotificationMappings(TypeAdapterConfig config)
        {
            config.NewConfig<Notification, NotificationDto>()
                .Map(dest => dest.Id,         src => src.Id)
                .Map(dest => dest.Title,      src => src.Title)
                .Map(dest => dest.Message,    src => src.Message)
                .Map(dest => dest.IsRead,     src => src.IsRead)
                .Map(dest => dest.Link,       src => src.Link)
                .Map(dest => dest.CreatedAt,  src => src.CreatedAt);
        }
    }
}
