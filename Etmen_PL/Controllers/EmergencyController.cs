using Etmen_BLL.DTOs.Emergency;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Emergency;
using Etmen_PL.Models.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Emergency Controller
    /// Triggers ambulance requests and tracks dispatch.
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class EmergencyController : Controller
    {
        private readonly IEmergencyService _emergencyService;
        private readonly IPatientService _patientService;
        private readonly ILogger<EmergencyController> _logger;

        public EmergencyController(
            IEmergencyService emergencyService,
            IPatientService patientService,
            ILogger<EmergencyController> logger)
        {
            _emergencyService = emergencyService;
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// POST: /Emergency/RequestAmbulance
        /// Requests an emergency ambulance at patient coordinates.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAmbulance(EmergencyRequestViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index", "PatientDashboard");

            try
            {
                var patientProfileId = viewModel.PatientProfileId;
                if (patientProfileId <= 0)
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrWhiteSpace(userId))
                        return RedirectToAction("Login", "Account");

                    var profileResult = await _patientService.GetProfileAsync(userId);
                    if (!profileResult.IsSuccess || profileResult.Data == null)
                    {
                        TempData["Error"] = profileResult.ErrorMessage ?? "لم يتم العثور على الملف الشخصي للمريض.";
                        return RedirectToAction("Index", "PatientDashboard");
                    }

                    patientProfileId = profileResult.Data.Id;
                }

                var dto = new EmergencyRequestDto
                {
                    PatientProfileId = patientProfileId,
                    Latitude = viewModel.Latitude,
                    Longitude = viewModel.Longitude,
                    EmergencyType = viewModel.EmergencyType,
                    Description = viewModel.Description
                };

                var result = await _emergencyService.CreateEmergencyRequestAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    _logger.LogWarning(
                        "Failed to request ambulance for patient profile {PatientProfileId}: {Message}",
                        patientProfileId,
                        result.ErrorMessage);
                    TempData["Error"] = result.ErrorMessage ?? "تعذر إرسال طلب الإسعاف.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                _logger.LogInformation("Emergency ambulance requested for patient profile {PatientProfileId}", patientProfileId);
                TempData["Success"] = "تم إرسال طلب الإسعاف بنجاح.";
                return RedirectToAction(nameof(Track));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting ambulance");
                TempData["Error"] = "حدث خطأ أثناء طلب الإسعاف.";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// GET: /Emergency/Track
        /// Tracks ambulance status and distance.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Track(int? id)
        {
            try
            {
                EmergencyTrackingDto? trackingDto = null;

                if (id.HasValue && id.Value > 0)
                {
                    var requestResult = await _emergencyService.GetEmergencyRequestAsync(id.Value);
                    if (!requestResult.IsSuccess || requestResult.Data == null)
                    {
                        TempData["Error"] = requestResult.ErrorMessage ?? "لم يتم العثور على طلب الإسعاف.";
                        return RedirectToAction("Index", "PatientDashboard");
                    }

                    trackingDto = new EmergencyTrackingDto
                    {
                        RequestId = id.Value,
                        Status = Etmen_Domain.Enums.EmergencyRequestStatus.Pending,
                        RequestedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    var pendingResult = await _emergencyService.GetPendingEmergenciesAsync();
                    if (!pendingResult.IsSuccess || pendingResult.Data == null || pendingResult.Data.Count == 0)
                    {
                        TempData["Error"] = pendingResult.ErrorMessage ?? "لا يوجد طلب إسعاف نشط حاليًا.";
                        return RedirectToAction("Index", "PatientDashboard");
                    }

                    trackingDto = pendingResult.Data
                        .OrderByDescending(request => request.RequestedAt)
                        .First();
                }

                var viewModel = new EmergencyTrackingViewModel
                {
                    RequestId = trackingDto.RequestId,
                    Status = trackingDto.Status.ToString(),
                    ProviderName = trackingDto.ProviderName,
                    AmbulancePhoneNumber = trackingDto.ProviderPhone,
                    EstimatedArrivalMinutes = (int)(trackingDto.EstimatedArrivalTime ?? 0),
                    RequestedAt = trackingDto.RequestedAt,
                    AcceptedAt = trackingDto.AcceptedAt
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking emergency");
                TempData["Error"] = "حدث خطأ أثناء تتبع الإسعاف.";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }
    }
}
