using System;
using System.Collections.Generic;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;

namespace Etmen_PL.Models.ViewModels.Doctor
{
    public class DoctorPatientDetailsViewModel
    {
        // Patient basic profile
        public int PatientProfileId { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public decimal BMI { get; set; }
        public string BMIWithCategory { get; set; } = string.Empty;
        public string ActivityLevel { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public bool HasChronicDiseases { get; set; }
        public string ChronicDiseasesNotes { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;
        public string CurrentMedications { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Medical history lists
        public List<MedicalRecord> MedicalRecords { get; set; } = new();
        public List<RiskAssessment> RiskAssessments { get; set; } = new();
        public List<LabResult> LabResults { get; set; } = new();
        public List<EmergencyRequest> EmergencyRequests { get; set; } = new();
        public List<FamilyLink> FamilyLinks { get; set; } = new();

        // Alert stats
        public int UnreadAlertsCount { get; set; }
        public int UpcomingAppointmentsCount { get; set; }

        // Master lists for actions
        public List<HealthcareProvider> ActiveEmergencyHospitals { get; set; } = new();
    }
}
