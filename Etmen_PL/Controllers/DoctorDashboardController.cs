using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Doctor;
using Etmen_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Doctor Dashboard Controller
    /// Displays doctor landing dashboard metrics, scheduling statistics, and onboarding flow.
    /// </summary>
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<DoctorDashboardController> _logger;

        public DoctorDashboardController(
            IDoctorService doctorService,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork uow,
            ILogger<DoctorDashboardController> logger)
        {
            _doctorService = doctorService;
            _userManager = userManager;
            _uow = uow;
            _logger = logger;
        }

        /// <summary>
        /// GET: /DoctorDashboard/Index
        /// Displays schedule summary and statistics charts
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                _logger.LogInformation("Doctor dashboard accessed for user {UserId}", userId);

                var dashboardResult = await _doctorService.GetDashboardAsync(userId);

                if (!dashboardResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to fetch dashboard data for doctor {UserId}", userId);
                    ModelState.AddModelError(string.Empty, "Failed to load dashboard. Please try again later.");
                    var emptyViewModel = new DoctorDashboardViewModel();
                    return View(emptyViewModel);
                }

                var viewModel = new DoctorDashboardViewModel
                {
                    DoctorName = dashboardResult.Data?.DoctorName ?? "",
                    Specialization = dashboardResult.Data?.Specialization,
                    TodayAppointmentsCount = dashboardResult.Data?.TodayAppointmentsCount ?? 0,
                    PendingAppointmentsCount = dashboardResult.Data?.PendingAppointmentsCount ?? 0,
                    TotalPatientsCount = dashboardResult.Data?.TotalPatientsCount ?? 0,
                    AverageRating = dashboardResult.Data?.AverageRating,
                    UpcomingAppointments = dashboardResult.Data?.UpcomingAppointments ?? new List<Etmen_BLL.DTOs.Doctor.UpcomingAppointmentDto>(),
                    RecentPatients = dashboardResult.Data?.RecentPatients ?? new List<Etmen_BLL.DTOs.Doctor.RecentPatientDto>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor dashboard");
                TempData["Error"] = "Error loading dashboard";
                return View(new DoctorDashboardViewModel());
            }
        }

        /// <summary>
        /// GET: /DoctorDashboard/Statistics
        /// Returns dashboard statistics as JSON for AJAX/widget updates
        /// </summary>
        [HttpGet]
        [Route("DoctorDashboard/Statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var statsResult = await _doctorService.GetStatisticsAsync(userId);

                if (!statsResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to fetch statistics for doctor {UserId}", userId);
                    return Json(new { isSuccess = false, message = "Failed to load statistics" });
                }

                return Json(new
                {
                    isSuccess = true,
                    data = statsResult.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching statistics");
                return Json(new { isSuccess = false, message = "An error occurred while fetching statistics" });
            }
        }

        /// <summary>
        /// GET: /DoctorDashboard/Onboarding
        /// Displays the multi-step doctor onboarding wizard.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Onboarding()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                var profileResult = await _doctorService.GetProfileAsync(userId);

                if (user == null || !profileResult.IsSuccess)
                {
                    return NotFound("Doctor account not found.");
                }

                // If doctor is already onboarded, send them to the main dashboard
                if (profileResult.Data != null && profileResult.Data.IsOnboarded)
                {
                    return RedirectToAction(nameof(Index));
                }

                // Pass the current user and doctor info to prefill the first step of the wizard
                var input = new DoctorOnboardingInputModel
                {
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Speciality = profileResult.Data?.Specialization ?? ""
                };

                return View(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening onboarding wizard");
                return View(new DoctorOnboardingInputModel());
            }
        }

        /// <summary>
        /// POST: /DoctorDashboard/SaveOnboarding
        /// Saves all onboarding wizard inputs, updates user/profile, and marks doctor as onboarded.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveOnboarding(
            DoctorOnboardingInputModel input,
            IFormFile? profilePicFile,
            IFormFile? entityLogoFile)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                var profileResult = await _doctorService.GetProfileAsync(userId);

                if (user == null || !profileResult.IsSuccess)
                {
                    return NotFound("Doctor account not found.");
                }

                if (!ModelState.IsValid)
                {
                    return View("Onboarding", input);
                }

                // 1. Update ApplicationUser fields
                user.FirstName = input.FirstName;
                user.LastName = input.LastName;
                user.PhoneNumber = input.PhoneNumber;

                // Handle profile picture upload if provided
                if (profilePicFile != null && profilePicFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(profilePicFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicFile.CopyToAsync(fileStream);
                    }
                    user.ProfilePicture = $"/uploads/{uniqueFileName}";
                }

                var userUpdateResult = await _userManager.UpdateAsync(user);
                if (!userUpdateResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Failed to update user personal info.");
                    return View("Onboarding", input);
                }

                // Handle entity logo upload if provided
                string? entityLogoPath = null;
                if (entityLogoFile != null && entityLogoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(entityLogoFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await entityLogoFile.CopyToAsync(fileStream);
                    }
                    entityLogoPath = $"/uploads/{uniqueFileName}";
                }

                // 2. Package the onboarding data as JSON
                var onboardingData = new
                {
                    EntityType = input.EntityType,
                    EntityName = input.EntityName,
                    EntityLogoUrl = entityLogoPath,
                    PricingOption = input.PricingOption,
                    BranchType = input.BranchType,
                    BranchEnglishName = input.BranchEnglishName,
                    BranchArabicName = input.BranchArabicName,
                    City = input.City,
                    Area = input.Area,
                    BranchMobile = input.BranchMobile,
                    SmsMobile = input.SmsMobile,
                    TaxId = input.TaxId,
                    CommercialRegistration = input.CommercialRegistration,
                    OnboardedAt = DateTime.UtcNow
                };
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(onboardingData);

                // 3. Update DoctorProfile using DoctorService
                var profileDto = profileResult.Data;
                if (profileDto == null)
                {
                    ModelState.AddModelError(string.Empty, "فشل العثور على ملف الطبيب.");
                    return View("Onboarding", input);
                }
                profileDto.FullName = $"{input.FirstName} {input.LastName}".Trim();
                profileDto.Specialization = input.Speciality;
                profileDto.IsOnboarded = true;
                profileDto.OnboardingDataJson = jsonPayload;

                var updateProfileResult = await _doctorService.UpdateProfileAsync(userId, profileDto);
                if (!updateProfileResult.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Failed to complete onboarding profile setup.");
                    return View("Onboarding", input);
                }

                _logger.LogInformation("Doctor {UserId} onboarding completed successfully.", userId);
                HttpContext.Session.SetInt32($"DoctorOnboarded_{userId}", 1);
                TempData["Success"] = "تم تفعيل حسابك وإنشاء ملفك الشخصي وعيادتك بنجاح!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving doctor onboarding data");
                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ البيانات. يرجى المحاولة مرة أخرى.");
                return View("Onboarding", input);
            }
        }

        [HttpGet]
        [Route("DoctorDashboard/Schedule")]
        public async Task<IActionResult> GetSchedule(DateTime date)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var profileResult = await _doctorService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                    return Json(new { success = false, message = "Doctor profile not found." });

                var doctorId = profileResult.Data.Id;
                
                var appointments = await _uow.Appointments.Table
                    .Include(a => a.PatientProfile)
                    .Where(a => a.DoctorProfileId == doctorId && a.AppointmentDate.Date == date.Date)
                    .OrderBy(a => a.StartTime)
                    .ToListAsync();

                var slots = await _uow.AvailableSlots.Table
                    .Where(s => s.DoctorProfileId == doctorId && s.SlotDate.Date == date.Date)
                    .OrderBy(s => s.SlotStart)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    appointments = appointments.Select(a => new
                    {
                        id = a.Id,
                        patientName = a.PatientProfile?.FullName ?? "مريض",
                        startTime = a.StartTime.ToString(@"hh\:mm"),
                        endTime = a.EndTime.ToString(@"hh\:mm"),
                        status = a.Status.ToString(),
                        notes = a.Notes ?? ""
                    }),
                    slots = slots.Select(s => new
                    {
                        id = s.Id,
                        startTime = s.SlotStart.ToString(@"hh\:mm"),
                        endTime = s.SlotEnd.ToString(@"hh\:mm"),
                        isBooked = s.IsBooked
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule for date {Date}", date);
                return Json(new { success = false, message = "حدث خطأ أثناء تحميل جدول اليوم." });
            }
        }

        [HttpPost]
        [Route("DoctorDashboard/SaveNotes")]
        public async Task<IActionResult> SaveNotes(int appointmentId, string notes, string status)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Unauthorized" });

                var statusDto = new Etmen_BLL.DTOs.Doctor.UpdateAppointmentStatusDto
                {
                    Status = status,
                    Notes = notes
                };

                var result = await _doctorService.UpdateAppointmentStatusAsync(userId, appointmentId, statusDto);
                if (result.IsSuccess)
                    return Json(new { success = true, message = "تم حفظ الملاحظات وتحديث الحالة بنجاح." });
                return Json(new { success = false, message = result.Errors.FirstOrDefault() ?? "فشل تحديث الموعد." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notes for appointment {AppointmentId}", appointmentId);
                return Json(new { success = false, message = "حدث خطأ غير متوقع." });
            }
        }
    }

    /// <summary>
    /// Model for Doctor onboarding form inputs
    /// </summary>
    public class DoctorOnboardingInputModel
    {
        // Step 1: Personal Info
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Speciality { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // Step 2: Entity Details
        public string EntityType { get; set; } = string.Empty; // Center, Hospital, Clinics
        public string EntityName { get; set; } = string.Empty;
        
        // Step 4: Branch & Book Setup
        public string PricingOption { get; set; } = string.Empty; // MonthlyFees, Transaction
        public string BranchType { get; set; } = string.Empty;
        public string BranchEnglishName { get; set; } = string.Empty;
        public string BranchArabicName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string BranchMobile { get; set; } = string.Empty;
        public string SmsMobile { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string CommercialRegistration { get; set; } = string.Empty;
    }
}
