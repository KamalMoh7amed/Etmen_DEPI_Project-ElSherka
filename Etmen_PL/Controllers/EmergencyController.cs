using Etmen_BLL.DTOs.Emergency;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

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

        public EmergencyController(
            IEmergencyService emergencyService,
            IPatientService patientService,
            IEmailService emailService,
            ILogger<EmergencyController> logger,
            Microsoft.AspNetCore.SignalR.IHubContext<Etmen_PL.Hubs.QueueHub> queueHubContext)
        {
            _emergencyService = emergencyService;
            _patientService   = patientService;
            _emailService     = emailService;
            _logger           = logger;
            _queueHubContext  = queueHubContext;
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
                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = "لم يتم العثور على ملفك الطبي.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                var dto = new EmergencyRequestDto
                {
                    PatientProfileId = profileResult.Data.Id,
                    Latitude         = viewModel.Latitude,
                    Longitude        = viewModel.Longitude,
                    EmergencyType    = viewModel.EmergencyType ?? "طوارئ عامة",
                    Description      = viewModel.Description
                };

                var result = await _emergencyService.CreateEmergencyRequestAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل إرسال طلب الطوارئ.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                // Broadcast alert to all listening hospitals
                await _queueHubContext.Clients.All.SendAsync("NewEmergencyRequest", result.Data);

                var patientEmail = User.FindFirstValue(ClaimTypes.Email);
                var patientName  = profileResult.Data.FullName ?? "المريض";

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

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null || result.Data.PatientProfileId != profileResult.Data.Id)
                {
                    _logger.LogWarning("User {UserId} attempted to track emergency request {RequestId} without ownership.", userId, requestId.Value);
                    return Forbid();
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking emergency");
                TempData["Error"] = "خطأ في تتبع الإسعاف";
                return RedirectToAction("Index", "PatientDashboard");
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
