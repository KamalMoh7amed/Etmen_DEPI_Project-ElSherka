using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Doctor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Doctor Slots Controller
    /// Configures doctor availability slots
    /// </summary>
    [Authorize(Roles = "Doctor")]
    public class DoctorSlotsController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly ILogger<DoctorSlotsController> _logger;

        public DoctorSlotsController(
            IDoctorService doctorService,
            ILogger<DoctorSlotsController> logger)
        {
            _doctorService = doctorService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /DoctorSlots/Index
        /// Renders calendar grid of slots
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                _logger.LogInformation("Doctor slots list accessed for user {UserId}", userId);

                var profileResult = await _doctorService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data?.Id == 0)
                {
                    _logger.LogWarning("Unable to get doctor profile for user {UserId}", userId);
                    ModelState.AddModelError(string.Empty, "Failed to retrieve doctor information");
                    return View(new List<Etmen_BLL.DTOs.Doctor.DoctorAvailableSlotDto>());
                }

                var slotsResult = await _doctorService.GetAvailableSlotsAsync(profileResult.Data!.Id);

                if (!slotsResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to fetch slots for doctor {UserId}", userId);
                    ModelState.AddModelError(string.Empty, "Failed to load available slots");
                    return View(new List<Etmen_BLL.DTOs.Nearby.AvailableSlotDto>());
                }

                return View(slotsResult.Data?.ToList() ?? new List<Etmen_BLL.DTOs.Nearby.AvailableSlotDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available slots");
                TempData["Error"] = "Error loading available slots";
                return View(new List<Etmen_BLL.DTOs.Doctor.DoctorAvailableSlotDto>());
            }
        }

        /// <summary>
        /// GET: /DoctorSlots/Create
        /// Show create slot form
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                return View(new CreateAvailableSlotViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create slot form");
                TempData["Error"] = "Error loading form";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /DoctorSlots/Create
        /// Adds a single availability slot
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAvailableSlotViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var slotDto = new Etmen_BLL.DTOs.Doctor.CreateAvailableSlotDto
                {
                    SlotDate = viewModel.SlotDate,
                    SlotStart = viewModel.StartTime,
                    SlotEnd = viewModel.EndTime
                };

                var result = await _doctorService.AddSlotAsync(userId, slotDto);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to create slot for doctor {UserId}", userId);
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error);
                    return View(viewModel);
                }

                _logger.LogInformation("New slot created for user {UserId}", userId);
                TempData["Success"] = "Slot added successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating slot");
                ModelState.AddModelError(string.Empty, "Error adding slot");
                return View(viewModel);
            }
        }

        /// <summary>
        /// GET: /DoctorSlots/BulkCreate
        /// Show bulk create slots form
        /// </summary>
        [HttpGet]
        public IActionResult BulkCreate()
        {
            try
            {
                return View(new BulkCreateSlotsViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bulk create form");
                TempData["Error"] = "Error loading form";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /DoctorSlots/BulkCreate
        /// Auto-generates series of slots in range
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreate(BulkCreateSlotsViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var bulkDto = new Etmen_BLL.DTOs.Doctor.BulkCreateSlotsDto
                {
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    DailyStartTime = viewModel.StartTime,
                    DailyEndTime = viewModel.EndTime,
                    SlotDurationMinutes = viewModel.SlotDurationMinutes,
                    ExcludedDays = new List<DayOfWeek>()
                };

                var result = await _doctorService.BulkAddSlotsAsync(userId, bulkDto);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to create bulk slots for doctor {UserId}", userId);
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error);
                    return View(viewModel);
                }

                _logger.LogInformation("Bulk slots created for user {UserId}", userId);
                TempData["Success"] = "Slots added successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk slots");
                ModelState.AddModelError(string.Empty, "Error adding slots");
                return View(viewModel);
            }
        }

        /// <summary>
        /// POST: /DoctorSlots/Delete
        /// Deletes an unbooked availability slot
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid slot ID";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _doctorService.DeleteSlotAsync(userId, id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to delete slot {SlotId} for doctor {UserId}", id, userId);
                    TempData["Error"] = result.Errors.FirstOrDefault() ?? "Failed to delete slot";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Slot {SlotId} deleted for user {UserId}", id, userId);
                TempData["Success"] = "Slot deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting slot {SlotId}", id);
                TempData["Error"] = "Error deleting slot";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Route("DoctorSlots/ToggleSlot")]
        public async Task<IActionResult> ToggleSlot(int id, DateTime date, TimeSpan startTime, TimeSpan endTime, bool enable)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Unauthorized" });

                if (enable)
                {
                    var slotDto = new Etmen_BLL.DTOs.Doctor.CreateAvailableSlotDto
                    {
                        SlotDate = date,
                        SlotStart = startTime,
                        SlotEnd = endTime
                    };
                    var result = await _doctorService.AddSlotAsync(userId, slotDto);
                    if (result.IsSuccess)
                        return Json(new { success = true, action = "created" });
                    return Json(new { success = false, message = result.Errors.FirstOrDefault() ?? "فشل تفعيل الفترة." });
                }
                else
                {
                    var result = await _doctorService.DeleteSlotAsync(userId, id);
                    if (result.IsSuccess)
                        return Json(new { success = true, action = "deleted" });
                    return Json(new { success = false, message = result.Errors.FirstOrDefault() ?? "فشل إلغاء الفترة." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling slot {SlotId}", id);
                return Json(new { success = false, message = "حدث خطأ غير متوقع." });
            }
        }

        [HttpPost]
        [Route("DoctorSlots/BlockFullDay")]
        public async Task<IActionResult> BlockFullDay(DateTime date)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Unauthorized" });

                var profileResult = await _doctorService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                    return Json(new { success = false, message = "Doctor profile not found." });

                var doctorId = profileResult.Data.Id;

                var unbookedSlots = await _doctorService.GetAvailableSlotsAsync(doctorId);
                if (unbookedSlots.IsSuccess && unbookedSlots.Data != null)
                {
                    var targetSlots = unbookedSlots.Data
                        .Where(s => s.Date.Date == date.Date && !s.IsBooked)
                        .ToList();

                    foreach (var slot in targetSlots)
                    {
                        await _doctorService.DeleteSlotAsync(userId, slot.Id);
                    }
                }

                return Json(new { success = true, message = "تم إغلاق كافة الفترات الشاغرة لهذا اليوم بنجاح." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking day {Date}", date);
                return Json(new { success = false, message = "حدث خطأ أثناء إغلاق فترات اليوم." });
            }
        }

        [HttpPost]
        [Route("DoctorSlots/BulkCreateAjax")]
        public async Task<IActionResult> BulkCreateAjax(BulkCreateSlotsViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(" | ", errors) });
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Unauthorized" });

                var bulkDto = new Etmen_BLL.DTOs.Doctor.BulkCreateSlotsDto
                {
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    DailyStartTime = viewModel.StartTime,
                    DailyEndTime = viewModel.EndTime,
                    SlotDurationMinutes = viewModel.SlotDurationMinutes,
                    ExcludedDays = new List<DayOfWeek>()
                };

                var result = await _doctorService.BulkAddSlotsAsync(userId, bulkDto);

                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.Errors.FirstOrDefault() ?? "فشل توليد الفترات." });
                }

                return Json(new { success = true, message = "تم توليد الفترات بنجاح!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating slots via AJAX");
                return Json(new { success = false, message = "حدث خطأ غير متوقع أثناء توليد الفترات." });
            }
        }
    }
}
