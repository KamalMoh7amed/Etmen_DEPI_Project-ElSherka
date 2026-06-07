using Etmen_BLL.DTOs.HospitalStaff;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Hospital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Hospital Queue Controller
    /// Monitors incoming ambulances and manages bed availability.
    /// </summary>
    [Authorize(Roles = "HospitalStaff")]
    public class HospitalQueueController : Controller
    {
        private readonly IHospitalStaffService _hospitalStaffService;
        private readonly ILogger<HospitalQueueController> _logger;

        public HospitalQueueController(
            IHospitalStaffService hospitalStaffService,
            ILogger<HospitalQueueController> logger)
        {
            _hospitalStaffService = hospitalStaffService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /HospitalQueue/Index
        /// Lists active ambulance triages.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int? providerId)
        {
            try
            {
                var result = await _hospitalStaffService.GetQueueAsync(providerId);
                if (!result.IsSuccess || result.Data == null)
                {
                    _logger.LogWarning("Failed to retrieve hospital queue for provider {ProviderId}: {Message}", providerId, result.ErrorMessage);
                    TempData["Error"] = result.ErrorMessage ?? "تعذر تحميل قائمة انتظار المستشفى.";
                    return View(new HospitalQueueViewModel { ProviderId = providerId });
                }

                return View(MapQueue(result.Data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hospital queue");
                TempData["Error"] = "حدث خطأ أثناء تحميل قائمة انتظار المستشفى.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// GET: /HospitalQueue/Details
        /// Displays detailed medical context of the emergency patient.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id, int? providerId)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "رقم طلب الطوارئ غير صحيح.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _hospitalStaffService.GetRequestDetailAsync(id, providerId);
                if (!result.IsSuccess || result.Data == null)
                {
                    _logger.LogWarning("Failed to retrieve emergency request {RequestId}: {Message}", id, result.ErrorMessage);
                    TempData["Error"] = result.ErrorMessage ?? "تعذر تحميل تفاصيل طلب الطوارئ.";
                    return RedirectToAction(nameof(Index), new { providerId });
                }

                return View(MapDetail(result.Data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving request details");
                TempData["Error"] = "حدث خطأ أثناء تحميل تفاصيل الطلب.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /HospitalQueue/Respond
        /// Hospital staff accepts, rejects, escalates, completes, or cancels a request.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(HospitalRespondViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });

            try
            {
                var dto = new HospitalStaffEmergencyRespondDto
                {
                    RequestId = viewModel.RequestId,
                    ProviderId = viewModel.ProviderId,
                    Status = viewModel.Status,
                    ResponseNotes = viewModel.ResponseNotes
                };

                var result = await _hospitalStaffService.RespondToRequestAsync(dto);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to respond to emergency request {RequestId}: {Message}", viewModel.RequestId, result.ErrorMessage);
                    TempData["Error"] = result.ErrorMessage ?? "تعذر حفظ الرد.";
                    return RedirectToAction(nameof(Details), new { id = viewModel.RequestId, providerId = viewModel.ProviderId });
                }

                _logger.LogInformation("Response provided to emergency request {RequestId}", viewModel.RequestId);
                TempData["Success"] = "تم تسجيل الرد بنجاح.";
                return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to request");
                TempData["Error"] = "حدث خطأ أثناء حفظ الرد.";
                return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });
            }
        }

        /// <summary>
        /// POST: /HospitalQueue/UpdateBeds
        /// Modifies the hospital's available emergency beds configuration.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBeds(HospitalBedsUpdateViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });

            try
            {
                var dto = new HospitalStaffBedsUpdateDto
                {
                    ProviderId = viewModel.ProviderId,
                    AvailableBeds = viewModel.AvailableBeds
                };

                var result = await _hospitalStaffService.UpdateBedsAsync(dto);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to update beds for provider {ProviderId}: {Message}", viewModel.ProviderId, result.ErrorMessage);
                    TempData["Error"] = result.ErrorMessage ?? "تعذر تحديث عدد الأسرة المتاحة.";
                    return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });
                }

                _logger.LogInformation("Hospital beds updated for provider {ProviderId}", viewModel.ProviderId);
                TempData["Success"] = "تم تحديث عدد الأسرة المتاحة بنجاح.";
                return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating beds");
                TempData["Error"] = "حدث خطأ أثناء تحديث عدد الأسرة.";
                return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });
            }
        }

        private static HospitalQueueViewModel MapQueue(HospitalStaffQueueDto dto) => new()
        {
            ProviderId = dto.ProviderId,
            ProviderName = dto.ProviderName,
            PendingCount = dto.PendingCount,
            AcceptedCount = dto.AcceptedCount,
            EscalatedCount = dto.EscalatedCount,
            AvailableBeds = dto.AvailableBeds,
            Items = dto.Items.Select(item => new HospitalQueueItemViewModel
            {
                RequestId = item.RequestId,
                PatientProfileId = item.PatientProfileId,
                PatientName = item.PatientName,
                PatientPhone = item.PatientPhone,
                EmergencyType = item.EmergencyType,
                Status = item.Status.ToString(),
                RequestedAt = item.RequestedAt,
                WaitingMinutes = item.WaitingMinutes,
                IsAutoGenerated = item.IsAutoGenerated,
                PriorityScore = item.PriorityScore
            }).ToList()
        };

        private static HospitalEmergencyDetailViewModel MapDetail(HospitalStaffEmergencyDetailDto dto) => new()
        {
            RequestId = dto.RequestId,
            Status = dto.Status.ToString(),
            EmergencyType = dto.EmergencyType,
            Description = dto.Description,
            RequestedAt = dto.RequestedAt,
            AcceptedAt = dto.AcceptedAt,
            ResponseNotes = dto.ResponseNotes,
            PatientName = dto.PatientName,
            PatientPhone = dto.PatientPhone,
            BloodType = dto.BloodType,
            HasChronicDiseases = dto.HasChronicDiseases,
            ChronicDiseasesNotes = dto.ChronicDiseasesNotes,
            Allergies = dto.Allergies,
            CurrentMedications = dto.CurrentMedications,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            AssignedProviderAvailableBeds = dto.AssignedProviderAvailableBeds
        };
    }
}
