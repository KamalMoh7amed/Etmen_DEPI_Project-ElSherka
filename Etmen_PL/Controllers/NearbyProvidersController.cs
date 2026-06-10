using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Nearby Providers Controller
    /// Queries and maps nearest emergency clinics
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class NearbyProvidersController : Controller
    {
        private readonly INearbyService _nearbyService;
        private readonly IAppointmentService _appointmentService;
        private readonly IPatientService _patientService;
        private readonly ILogger<NearbyProvidersController> _logger;

        public NearbyProvidersController(
            INearbyService nearbyService,
            IAppointmentService appointmentService,
            IPatientService patientService,
            ILogger<NearbyProvidersController> logger)
        {
            _nearbyService = nearbyService;
            _appointmentService = appointmentService;
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /NearbyProviders/Index
        /// Renders GPS location map finder
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            var viewModel = new NearbySearchViewModel();
            return View(viewModel);
        }

        /// <summary>
        /// POST: /NearbyProviders/Index
        /// Lists facilities near coordinate radius
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(NearbySearchViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                if (!viewModel.Latitude.HasValue || !viewModel.Longitude.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Location coordinates are required.");
                    return View(viewModel);
                }

                var searchDto = new NearbySearchDto
                {
                    Latitude = viewModel.Latitude.Value,
                    Longitude = viewModel.Longitude.Value,
                    RadiusInKm = viewModel.RadiusInKm,
                    Type = viewModel.Type
                };

                var result = await _nearbyService.SearchNearbyProvidersAsync(searchDto);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error searching nearby providers.");
                    return View(viewModel);
                }

                viewModel.SearchResults = result.Data ?? new List<ProviderDto>();
                _logger.LogInformation("Nearby providers search performed at {Latitude}, {Longitude}", viewModel.Latitude, viewModel.Longitude);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching nearby providers");
                ModelState.AddModelError(string.Empty, "خطأ في البحث عن المراكز الصحية");
                return View(viewModel);
            }
        }

        /// <summary>
        /// POST: /NearbyProviders/Book
        /// Books a slot with a doctor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int providerId, int slotId, int? doctorId = null)
        {
            try
            {
                var selectedDoctorId = doctorId.GetValueOrDefault(providerId);
                if (selectedDoctorId <= 0 || slotId <= 0)
                {
                    TempData["Error"] = "Invalid doctor or slot.";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return RedirectToAction("Login", "Account");

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = profileResult.ErrorMessage ?? "Patient profile not found.";
                    return RedirectToAction(nameof(Index));
                }

                var slotsResult = await _nearbyService.GetAvailableSlotsByProviderAsync(selectedDoctorId);
                if (!slotsResult.IsSuccess || slotsResult.Data == null)
                {
                    TempData["Error"] = slotsResult.ErrorMessage ?? "No available slots for this provider.";
                    return RedirectToAction(nameof(Index));
                }

                var slot = slotsResult.Data.FirstOrDefault(s => s.Id == slotId);
                if (slot == null)
                {
                    TempData["Error"] = "Selected slot is not available.";
                    return RedirectToAction(nameof(Index));
                }

                var bookingDto = new BookingRequestDto
                {
                    PatientProfileId = profileResult.Data.Id,
                    DoctorId = slot.DoctorId,
                    SlotId = slot.Id,
                    Date = slot.Date,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime
                };

                var result = await _appointmentService.BookAppointmentAsync(userId, bookingDto);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error booking appointment.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Appointment booked for user {UserId} and slot {SlotId}", userId, slotId);
                TempData["Success"] = "تم حجز الموعد بنجاح";
                return RedirectToAction("Index", "PatientDashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking appointment");
                TempData["Error"] = "خطأ في حجز الموعد";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
