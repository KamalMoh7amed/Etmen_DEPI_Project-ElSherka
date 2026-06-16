using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Etmen_PL.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorClinicController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHospitalStaffService _hospitalStaffService;
        private readonly ILogger<DoctorClinicController> _logger;

        public DoctorClinicController(
            IDoctorService doctorService,
            IUnitOfWork uow,
            UserManager<ApplicationUser> userManager,
            IHospitalStaffService hospitalStaffService,
            ILogger<DoctorClinicController> logger)
        {
            _doctorService = doctorService;
            _uow = uow;
            _userManager = userManager;
            _hospitalStaffService = hospitalStaffService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var profileResult = await _doctorService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = "فشل في تحميل ملف الطبيب";
                    return RedirectToAction("Index", "DoctorDashboard");
                }

                var doctor = profileResult.Data;
                
                // Parse OnboardingDataJson if it exists
                DoctorClinicViewModel clinicInfo = new();
                if (!string.IsNullOrEmpty(doctor.OnboardingDataJson))
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(doctor.OnboardingDataJson);
                        if (data != null)
                        {
                            clinicInfo.EntityName = data.GetValueOrDefault("EntityName")?.ToString() ?? string.Empty;
                            clinicInfo.EntityType = data.GetValueOrDefault("EntityType")?.ToString() ?? "Clinic";
                            clinicInfo.BranchArabicName = data.GetValueOrDefault("BranchArabicName")?.ToString() ?? string.Empty;
                            clinicInfo.City = data.GetValueOrDefault("City")?.ToString() ?? string.Empty;
                            clinicInfo.Area = data.GetValueOrDefault("Area")?.ToString() ?? string.Empty;
                            clinicInfo.BranchMobile = data.GetValueOrDefault("BranchMobile")?.ToString() ?? string.Empty;
                            clinicInfo.TaxId = data.GetValueOrDefault("TaxId")?.ToString() ?? string.Empty;
                            clinicInfo.CommercialRegistration = data.GetValueOrDefault("CommercialRegistration")?.ToString() ?? string.Empty;
                            clinicInfo.EntityLogoUrl = data.GetValueOrDefault("EntityLogoUrl")?.ToString();
                            
                            if (data.TryGetValue("Latitude", out var latVal) && decimal.TryParse(latVal.ToString(), out var lat))
                                clinicInfo.Latitude = lat;
                            if (data.TryGetValue("Longitude", out var lngVal) && decimal.TryParse(lngVal.ToString(), out var lng))
                                clinicInfo.Longitude = lng;
                            if (data.TryGetValue("HealthcareProviderId", out var hpIdVal) && int.TryParse(hpIdVal.ToString(), out var hpId))
                                clinicInfo.HealthcareProviderId = hpId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing onboarding JSON for doctor {UserId}", userId);
                    }
                }

                // If no HealthcareProviderId but doctor is onboarded, let's sync a provider center!
                if (clinicInfo.HealthcareProviderId == null && doctor.IsOnboarded && !string.IsNullOrEmpty(clinicInfo.EntityName))
                {
                    var newProvider = new HealthcareProvider
                    {
                        Name = clinicInfo.EntityName,
                        Type = clinicInfo.EntityType,
                        Address = $"{clinicInfo.City}, {clinicInfo.Area}",
                        Phone = clinicInfo.BranchMobile,
                        Latitude = clinicInfo.Latitude ?? 30.0444m,
                        Longitude = clinicInfo.Longitude ?? 31.2357m,
                        AvailableBeds = 0,
                        IsEmergencyCenter = false,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.HealthcareProviders.AddAsync(newProvider);
                    await _uow.CompleteAsync();

                    // Ensure doctor is associated
                    var existingAff = await _uow.DoctorProviders.Table.FirstOrDefaultAsync(dp => dp.DoctorProfileId == doctor.Id && dp.HealthcareProviderId == newProvider.Id);
                    if (existingAff == null)
                    {
                        var affiliation = new DoctorProvider
                        {
                            DoctorProfileId = doctor.Id,
                            HealthcareProviderId = newProvider.Id,
                            IsEmergencyDoctor = false,
                            IsOwner = true,
                            AffiliationRole = "المالك / طبيب رئيسي"
                        };
                        await _uow.DoctorProviders.AddAsync(affiliation);
                        await _uow.CompleteAsync();
                    }

                    // Update DoctorProfile JSON
                    clinicInfo.HealthcareProviderId = newProvider.Id;
                    await SaveClinicJsonAsync(userId, doctor, clinicInfo);
                }
                else if (clinicInfo.HealthcareProviderId.HasValue)
                {
                    // Also ensure doctor is associated to existing provider if association is missing
                    var existingAff = await _uow.DoctorProviders.Table.FirstOrDefaultAsync(dp => dp.DoctorProfileId == doctor.Id && dp.HealthcareProviderId == clinicInfo.HealthcareProviderId.Value);
                    if (existingAff == null)
                    {
                        var affiliation = new DoctorProvider
                        {
                            DoctorProfileId = doctor.Id,
                            HealthcareProviderId = clinicInfo.HealthcareProviderId.Value,
                            IsEmergencyDoctor = false,
                            IsOwner = true,
                            AffiliationRole = "المالك / طبيب رئيسي"
                        };
                        await _uow.DoctorProviders.AddAsync(affiliation);
                        await _uow.CompleteAsync();
                    }
                }

                // Get Available Slots
                var slotsResult = await _doctorService.GetAvailableSlotsAsync(doctor.Id);
                clinicInfo.Slots = slotsResult.IsSuccess ? slotsResult.Data?.ToList() ?? new() : new();

                // Get Appointments
                var appointmentsResult = await _doctorService.GetAppointmentsAsync(userId);
                clinicInfo.Appointments = appointmentsResult.IsSuccess ? appointmentsResult.Data?.ToList() ?? new() : new();

                // Get Staff Members
                if (clinicInfo.HealthcareProviderId.HasValue)
                {
                    var staffResult = await _hospitalStaffService.GetStaffMembersAsync(clinicInfo.HealthcareProviderId.Value);
                    ViewBag.StaffMembers = staffResult.IsSuccess ? staffResult.Data : new List<Etmen_BLL.DTOs.HospitalStaff.StaffProfileDto>();
                }
                else
                {
                    ViewBag.StaffMembers = new List<Etmen_BLL.DTOs.HospitalStaff.StaffProfileDto>();
                }

                return View(clinicInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering clinic dashboard for doctor");
                TempData["Error"] = "حدث خطأ في تحميل لوحة تحكم العيادة";
                return RedirectToAction("Index", "DoctorDashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var profileResult = await _doctorService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                    return NotFound();

                var doctor = profileResult.Data;
                DoctorClinicViewModel clinicInfo = new();
                if (!string.IsNullOrEmpty(doctor.OnboardingDataJson))
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(doctor.OnboardingDataJson);
                    if (data != null)
                    {
                        clinicInfo.EntityName = data.GetValueOrDefault("EntityName")?.ToString() ?? string.Empty;
                        clinicInfo.EntityType = data.GetValueOrDefault("EntityType")?.ToString() ?? "Clinic";
                        clinicInfo.BranchArabicName = data.GetValueOrDefault("BranchArabicName")?.ToString() ?? string.Empty;
                        clinicInfo.City = data.GetValueOrDefault("City")?.ToString() ?? string.Empty;
                        clinicInfo.Area = data.GetValueOrDefault("Area")?.ToString() ?? string.Empty;
                        clinicInfo.BranchMobile = data.GetValueOrDefault("BranchMobile")?.ToString() ?? string.Empty;
                        clinicInfo.TaxId = data.GetValueOrDefault("TaxId")?.ToString() ?? string.Empty;
                        clinicInfo.CommercialRegistration = data.GetValueOrDefault("CommercialRegistration")?.ToString() ?? string.Empty;
                        
                        if (data.TryGetValue("Latitude", out var latVal) && decimal.TryParse(latVal.ToString(), out var lat))
                            clinicInfo.Latitude = lat;
                        if (data.TryGetValue("Longitude", out var lngVal) && decimal.TryParse(lngVal.ToString(), out var lng))
                            clinicInfo.Longitude = lng;
                        if (data.TryGetValue("HealthcareProviderId", out var hpIdVal) && int.TryParse(hpIdVal.ToString(), out var hpId))
                            clinicInfo.HealthcareProviderId = hpId;
                    }
                }

                var model = new DoctorClinicEditModel
                {
                    EntityName = clinicInfo.EntityName,
                    EntityType = clinicInfo.EntityType,
                    BranchArabicName = clinicInfo.BranchArabicName,
                    City = clinicInfo.City,
                    Area = clinicInfo.Area,
                    BranchMobile = clinicInfo.BranchMobile,
                    TaxId = clinicInfo.TaxId,
                    CommercialRegistration = clinicInfo.CommercialRegistration,
                    Latitude = clinicInfo.Latitude ?? 30.0444m,
                    Longitude = clinicInfo.Longitude ?? 31.2357m,
                    HealthcareProviderId = clinicInfo.HealthcareProviderId
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening edit clinic view");
                TempData["Error"] = "حدث خطأ في تحميل صفحة التعديل";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DoctorClinicEditModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var profileResult = await _doctorService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                    return NotFound();

                var doctor = profileResult.Data;

                // Sync to HealthcareProvider table
                HealthcareProvider? provider = null;
                if (model.HealthcareProviderId.HasValue)
                {
                    provider = await _uow.HealthcareProviders.GetByIdAsync(model.HealthcareProviderId.Value);
                }

                if (provider == null)
                {
                    provider = new HealthcareProvider
                    {
                        CreatedAt = DateTime.UtcNow
                    };
                }

                provider.Name = model.EntityName;
                provider.Type = model.EntityType;
                provider.Address = $"{model.City}, {model.Area}";
                provider.Phone = model.BranchMobile;
                provider.Latitude = model.Latitude;
                provider.Longitude = model.Longitude;
                provider.IsActive = true;

                if (model.HealthcareProviderId.HasValue)
                    _uow.HealthcareProviders.Update(provider);
                else
                    await _uow.HealthcareProviders.AddAsync(provider);

                await _uow.CompleteAsync();

                // Ensure doctor is associated
                var existingAff = await _uow.DoctorProviders.Table.FirstOrDefaultAsync(dp => dp.DoctorProfileId == doctor.Id && dp.HealthcareProviderId == provider.Id);
                if (existingAff == null)
                {
                    var affiliation = new DoctorProvider
                    {
                        DoctorProfileId = doctor.Id,
                        HealthcareProviderId = provider.Id,
                        IsEmergencyDoctor = false,
                        IsOwner = true,
                        AffiliationRole = "المالك / طبيب رئيسي"
                    };
                    await _uow.DoctorProviders.AddAsync(affiliation);
                    await _uow.CompleteAsync();
                }

                // Save updated JSON
                var updatedInfo = new DoctorClinicViewModel
                {
                    EntityName = model.EntityName,
                    EntityType = model.EntityType,
                    BranchArabicName = model.BranchArabicName,
                    City = model.City,
                    Area = model.Area,
                    BranchMobile = model.BranchMobile,
                    TaxId = model.TaxId,
                    CommercialRegistration = model.CommercialRegistration,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    HealthcareProviderId = provider.Id
                };

                await SaveClinicJsonAsync(userId, doctor, updatedInfo);

                TempData["Success"] = "تم تحديث بيانات عيادتك وتحديث موقعها على الخريطة بنجاح!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving clinic updates");
                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ التعديلات. يرجى المحاولة مرة أخرى.");
                return View(model);
            }
        }

        private async Task SaveClinicJsonAsync(string userId, Etmen_BLL.DTOs.Doctor.DoctorProfileDto doctor, DoctorClinicViewModel info)
        {
            var dict = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(doctor.OnboardingDataJson))
            {
                try
                {
                    dict = JsonSerializer.Deserialize<Dictionary<string, object>>(doctor.OnboardingDataJson) ?? new();
                }
                catch { }
            }

            dict["EntityName"] = info.EntityName;
            dict["EntityType"] = info.EntityType;
            dict["BranchArabicName"] = info.BranchArabicName;
            dict["City"] = info.City;
            dict["Area"] = info.Area;
            dict["BranchMobile"] = info.BranchMobile;
            dict["TaxId"] = info.TaxId;
            dict["CommercialRegistration"] = info.CommercialRegistration;
            dict["Latitude"] = info.Latitude ?? 30.0444m;
            dict["Longitude"] = info.Longitude ?? 31.2357m;
            if (info.HealthcareProviderId.HasValue)
                dict["HealthcareProviderId"] = info.HealthcareProviderId.Value;

            doctor.OnboardingDataJson = JsonSerializer.Serialize(dict);
            await _doctorService.UpdateProfileAsync(userId, doctor);
        }
    }

    public class DoctorClinicViewModel
    {
        public string EntityName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string BranchArabicName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string BranchMobile { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string CommercialRegistration { get; set; } = string.Empty;
        public string? EntityLogoUrl { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? HealthcareProviderId { get; set; }

        public List<Etmen_BLL.DTOs.Nearby.AvailableSlotDto> Slots { get; set; } = new();
        public List<Etmen_BLL.DTOs.Doctor.DoctorAppointmentDto> Appointments { get; set; } = new();
    }

    public class DoctorClinicEditModel
    {
        public string EntityName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string BranchArabicName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string BranchMobile { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string CommercialRegistration { get; set; } = string.Empty;
        public decimal Latitude { get; set; } = 30.0444m;
        public decimal Longitude { get; set; } = 31.2357m;
        public int? HealthcareProviderId { get; set; }
    }
}
