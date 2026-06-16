using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Etmen_DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;

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
        private readonly IReviewService _reviewService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<NearbyProvidersController> _logger;

        public NearbyProvidersController(
            INearbyService nearbyService,
            IAppointmentService appointmentService,
            IPatientService patientService,
            IReviewService reviewService,
            IUnitOfWork uow,
            ILogger<NearbyProvidersController> logger)
        {
            _nearbyService = nearbyService;
            _appointmentService = appointmentService;
            _patientService = patientService;
            _reviewService = reviewService;
            _uow = uow;
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

                if (viewModel.ShowAll)
                {
                    var allProviders = await _uow.HealthcareProviders.Table.Where(p => p.IsActive).ToListAsync();
                    viewModel.SearchResults = allProviders.Select(p => new ProviderDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Type = p.Type,
                        Address = p.Address,
                        Phone = p.Phone,
                        AvailableBeds = p.AvailableBeds,
                        IsEmergencyCenter = p.IsEmergencyCenter,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        DistanceKm = (decimal)Etmen_BLL.Helpers.GeoHelper.CalculateDistanceKm(
                            (double)viewModel.Latitude.Value,
                            (double)viewModel.Longitude.Value,
                            (double)p.Latitude,
                            (double)p.Longitude)
                    }).OrderBy(p => p.DistanceKm).ToList();

                    _logger.LogInformation("All providers search performed by patient");
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
        /// GET: /NearbyProviders/GetSlots
        /// Retrieves available slots for a provider (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSlots(int providerId)
        {
            var result = await _nearbyService.GetAvailableSlotsByProviderAsync(providerId);
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);
            return Json(result.Data);
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

                var slot = await _uow.AvailableSlots.GetByIdAsync(slotId);
                if (slot == null || slot.IsBooked)
                {
                    TempData["Error"] = "Selected slot is not available.";
                    return RedirectToAction(nameof(Index));
                }

                var bookingDto = new BookingRequestDto
                {
                    PatientProfileId = profileResult.Data.Id,
                    DoctorId = slot.DoctorProfileId,
                    SlotId = slot.Id,
                    Date = slot.SlotDate,
                    StartTime = slot.SlotStart,
                    EndTime = slot.SlotEnd
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

        [HttpGet("NearbyProviders/DoctorDetails/{id}")]
        public async Task<IActionResult> DoctorDetails(int id, int? doctorId = null)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                // 1. Get Healthcare Provider details
                var provider = await _uow.HealthcareProviders.GetByIdAsync(id);
                if (provider == null)
                {
                    TempData["Error"] = "المنشأة الطبية غير موجودة";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Find doctor linked to this provider
                Etmen_Domain.Entities.DoctorProfile? doctor = null;
                if (doctorId.HasValue && doctorId.Value > 0)
                {
                    doctor = await _uow.DoctorProfiles.Table.Include(d => d.ApplicationUser).FirstOrDefaultAsync(d => d.Id == doctorId.Value);
                }
                else
                {
                    var affiliations = await _uow.DoctorProviders.GetByProviderIdAsync(id);
                    var affiliation = affiliations.FirstOrDefault();
                    if (affiliation != null)
                    {
                        doctor = affiliation.DoctorProfile;
                    }
                    else
                    {
                        // Fallback to onboarding JSON data
                        var doctors = await _uow.DoctorProfiles.Table.Include(d => d.ApplicationUser).ToListAsync();
                        foreach (var doc in doctors)
                        {
                            if (!string.IsNullOrEmpty(doc.OnboardingDataJson))
                            {
                                try
                                {
                                    using var docJson = System.Text.Json.JsonDocument.Parse(doc.OnboardingDataJson);
                                    if (docJson.RootElement.TryGetProperty("HealthcareProviderId", out var prop) && prop.GetInt32() == id)
                                    {
                                        doctor = doc;
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                // 3. Get average rating and reviews list
                double avgRating = 0;
                var reviews = new List<Etmen_BLL.DTOs.Review.ReviewDto>();
                if (doctor != null)
                {
                    var avgResult = await _reviewService.GetDoctorAverageRatingAsync(doctor.Id);
                    avgRating = avgResult.IsSuccess ? avgResult.Data : 0;
                    
                    var reviewsResult = await _reviewService.GetDoctorReviewsAsync(doctor.Id);
                    reviews = reviewsResult.IsSuccess ? reviewsResult.Data ?? new() : new();
                }
                
                ViewBag.AverageRating = avgRating;
                ViewBag.Reviews = reviews;

                // 4. Get available slots
                var slotsResult = await _nearbyService.GetAvailableSlotsByProviderAsync(id);
                var slots = slotsResult.IsSuccess ? slotsResult.Data ?? new() : new();

                if (doctor != null)
                {
                    slots = slots.Where(s => s.DoctorId == doctor.Id).ToList();
                }

                // Group slots by Date for the 3-day sliding calendar
                var viewModel = new DoctorDetailsViewModel
                {
                    ProviderId = id,
                    ProviderName = provider.Name,
                    ProviderAddress = provider.Address ?? string.Empty,
                    ProviderType = provider.Type,
                    DoctorId = doctor?.Id,
                    DoctorName = doctor?.FullName ?? (doctor?.ApplicationUser != null ? $"{doctor.ApplicationUser.FirstName} {doctor.ApplicationUser.LastName}".Trim() : string.Empty),
                    Specialization = doctor?.Specialization ?? "أخصائي عام",
                    Bio = doctor?.Bio ?? "أخصائي متميز بخبرة في الرعاية الطبية الشاملة وتقديم الاستشارات المتخصصة للمرضى.",
                    ConsultationFee = doctor?.ConsultationFee ?? 150.00m,
                    YearsOfExperience = doctor?.YearsOfExperience ?? 5,
                    LicenseNumber = doctor?.LicenseNumber ?? string.Empty,
                    AvailableSlots = slots
                };

                // Fallback for DoctorName if no doctor profile is linked
                if (string.IsNullOrWhiteSpace(viewModel.DoctorName))
                {
                    viewModel.DoctorName = provider.Name;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctor booking details");
                TempData["Error"] = "حدث خطأ أثناء تحميل صفحة الطبيب";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("NearbyProviders/ProviderDoctors/{id}")]
        public async Task<IActionResult> ProviderDoctors(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                var provider = await _uow.HealthcareProviders.GetByIdAsync(id);
                if (provider == null)
                {
                    TempData["Error"] = "المنشأة الطبية غير موجودة";
                    return RedirectToAction(nameof(Index));
                }

                // Get all affiliated doctors
                var affiliations = await _uow.DoctorProviders.GetByProviderIdAsync(id);
                var doctorsList = new List<ProviderDoctorDto>();

                foreach (var aff in affiliations)
                {
                    var doc = aff.DoctorProfile;
                    if (doc == null) continue;

                    // Get average rating
                    var avgResult = await _reviewService.GetDoctorAverageRatingAsync(doc.Id);
                    double avgRating = avgResult.IsSuccess ? avgResult.Data : 0;

                    // Get review count
                    var reviewsResult = await _reviewService.GetDoctorReviewsAsync(doc.Id);
                    int reviewCount = reviewsResult.IsSuccess ? (reviewsResult.Data?.Count ?? 0) : 0;

                    doctorsList.Add(new ProviderDoctorDto
                    {
                        DoctorId = doc.Id,
                        FullName = doc.FullName ?? (doc.ApplicationUser != null ? $"{doc.ApplicationUser.FirstName} {doc.ApplicationUser.LastName}".Trim() : "طبيب"),
                        Specialization = doc.Specialization ?? "أخصائي عام",
                        Bio = doc.Bio ?? "أخصائي متميز بخبرة في الرعاية الطبية الشاملة وتقديم الاستشارات المتخصصة للمرضى.",
                        ConsultationFee = doc.ConsultationFee ?? 150.00m,
                        YearsOfExperience = doc.YearsOfExperience ?? 5,
                        LicenseNumber = doc.LicenseNumber ?? string.Empty,
                        AverageRating = avgRating,
                        ReviewCount = reviewCount,
                        IsEmergencyDoctor = aff.IsEmergencyDoctor,
                        AffiliationRole = aff.AffiliationRole ?? "عضو طاقم"
                    });
                }

                ViewBag.ProviderName = provider.Name;
                ViewBag.ProviderAddress = provider.Address;
                ViewBag.ProviderType = provider.Type == "Hospital" ? "مستشفى استقبال" : "عيادة تخصصية";
                ViewBag.ProviderId = id;

                return View(doctorsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading provider doctors");
                TempData["Error"] = "حدث خطأ أثناء تحميل أطباء المنشأة";
                return RedirectToAction(nameof(Index));
            }
        }
    }

    public class ProviderDoctorDto
    {
        public int DoctorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public int YearsOfExperience { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsEmergencyDoctor { get; set; }
        public string AffiliationRole { get; set; } = string.Empty;
    }
}
