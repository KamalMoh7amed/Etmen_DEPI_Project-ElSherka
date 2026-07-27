using Etmen_BLL.DTOs.Emergency;
using Etmen_BLL.DTOs.HospitalStaff;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Etmen_DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Emergency Controller
    /// Triggers ambulance requests, sends confirmation email, and tracks dispatch
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class EmergencyController : Controller
    {
        private readonly IEmergencyService _emergencyService;
        private readonly IPatientService _patientService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmergencyController> _logger;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<Etmen_PL.Hubs.QueueHub> _queueHubContext;
        private readonly IUnitOfWork _uow;
        private readonly IHospitalStaffService _hospitalStaffService;

        public EmergencyController(
            IEmergencyService emergencyService,
            IPatientService patientService,
            IEmailService emailService,
            ILogger<EmergencyController> logger,
            Microsoft.AspNetCore.SignalR.IHubContext<Etmen_PL.Hubs.QueueHub> queueHubContext,
            IUnitOfWork uow,
            IHospitalStaffService hospitalStaffService)
        {
            _emergencyService = emergencyService;
            _patientService   = patientService;
            _emailService     = emailService;
            _logger           = logger;
            _queueHubContext  = queueHubContext;
            _uow              = uow;
            _hospitalStaffService = hospitalStaffService;
        }

        /// <summary>
        /// POST: /Emergency/RequestAmbulance
        /// Creates emergency request and sends confirmation email to patient
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAmbulance(EmergencyRequestViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index", "PatientDashboard");

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                // Get patient profile for email
                var patient = HttpContext.Items["PatientProfile"] as Etmen_Domain.Entities.PatientProfile;
                if (patient == null)
                {
                    TempData["Error"] = "لم يتم العثور على ملفك الطبي.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                var dto = new EmergencyRequestDto
                {
                    PatientProfileId = patient.Id,
                    Latitude         = viewModel.Latitude,
                    Longitude        = viewModel.Longitude,
                    EmergencyType    = viewModel.EmergencyType ?? "طوارئ عامة",
                    Description      = viewModel.Description,
                    RiskAssessmentId = viewModel.RiskAssessmentId
                };

                var result = await _emergencyService.CreateEmergencyRequestAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل إرسال طلب الطوارئ.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                // Broadcast alert to all listening hospitals
                await _queueHubContext.Clients.All.SendAsync("NewEmergencyRequest", result.Data);

                // AI Auto-Admission Mode Check
                if (result.IsSuccess && result.Data != null && result.Data.HealthcareProviderId.HasValue)
                {
                    int providerId = result.Data.HealthcareProviderId.Value;
                    if (HospitalQueueController.IsAiModeActive(providerId))
                    {
                        var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
                        if (provider != null && (provider.AvailableBeds ?? 0) > 0 && (provider.AvailableAmbulances ?? 0) > 0)
                        {
                            var autoRespond = new HospitalStaffEmergencyRespondDto
                            {
                                RequestId = result.Data.Id,
                                ProviderId = providerId,
                                Status = "Accepted",
                                ResponseNotes = "تم القبول والفرز تلقائياً بواسطة نظام الذكاء الاصطناعي لسرعة الاستجابة"
                            };
                            var autoAcceptResult = await _hospitalStaffService.RespondToRequestAsync(autoRespond);
                            if (autoAcceptResult.IsSuccess)
                            {
                                // Broadcast update to SignalR clients (Patients and Admin dashboard)
                                await _queueHubContext.Clients.All.SendAsync("EmergencyRequestUpdated", new
                                {
                                    requestId = result.Data.Id,
                                    status = "Accepted",
                                    providerId = providerId,
                                    doctorName = "طاقم الطوارئ المناوب",
                                    responseNotes = autoRespond.ResponseNotes
                                });

                                // Broadcast capacity changes to SignalR clients
                                await _queueHubContext.Clients.All.SendAsync("HospitalBedsUpdated", new
                                {
                                    providerId = provider.Id,
                                    availableBeds = provider.AvailableBeds ?? 0,
                                    bedCapacity = provider.BedCapacity ?? 150,
                                    availableAmbulances = provider.AvailableAmbulances ?? 0,
                                    ambulanceCapacity = provider.AmbulanceCapacity ?? 4
                                });
                            }
                        }
                    }
                }

                var patientEmail = User.FindFirstValue(ClaimTypes.Email);
                var patientName  = patient.FullName ?? "المريض";

                if (!string.IsNullOrWhiteSpace(patientEmail))
                {
                    try
                    {
                        await _emailService.SendEmergencyConfirmationEmailAsync(
                            patientEmail,
                            patientName,
                            dto.EmergencyType ?? "طوارئ عامة",
                            DateTime.UtcNow);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send emergency confirmation email for request {RequestId}.", result.Data.Id);
                    }
                }

                _logger.LogInformation(
                    "Emergency request created for user {UserId}, type: {Type}",
                    userId, dto.EmergencyType);

                TempData["EmergencyRequestId"] = result.Data.Id;
                TempData["Success"] = "🚨 تم إرسال طلب الطوارئ! ستصلك رسالة تأكيد على بريدك الإلكتروني.";
                return RedirectToAction(nameof(Track), new { id = result.Data.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting ambulance");
                TempData["Error"] = "خطأ في طلب الإسعاف. يُرجى الاتصال بالطوارئ مباشرة: 123";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// GET: /Emergency/Track
        /// Tracks ambulance status and estimated arrival
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Track(int? id)
        {
            try
            {
                var requestId = id ?? TryReadRequestIdFromTempData();
                if (requestId == null || requestId <= 0)
                    return RedirectToAction("Index", "PatientDashboard");

                var result = await _emergencyService.GetEmergencyRequestAsync(requestId.Value);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = "لم يتم العثور على طلب الطوارئ.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return RedirectToAction("Login", "Account");

                var patient = HttpContext.Items["PatientProfile"] as Etmen_Domain.Entities.PatientProfile;
                if (patient == null || result.Data.PatientProfileId != patient.Id)
                {
                    _logger.LogWarning("User {UserId} attempted to track emergency request {RequestId} without ownership.", userId, requestId.Value);
                    return Forbid();
                }

                // Load latest risk assessment
                var latestRisk = await _uow.RiskAssessments.Table
                    .Where(r => r.PatientProfileId == patient.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();
                ViewBag.LatestRisk = latestRisk;

                // Load all emergency receiving hospitals for the map
                var hospitals = await _uow.HealthcareProviders.Table
                    .Where(h => h.IsActive && (h.IsEmergencyCenter || h.Type == "Hospital"))
                    .Select(h => new
                    {
                        h.Id,
                        h.Name,
                        Latitude = (double)h.Latitude,
                        Longitude = (double)h.Longitude,
                        AvailableBeds = h.AvailableBeds ?? 0,
                        AvailableAmbulances = h.AvailableAmbulances ?? 0,
                        AmbulanceCapacity = h.AmbulanceCapacity ?? 4,
                        h.Address
                    })
                    .ToListAsync();
                ViewBag.Hospitals = hospitals;

                // Load the raw entity to get status and accepted hospital info
                var rawRequest = await _uow.EmergencyRequests.Table
                    .Include(r => r.HealthcareProvider)
                    .Include(r => r.AssignedDoctor)
                    .FirstOrDefaultAsync(r => r.Id == requestId.Value);

                ViewBag.CurrentStatus = rawRequest?.Status.ToString() ?? "Pending";
                ViewBag.AcceptedProviderId = rawRequest?.HealthcareProviderId;
                ViewBag.RequestedAt = rawRequest?.RequestedAt;
                ViewBag.AcceptedAt = rawRequest?.AcceptedAt;
                ViewBag.CompletedAt = rawRequest?.CompletedAt;
                ViewBag.DoctorName = rawRequest?.AssignedDoctor != null 
                    ? $"{rawRequest.AssignedDoctor.FirstName} {rawRequest.AssignedDoctor.LastName}" 
                    : null;

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking emergency");
                TempData["Error"] = "خطأ في تتبع الإسعاف";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// POST: /Emergency/RequestDirectReception
        /// Creates a direct hospital reception request from the patient
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDirectReception(int hospitalId, string notes, int? riskAssessmentId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "لم يتم تسجيل الدخول" });

                var patient = HttpContext.Items["PatientProfile"] as Etmen_Domain.Entities.PatientProfile;
                if (patient == null)
                    return Json(new { success = false, message = "لم يتم العثور على ملفك الطبي" });

                // Find patient profile coordinates
                decimal lat = patient.Latitude ?? 30.0444m;
                decimal lng = patient.Longitude ?? 31.2357m;

                var dto = new EmergencyRequestDto
                {
                    PatientProfileId = patient.Id,
                    Latitude         = lat,
                    Longitude        = lng,
                    EmergencyType    = "استقبال مباشر",
                    Description      = notes,
                    RiskAssessmentId = riskAssessmentId,
                    HealthcareProviderId = hospitalId
                };

                var result = await _emergencyService.CreateEmergencyRequestAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                    return Json(new { success = false, message = result.ErrorMessage ?? "فشل إرسال الطلب" });

                // Broadcast alert to hospital via SignalR
                await _queueHubContext.Clients.All.SendAsync("NewEmergencyRequest", result.Data);

                // AI Auto-Admission Mode Check
                if (result.IsSuccess && result.Data != null && result.Data.HealthcareProviderId.HasValue)
                {
                    int providerId = result.Data.HealthcareProviderId.Value;
                    if (HospitalQueueController.IsAiModeActive(providerId))
                    {
                        var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
                        if (provider != null && (provider.AvailableBeds ?? 0) > 0)
                        {
                            var autoRespond = new HospitalStaffEmergencyRespondDto
                            {
                                RequestId = result.Data.Id,
                                ProviderId = providerId,
                                Status = "Accepted",
                                ResponseNotes = "تم القبول والفرز تلقائياً بواسطة نظام الذكاء الاصطناعي لسرعة الاستجابة"
                            };
                            var autoAcceptResult = await _hospitalStaffService.RespondToRequestAsync(autoRespond);
                            if (autoAcceptResult.IsSuccess)
                            {
                                // Broadcast update to SignalR clients (Patients and Admin dashboard)
                                await _queueHubContext.Clients.All.SendAsync("EmergencyRequestUpdated", new
                                {
                                    requestId = result.Data.Id,
                                    status = "Accepted",
                                    providerId = providerId,
                                    doctorName = "طاقم الطوارئ المناوب",
                                    responseNotes = autoRespond.ResponseNotes
                                });

                                // Broadcast capacity changes to SignalR clients
                                await _queueHubContext.Clients.All.SendAsync("HospitalBedsUpdated", new
                                {
                                    providerId = provider.Id,
                                    availableBeds = provider.AvailableBeds ?? 0,
                                    bedCapacity = provider.BedCapacity ?? 150,
                                    availableAmbulances = provider.AvailableAmbulances ?? 0,
                                    ambulanceCapacity = provider.AmbulanceCapacity ?? 4
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, message = "تم إرسال طلب الاستقبال بنجاح للمستشفى!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting direct hospital reception");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "غير مصرح" });

                var result = await _emergencyService.UpdateEmergencyStatusAsync(id, new Etmen_BLL.DTOs.Emergency.EmergencyUpdateDto
                {
                    Status = "Completed",
                    ResponseNotes = "تم وصول الحالة تلقائياً عن طريق التتبع الجغرافي للمريض."
                });

                if (!result.IsSuccess)
                    return Json(new { success = false, message = result.ErrorMessage ?? "فشل تحديث الحالة" });

                // Get request info to broadcast capacity updates
                var emergencyRequest = await _uow.EmergencyRequests.GetByIdAsync(id);
                if (emergencyRequest != null && emergencyRequest.HealthcareProviderId.HasValue)
                {
                    var provider = await _uow.HealthcareProviders.GetByIdAsync(emergencyRequest.HealthcareProviderId.Value);
                    if (provider != null)
                    {
                        await _queueHubContext.Clients.All.SendAsync("HospitalBedsUpdated", new
                        {
                            providerId = provider.Id,
                            availableBeds = provider.AvailableBeds ?? 0,
                            bedCapacity = provider.BedCapacity ?? 150,
                            availableAmbulances = provider.AvailableAmbulances ?? 0,
                            ambulanceCapacity = provider.AmbulanceCapacity ?? 4
                        });
                    }
                }

                // Broadcast SignalR update
                await _queueHubContext.Clients.All.SendAsync("EmergencyRequestUpdated", new
                {
                    requestId = id,
                    status = "Completed"
                });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing emergency request via client tracking");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int? TryReadRequestIdFromTempData()
        {
            var value = TempData["EmergencyRequestId"];
            if (value is null)
                return null;

            return int.TryParse(value.ToString(), out var requestId) ? requestId : null;
        }
    }
}
