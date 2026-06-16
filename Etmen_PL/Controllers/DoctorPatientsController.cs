using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Doctor;
using Etmen_PL.Models.ViewModels.Patient;
using Etmen_DAL.DbContext;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Doctor Patients Controller
    /// Search patient registry, review histories, record diagnostic logs, and show AI summaries
    /// </summary>
    [Authorize(Roles = "Doctor")]
    public class DoctorPatientsController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly IPatientService _patientService;
        private readonly ICriticalIntelligenceService _criticalIntelligenceService;
        private readonly EtmenDbContext _context;
        private readonly ILogger<DoctorPatientsController> _logger;

        public DoctorPatientsController(
            IDoctorService doctorService,
            IPatientService patientService,
            ICriticalIntelligenceService criticalIntelligenceService,
            EtmenDbContext context,
            ILogger<DoctorPatientsController> logger)
        {
            _doctorService = doctorService;
            _patientService = patientService;
            _criticalIntelligenceService = criticalIntelligenceService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: /DoctorPatients/Search
        /// Displays patient search lookup page
        /// </summary>
        [HttpGet]
        public IActionResult Search()
        {
            var viewModel = new PatientSearchViewModel();
            return View(viewModel);
        }

        /// <summary>
        /// POST: /DoctorPatients/Search
        /// Returns patients matching keyword (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return PartialView("_PatientList", new List<Etmen_BLL.DTOs.Doctor.PatientSearchDto>());

            try
            {
                var searchResult = await _doctorService.SearchPatientsAsync(searchTerm);

                if (!searchResult.IsSuccess)
                {
                    _logger.LogWarning("Search failed for term: {SearchTerm}", searchTerm);
                    return PartialView("_PatientList", new List<Etmen_BLL.DTOs.Doctor.PatientSearchDto>());
                }

                return PartialView("_PatientList", searchResult.Data ?? new List<Etmen_BLL.DTOs.Doctor.PatientSearchDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with term {SearchTerm}", searchTerm);
                return PartialView("_PatientList", new List<Etmen_BLL.DTOs.Doctor.PatientSearchDto>());
            }
        }

        /// <summary>
        /// GET: /DoctorPatients/Details
        /// Renders patient clinical records, AI summary, and deterioration warnings
        /// </summary>
        [HttpGet("DoctorPatients/Details/{patientProfileId}")]
        public async Task<IActionResult> Details(int patientProfileId)
        {
            try
            {
                if (patientProfileId <= 0)
                {
                    TempData["Error"] = "معرف مريض غير صالح";
                    return RedirectToAction(nameof(Search));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                _logger.LogInformation("Doctor {UserId} accessing patient {PatientId} details", userId, patientProfileId);

                // Fetch Patient Profile with includes
                var patient = await _context.PatientProfiles
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.MedicalRecords)
                    .Include(p => p.RiskAssessments)
                    .Include(p => p.LabResults)
                    .Include(p => p.EmergencyRequests)
                    .FirstOrDefaultAsync(p => p.Id == patientProfileId);

                if (patient == null)
                {
                    _logger.LogWarning("Patient profile not found {PatientId}", patientProfileId);
                    TempData["Error"] = "Failed to load patient data";
                    return RedirectToAction(nameof(Search));
                }

                // Fetch active accepted family links
                var familyLinks = await _context.FamilyLinks
                    .Include(f => f.PrimaryPatient).ThenInclude(p => p.ApplicationUser)
                    .Include(f => f.LinkedPatient).ThenInclude(p => p.ApplicationUser)
                    .Where(f => f.IsAccepted && (f.PrimaryPatientId == patientProfileId || f.LinkedPatientId == patientProfileId))
                    .ToListAsync();

                // Fetch active emergency hospitals
                var activeHospitals = await _context.HealthcareProviders
                    .Where(h => h.IsActive && h.IsEmergencyCenter)
                    .ToListAsync();

                // Calculate age
                int age = 0;
                if (patient.DateOfBirth.HasValue)
                {
                    age = DateTime.Today.Year - patient.DateOfBirth.Value.Year;
                    if (patient.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
                }

                // Calculate BMI category
                var bmi = patient.BMI;
                string bmiCategory = bmi switch
                {
                    < 18.5m => "نقص وزن",
                    < 25.0m => "وزن طبيعي",
                    < 30.0m => "وزن زائد",
                    _ => "سمنة مفرطة"
                };

                var viewModel = new DoctorPatientDetailsViewModel
                {
                    PatientProfileId = patient.Id,
                    ApplicationUserId = patient.ApplicationUserId,
                    PatientName = patient.FullName ?? $"{patient.ApplicationUser.FirstName} {patient.ApplicationUser.LastName}".Trim(),
                    Age = age,
                    Gender = patient.Gender ?? string.Empty,
                    Height = patient.Height,
                    Weight = patient.Weight,
                    BMI = bmi,
                    BMIWithCategory = $"{bmi} ({bmiCategory})",
                    ActivityLevel = patient.ActivityLevel.ToString(),
                    BloodType = patient.BloodType ?? string.Empty,
                    HasChronicDiseases = patient.HasChronicDiseases,
                    ChronicDiseasesNotes = patient.ChronicDiseasesNotes ?? string.Empty,
                    Allergies = patient.Allergies ?? string.Empty,
                    CurrentMedications = patient.CurrentMedications ?? string.Empty,
                    Latitude = patient.Latitude,
                    Longitude = patient.Longitude,
                    MedicalRecords = patient.MedicalRecords.OrderByDescending(r => r.RecordDate).ToList(),
                    RiskAssessments = patient.RiskAssessments.OrderByDescending(r => r.AssessmentDate).ToList(),
                    LabResults = patient.LabResults.OrderByDescending(l => l.TestDate).ToList(),
                    EmergencyRequests = patient.EmergencyRequests.OrderByDescending(e => e.RequestedAt).ToList(),
                    FamilyLinks = familyLinks,
                    ActiveEmergencyHospitals = activeHospitals,
                    UnreadAlertsCount = 0,
                    UpcomingAppointmentsCount = 0
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient details for {PatientProfileId}", patientProfileId);
                TempData["Error"] = "Error loading patient data";
                return RedirectToAction(nameof(Search));
            }
        }

        /// <summary>
        /// GET: /DoctorPatients/AddMedicalRecord
        /// Show form to add medical record for patient
        /// </summary>
        [HttpGet("DoctorPatients/AddMedicalRecord/{patientProfileId}")]
        public IActionResult AddMedicalRecord(int patientProfileId)
        {
            try
            {
                if (patientProfileId <= 0)
                {
                    TempData["Error"] = "Invalid patient ID";
                    return RedirectToAction(nameof(Search));
                }

                var viewModel = new MedicalRecordCreateViewModel
                {
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading add medical record form");
                TempData["Error"] = "Error loading form";
                return RedirectToAction(nameof(Search));
            }
        }

        /// <summary>
        /// POST: /DoctorPatients/AddMedicalRecord
        /// Documents diagnosis and treatment notes for a patient
        /// </summary>
        [HttpPost("DoctorPatients/AddMedicalRecord/{patientProfileId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicalRecord(int patientProfileId, MedicalRecordCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                if (patientProfileId <= 0)
                {
                    TempData["Error"] = "Invalid patient ID";
                    return RedirectToAction(nameof(Search));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var recordDto = new Etmen_BLL.DTOs.Medical.MedicalRecordCreateDto
                {
                    PatientId = patientProfileId,
                    RecordDate = DateTime.UtcNow,
                    SystolicBP = viewModel.SystolicBP,
                    DiastolicBP = viewModel.DiastolicBP,
                    BloodSugar = viewModel.BloodSugar,
                    HeartRate = viewModel.HeartRate,
                    Temperature = viewModel.Temperature,
                    OxygenSaturation = viewModel.OxygenSaturation,
                    Symptoms = viewModel.Symptoms,
                    Notes = viewModel.Notes
                };

                var result = await _doctorService.AddMedicalRecordForPatientAsync(userId, recordDto);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to add medical record for patient {PatientId}", patientProfileId);
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error);
                    return View(viewModel);
                }

                _logger.LogInformation("Medical record added for patient {PatientId} by doctor {UserId}", 
                    patientProfileId, userId);
                TempData["Success"] = "Medical record added successfully";
                return RedirectToAction(nameof(Details), new { patientProfileId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding medical record for patient {PatientId}", patientProfileId);
                ModelState.AddModelError(string.Empty, "Error adding medical record");
                return View(viewModel);
            }
        }

        /// <summary>
        /// POST: /DoctorPatients/SendAlertToFamily/{patientProfileId}
        /// Broadcasts an urgent notification/alert to the chosen family member's dashboard
        /// </summary>
        [HttpPost("DoctorPatients/SendAlertToFamily/{patientProfileId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAlertToFamily(int patientProfileId, string recipientUserId, string alertMessage)
        {
            try
            {
                if (patientProfileId <= 0 || string.IsNullOrEmpty(recipientUserId) || string.IsNullOrEmpty(alertMessage))
                {
                    TempData["Error"] = "جميع الحقول مطلوبة لإرسال التنبيه.";
                    return RedirectToAction(nameof(Details), new { patientProfileId });
                }

                var patient = await _context.PatientProfiles.FindAsync(patientProfileId);
                var patientName = patient?.FullName ?? "المريض";

                // Add to Alerts
                var alert = new Alert
                {
                    UserId = recipientUserId,
                    Title = $"تنبيه طبي عاجل بخصوص {patientName}",
                    Message = alertMessage,
                    Status = AlertStatus.Unread,
                    AlertType = "FamilyAlert",
                    CreatedAt = DateTime.UtcNow
                };

                // Add to Notifications
                var notification = new Notification
                {
                    UserId = recipientUserId,
                    Title = $"تنبيه طبي عاجل بخصوص {patientName}",
                    Message = alertMessage,
                    IsRead = false,
                    Link = $"/FamilyLinking/Details/{patientProfileId}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Alerts.Add(alert);
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إرسال التنبيه الطبي العاجل لقريب المريض بنجاح.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending family alert for patient {PatientId}", patientProfileId);
                TempData["Error"] = "حدث خطأ أثناء إرسال التنبيه.";
            }

            return RedirectToAction(nameof(Details), new { patientProfileId });
        }

        /// <summary>
        /// POST: /DoctorPatients/TransferPatient/{patientProfileId}
        /// Refers patient immediately to emergency hospital room
        /// </summary>
        [HttpPost("DoctorPatients/TransferPatient/{patientProfileId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferPatient(int patientProfileId, int hospitalId, string diagnosis)
        {
            try
            {
                if (patientProfileId <= 0 || hospitalId <= 0 || string.IsNullOrEmpty(diagnosis))
                {
                    TempData["Error"] = "جميع الحقول مطلوبة لإجراء عملية التحويل.";
                    return RedirectToAction(nameof(Details), new { patientProfileId });
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var patient = await _context.PatientProfiles.FindAsync(patientProfileId);
                if (patient == null)
                {
                    TempData["Error"] = "ملف المريض غير موجود.";
                    return RedirectToAction(nameof(Search));
                }

                var emergencyRequest = new EmergencyRequest
                {
                    PatientProfileId = patientProfileId,
                    HealthcareProviderId = hospitalId,
                    Status = EmergencyRequestStatus.Pending,
                    EmergencyType = "DoctorTransfer",
                    Description = diagnosis,
                    Latitude = patient.Latitude,
                    Longitude = patient.Longitude,
                    RequestedAt = DateTime.UtcNow,
                    PriorityScore = 100, // Doctor transfers get absolute high priority
                    AssignedDoctorUserId = userId, // referring doctor ID
                    DoctorsNotified = false,
                    AdminNotified = false
                };

                _context.EmergencyRequests.Add(emergencyRequest);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تحويل المريض إلى قسم الطوارئ بالمستشفى المختار بنجاح.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring patient {PatientId} to hospital {HospitalId}", patientProfileId, hospitalId);
                TempData["Error"] = "حدث خطأ أثناء إتمام عملية التحويل.";
            }

            return RedirectToAction(nameof(Details), new { patientProfileId });
        }
    }
}
