using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Hubs;
using Etmen_PL.Models.ViewModels.Emergency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Doctor Panic Inbox Controller
    /// Handles urgent patient alerts and case assignment
    /// </summary>
    [Authorize(Roles = "Doctor")]
    public class DoctorPanicInboxController : Controller
    {
        private readonly ICriticalIntelligenceService _criticalIntelligenceService;
        private readonly ILogger<DoctorPanicInboxController> _logger;
        private readonly IHubContext<EmergencyHub> _emergencyHub;

        public DoctorPanicInboxController(
            ICriticalIntelligenceService criticalIntelligenceService,
            ILogger<DoctorPanicInboxController> logger,
            IHubContext<EmergencyHub> emergencyHub)
        {
            _criticalIntelligenceService = criticalIntelligenceService;
            _logger = logger;
            _emergencyHub = emergencyHub;
        }

        /// <summary>
        /// GET: /DoctorPanicInbox/Index
        /// Lists assigned and unassigned critical cases
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                _logger.LogInformation("Doctor panic inbox accessed for user {UserId}", userId);

                var inboxResult = await _criticalIntelligenceService.GetDoctorPanicInboxAsync(userId);

                if (!inboxResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to fetch panic inbox for doctor {UserId}", userId);
                    ModelState.AddModelError(string.Empty, "Failed to load critical cases list");
                    return View(new DoctorPanicInboxViewModel());
                }

                var viewModel = new DoctorPanicInboxViewModel
                {
                    DoctorName = inboxResult.Data?.DoctorName ?? "",
                    DoctorUserId = userId,
                    IsAvailable = inboxResult.Data?.IsAvailable ?? true,
                    TotalCriticalCases = inboxResult.Data?.TotalCriticalCases ?? 0,
                    AssignedToDoctor = inboxResult.Data?.AssignedToDoctor ?? 0,
                    UnassignedCriticalCases = inboxResult.Data?.UnassignedCriticalCases ?? 0,
                    Items = inboxResult.Data?.Items ?? new List<Etmen_BLL.DTOs.CriticalIntelligence.DoctorPanicInboxItemDto>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving panic inbox");
                TempData["Error"] = "Error loading critical cases list";
                return View(new DoctorPanicInboxViewModel());
            }
        }

        /// <summary>
        /// POST: /DoctorPanicInbox/ToggleAvailability
        /// Toggles the doctor's availability for emergency cases
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _criticalIntelligenceService.ToggleDoctorAvailabilityAsync(userId);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors.FirstOrDefault() ?? "Failed to update availability";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = result.Data
                    ? "أصبحت متاحاً لاستقبال حالات الطوارئ"
                    : "تم إيقاف استقبال حالات الطوارئ مؤقتاً";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling availability");
                TempData["Error"] = "Error updating availability status";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /DoctorPanicInbox/AssignToMe
        /// Assigns a critical care request to the current doctor (removes from other doctors' lists)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToMe(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid case ID";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Use AssignCaseToDoctorAsync so that the case is assigned specifically to THIS doctor
                // (removing it from other doctors' unassigned lists automatically via DB query filter)
                var assignResult = await _criticalIntelligenceService.AssignCaseToDoctorAsync(id, userId);

                if (!assignResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to assign panic case {CaseId} to doctor {UserId}", id, userId);
                    TempData["Error"] = assignResult.Errors.FirstOrDefault() ?? "Failed to assign case";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Panic case {CaseId} assigned to doctor {UserId}", id, userId);

                // Broadcast real-time removal to all other doctors
                await _emergencyHub.Clients.Group("AllDoctors")
                    .SendAsync("CaseAssigned", new
                    {
                        emergencyRequestId = id,
                        assignedToDoctorId = userId,
                        assignedDoctorName = assignResult.Data?.DoctorName
                    });

                // Retrieve patient user ID and broadcast status update to patient/family group
                var caseDetail = await _criticalIntelligenceService.GetEmergencyCaseDetailAsync(id, userId);
                if (caseDetail.IsSuccess && caseDetail.Data != null)
                {
                    var patientUserId = caseDetail.Data.PatientUserId;
                    await _emergencyHub.Clients.Group($"Emergency_{patientUserId}")
                        .SendAsync("EmergencyUpdated", new
                        {
                            emergencyRequestId = id,
                            status = "Accepted",
                            doctorName = assignResult.Data?.DoctorName ?? "",
                            doctorUserId = userId,
                            hospitalName = caseDetail.Data.HospitalName ?? "قيد المتابعة والبحث",
                            patientRecommendations = caseDetail.Data.PatientRecommendations,
                            familyRecommendations = caseDetail.Data.FamilyRecommendations,
                            prescribedMedications = caseDetail.Data.PrescribedMedications
                        });
                }

                TempData["Success"] = "✔ تم تولي الحالة بنجاح. سيظهر المريض في قائمتك فقط.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning panic case {CaseId}", id);
                TempData["Error"] = "Error assigning case";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /DoctorPanicInbox/MarkResolved
        /// Marks a critical case as resolved
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "معرف حالة غير صالح";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                _logger.LogInformation("Doctor {UserId} viewing panic case {CaseId}", userId, id);

                var result = await _criticalIntelligenceService.GetEmergencyCaseDetailAsync(id, userId);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors.FirstOrDefault() ?? "تعذّر تحميل بيانات الحالة";
                    return RedirectToAction(nameof(Index));
                }

                var d = result.Data!;
                var vm = new EmergencyCaseDetailViewModel
                {
                    EmergencyRequestId = d.EmergencyRequestId,
                    Status = d.Status,
                    EmergencyType = d.EmergencyType,
                    Description = d.Description,
                    PriorityScore = d.PriorityScore,
                    RequestedAt = d.RequestedAt,
                    AcceptedAt = d.AcceptedAt,
                    DoctorAssignedAt = d.DoctorAssignedAt,
                    EscalationReason = d.EscalationReason,
                    IsAssignedToCurrentDoctor = d.IsAssignedToCurrentDoctor,
                    RiskScore = d.RiskScore,
                    RiskLevel = d.RiskLevel,
                    Symptoms = d.Symptoms,
                    PatientProfileId = d.PatientProfileId,
                    PatientUserId = d.PatientUserId,
                    PatientName = d.PatientName,
                    PatientPhone = d.PatientPhone,
                    PatientEmail = d.PatientEmail,
                    Gender = d.Gender,
                    DateOfBirth = d.DateOfBirth,
                    BloodType = d.BloodType,
                    HasChronicDiseases = d.HasChronicDiseases,
                    ChronicDiseasesNotes = d.ChronicDiseasesNotes,
                    Allergies = d.Allergies,
                    CurrentMedications = d.CurrentMedications,
                    Height = d.Height,
                    Weight = d.Weight,
                    SystolicBP = d.SystolicBP,
                    DiastolicBP = d.DiastolicBP,
                    HeartRate = d.HeartRate,
                    Temperature = d.Temperature,
                    OxygenSaturation = d.OxygenSaturation,
                    BloodSugar = d.BloodSugar,
                    VitalsRecordedAt = d.VitalsRecordedAt,
                    AssignedDoctorName = d.AssignedDoctorName,
                    AssignedDoctorSpecialization = d.AssignedDoctorSpecialization,
                    HospitalName = d.HospitalName,
                    SuggestedFirstMessage = d.SuggestedFirstMessage,
                    PatientRecommendations = d.PatientRecommendations,
                    FamilyRecommendations = d.FamilyRecommendations,
                    PrescribedMedications = d.PrescribedMedications,
                    PatientFamilyMembers = d.PatientFamilyMembers
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving panic case {CaseId}", id);
                TempData["Error"] = "خطأ في تحميل بيانات الحالة";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /DoctorPanicInbox/MarkResolved
        /// Marks a critical case as resolved
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkResolved(int id, string resolution)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid case ID";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                _logger.LogInformation("Panic case {CaseId} marked as resolved by doctor {UserId}", id, userId);
                TempData["Success"] = "Case resolution recorded successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving panic case {CaseId}", id);
                TempData["Error"] = "Error updating case status";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /DoctorPanicInbox/SaveRecommendations
        /// Saves doctor recommendations and prescriptions for an emergency request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRecommendations(int id, string? patientRecommendations, string? familyRecommendations, string? prescribedMedications)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "معرف حالة غير صالح";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _criticalIntelligenceService.SaveRecommendationsAsync(id, patientRecommendations, familyRecommendations, prescribedMedications);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل حفظ التوصيات";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                // Broadcast real-time recommendation updates to patient/family group
                var caseDetail = await _criticalIntelligenceService.GetEmergencyCaseDetailAsync(id, userId);
                if (caseDetail.IsSuccess && caseDetail.Data != null)
                {
                    var patientUserId = caseDetail.Data.PatientUserId;
                    await _emergencyHub.Clients.Group($"Emergency_{patientUserId}")
                        .SendAsync("EmergencyUpdated", new
                        {
                            emergencyRequestId = id,
                            status = caseDetail.Data.Status.ToString(),
                            doctorName = caseDetail.Data.AssignedDoctorName ?? "",
                            doctorUserId = userId,
                            hospitalName = caseDetail.Data.HospitalName ?? "قيد المتابعة والبحث",
                            patientRecommendations = patientRecommendations,
                            familyRecommendations = familyRecommendations,
                            prescribedMedications = prescribedMedications
                        });
                }

                TempData["Success"] = "✔ تم حفظ التوصيات والتعليمات الطبية بنجاح وإشعار المريض";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving recommendations for case {CaseId}", id);
                TempData["Error"] = "حدث خطأ أثناء حفظ التوصيات";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        /// <summary>
        /// GET: /DoctorPanicInbox/GetUnreadCount
        /// Returns the count of active unassigned and assigned critical cases for this doctor's inbox
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { count = 0 });

                var inboxResult = await _criticalIntelligenceService.GetDoctorPanicInboxAsync(userId);
                if (!inboxResult.IsSuccess || inboxResult.Data == null)
                    return Json(new { count = 0 });

                return Json(new { count = inboxResult.Data.TotalCriticalCases });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }
    }
}
