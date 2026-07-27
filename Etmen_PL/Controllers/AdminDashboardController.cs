using System;
using Etmen_BLL.Repositories.IServices;
using Etmen_BLL.Helpers;
using Etmen_PL.Models.ViewModels.Admin;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Enums;
using Etmen_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etmen_PL.Controllers
{
    
    [Authorize(Roles = "Admin,HospitalStaff")]
    public class AdminDashboardController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ICriticalIntelligenceService _criticalIntelligenceService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            IAdminService adminService,
            ICriticalIntelligenceService criticalIntelligenceService,
            IUnitOfWork uow,
            ILogger<AdminDashboardController> logger)
        {
            _adminService = adminService;
            _criticalIntelligenceService = criticalIntelligenceService;
            _uow = uow;
            _logger = logger;
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            var actionName = context.RouteData.Values["action"]?.ToString();
            if (actionName != "NormalMap" && actionName != "GetMapData" && User.IsInRole("HospitalStaff"))
            {
                context.Result = Forbid();
                return;
            }
            base.OnActionExecuting(context);
        }

        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _adminService.GetDashboardAsync();
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error loading admin dashboard";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new AdminDashboardViewModel
                {
                    TotalUsers = result.Data.TotalUsers,
                    ActiveDoctors = result.Data.ActiveDoctors,
                    ActivePatients = result.Data.ActivePatients,
                    TotalAppointments = result.Data.TotalAppointments,
                    PendingEmergencyRequests = result.Data.PendingEmergencyRequests,
                    IsCrisisModeActive = result.Data.IsCrisisModeActive,
                    ActiveCrisisName = result.Data.ActiveCrisisName
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard");
                TempData["Error"] = "خطأ في تحميل لوحة التحكم";
                return RedirectToAction("Index", "Home");
            }
        }

      
        [HttpGet]
        public IActionResult NormalMap()
        {
            return View();
        }

     
        [HttpGet]
        public async Task<IActionResult> GetMapData()
        {
            try
            {
                var providersList = await _uow.HealthcareProviders.Table
                    .Where(p => p.IsActive)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Type,
                        Latitude = (double)p.Latitude,
                        Longitude = (double)p.Longitude,
                        p.Address,
                        p.Phone
                    })
                    .ToListAsync();

                var doctorsListRaw = await _uow.DoctorProfiles.Table
                    .Include(d => d.ApplicationUser)
                    .Where(d => d.IsOnboarded && !string.IsNullOrEmpty(d.OnboardingDataJson))
                    .ToListAsync();

                var doctorsList = new List<object>();
                foreach (var doc in doctorsListRaw)
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(doc.OnboardingDataJson!);
                        if (data != null && 
                            data.TryGetValue("Latitude", out var latVal) && double.TryParse(latVal.ToString(), out var lat) &&
                            data.TryGetValue("Longitude", out var lngVal) && double.TryParse(lngVal.ToString(), out var lng))
                        {
                            doctorsList.Add(new
                            {
                                doc.Id,
                                Name = doc.FullName ?? $"{doc.ApplicationUser?.FirstName} {doc.ApplicationUser?.LastName}".Trim(),
                                doc.Specialization,
                                ConsultationFee = doc.ConsultationFee ?? 0,
                                Latitude = lat,
                                Longitude = lng
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing doctor onboarding coordinates for doc ID {DocId}", doc.Id);
                    }
                }

                // 3. Query Critical Cases (Emergency requests where Status != Completed)
                var criticalCases = await _uow.EmergencyRequests.Table
                    .Include(e => e.PatientProfile)
                    .ThenInclude(p => p.ApplicationUser)
                    .Where(e => e.Status != EmergencyRequestStatus.Completed)
                    .ToListAsync();

                var criticalList = new List<object>();
                foreach (var req in criticalCases)
                {
                    double? lat = (double?)req.Latitude;
                    double? lng = (double?)req.Longitude;

                    if ((lat == null || lat == 0) && req.PatientProfile != null)
                    {
                        lat = (double?)req.PatientProfile.Latitude;
                        lng = (double?)req.PatientProfile.Longitude;
                    }

                    if (lat.HasValue && lng.HasValue && lat != 0 && lng != 0)
                    {
                        criticalList.Add(new
                        {
                            req.Id,
                            PatientName = req.PatientProfile?.FullName ?? $"{req.PatientProfile?.ApplicationUser?.FirstName} {req.PatientProfile?.ApplicationUser?.LastName}".Trim(),
                            EmergencyType = req.EmergencyType ?? "General Emergency",
                            Description = req.Description ?? string.Empty,
                            Latitude = lat.Value,
                            Longitude = lng.Value,
                            PriorityScore = req.PriorityScore
                        });
                    }
                }

                return Json(new
                {
                    success = true,
                    providers = providersList,
                    doctors = doctorsList,
                    criticalCases = criticalList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching map markers data");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: /AdminDashboard/GetCrisisHeatmapData
        /// Returns all real outbreak zones, critical cases, and hospitals for mini crisis map
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCrisisHeatmapData(int? crisisId = null)
        {
            try
            {
                var result = await _criticalIntelligenceService.GetCrisisHeatmapAsync(crisisId);
                if (!result.IsSuccess || result.Data is null)
                {
                    return Json(new { success = false, message = result.ErrorMessage ?? "فشل تحميل بيانات الأزمات" });
                }

                return Json(new
                {
                    success = true,
                    totalCases = result.Data.TotalGeoTaggedCriticalCases,
                    zones = result.Data.Zones,
                    points = result.Data.Points,
                    hospitals = result.Data.Hospitals
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching crisis heatmap data");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: /AdminDashboard/GetDailyStats
        /// Returns real statistics for a specific date from the database
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDailyStats(int year, int month, int day)
        {
            try
            {
                var targetDate = new DateTime(year, month, day);
                var nextDate = targetDate.AddDays(1);

                // Real emergency requests count for this day
                var emergencies = await _uow.EmergencyRequests.CountAsync(
                    e => e.RequestedAt >= targetDate && e.RequestedAt < nextDate);

                // Real appointments count for this day
                var appointments = await _uow.Appointments.CountAsync(
                    a => a.AppointmentDate >= targetDate && a.AppointmentDate < nextDate);

                // Real active doctors: doctors who have appointments on this day
                var activeDoctors = await _uow.Appointments.Table
                    .Where(a => a.AppointmentDate >= targetDate && a.AppointmentDate < nextDate && a.DoctorProfileId != null)
                    .Select(a => a.DoctorProfileId)
                    .Distinct()
                    .CountAsync();

                // Real occupied beds: total available beds from all active providers
                var totalBeds = await _uow.HealthcareProviders.Table
                    .Where(p => p.IsActive)
                    .SumAsync(p => p.AvailableBeds ?? 0);

                return Json(new
                {
                    success = true,
                    day = day,
                    month = month,
                    year = year,
                    emergencies = emergencies,
                    appointments = appointments,
                    activeDoctors = activeDoctors,
                    totalBeds = totalBeds
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: /AdminDashboard/GetRangeStats
        /// Returns aggregated statistics for a date range
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRangeStats(string from, string to)
        {
            try
            {
                var fromDate = DateTime.Parse(from);
                var toDate = DateTime.Parse(to).AddDays(1); // inclusive end

                var emergencies = await _uow.EmergencyRequests.CountAsync(
                    e => e.RequestedAt >= fromDate && e.RequestedAt < toDate);

                var appointments = await _uow.Appointments.CountAsync(
                    a => a.AppointmentDate >= fromDate && a.AppointmentDate < toDate);

                var activeDoctors = await _uow.Appointments.Table
                    .Where(a => a.AppointmentDate >= fromDate && a.AppointmentDate < toDate && a.DoctorProfileId != null)
                    .Select(a => a.DoctorProfileId)
                    .Distinct()
                    .CountAsync();

                var completedEmergencies = await _uow.EmergencyRequests.CountAsync(
                    e => e.RequestedAt >= fromDate && e.RequestedAt < toDate 
                         && e.Status == EmergencyRequestStatus.Completed);

                var totalBeds = await _uow.HealthcareProviders.Table
                    .Where(p => p.IsActive)
                    .SumAsync(p => p.AvailableBeds ?? 0);

                return Json(new
                {
                    success = true,
                    fromDate = from,
                    toDate = to,
                    emergencies = emergencies,
                    appointments = appointments,
                    activeDoctors = activeDoctors,
                    completedEmergencies = completedEmergencies,
                    totalBeds = totalBeds
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: /AdminDashboard/GetRecentActivities
        /// Returns real recent system activities from the database
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecentActivities()
        {
            try
            {
                var activities = new List<object>();

                // 1. Recent Appointments (last 10)
                var recentAppointments = await _uow.Appointments.Table
                    .Include(a => a.PatientProfile)
                    .ThenInclude(p => p.ApplicationUser)
                    .Include(a => a.DoctorProfile)
                    .ThenInclude(d => d!.ApplicationUser)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                foreach (var apt in recentAppointments)
                {
                    var patientName = apt.PatientProfile?.FullName 
                        ?? $"{apt.PatientProfile?.ApplicationUser?.FirstName} {apt.PatientProfile?.ApplicationUser?.LastName}".Trim();
                    var doctorName = apt.DoctorProfile?.FullName 
                        ?? $"{apt.DoctorProfile?.ApplicationUser?.FirstName} {apt.DoctorProfile?.ApplicationUser?.LastName}".Trim();
                    
                    activities.Add(new
                    {
                        type = "appointment",
                        icon = "fa-calendar-check",
                        color = "bg-primary",
                        title = string.IsNullOrWhiteSpace(patientName) ? "مريض" : patientName,
                        description = $"حجز موعد مع د. {(string.IsNullOrWhiteSpace(doctorName) ? "طبيب" : doctorName)}",
                        timestamp = apt.CreatedAt,
                        status = apt.Status.ToString()
                    });
                }

                // 2. Recent Emergency Requests (last 10)
                var recentEmergencies = await _uow.EmergencyRequests.Table
                    .Include(e => e.PatientProfile)
                    .ThenInclude(p => p.ApplicationUser)
                    .OrderByDescending(e => e.RequestedAt)
                    .Take(10)
                    .ToListAsync();

                foreach (var emg in recentEmergencies)
                {
                    var patientName = emg.PatientProfile?.FullName 
                        ?? $"{emg.PatientProfile?.ApplicationUser?.FirstName} {emg.PatientProfile?.ApplicationUser?.LastName}".Trim();
                    
                    activities.Add(new
                    {
                        type = "emergency",
                        icon = "fa-truck-medical",
                        color = emg.Status == EmergencyRequestStatus.Pending ? "bg-danger" : "bg-warning",
                        title = string.IsNullOrWhiteSpace(patientName) ? "مريض" : patientName,
                        description = $"طلب طوارئ: {emg.EmergencyType ?? "حالة طارئة"} — {emg.Status switch { EmergencyRequestStatus.Pending => "معلق", EmergencyRequestStatus.Accepted => "تم القبول", EmergencyRequestStatus.Completed => "مكتمل", EmergencyRequestStatus.Escalated => "تم التصعيد", _ => emg.Status.ToString() }}",
                        timestamp = emg.RequestedAt,
                        status = emg.Status.ToString()
                    });
                }

                // 3. Recent Doctor Registrations (last 5)
                var recentDoctors = await _uow.DoctorProfiles.Table
                    .Include(d => d.ApplicationUser)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                foreach (var doc in recentDoctors)
                {
                    var docName = doc.FullName ?? $"{doc.ApplicationUser?.FirstName} {doc.ApplicationUser?.LastName}".Trim();
                    activities.Add(new
                    {
                        type = "doctor",
                        icon = "fa-user-md",
                        color = "bg-success",
                        title = string.IsNullOrWhiteSpace(docName) ? "طبيب جديد" : $"د. {docName}",
                        description = $"انضم للنظام — التخصص: {doc.Specialization ?? "عام"}",
                        timestamp = doc.CreatedAt,
                        status = doc.IsAvailable ? "متاح" : "غير متاح"
                    });
                }

                // 4. Recent Risk Assessments (last 5) 
                var recentRisks = await _uow.RiskAssessments.Table
                    .Include(r => r.PatientProfile)
                    .ThenInclude(p => p.ApplicationUser)
                    .Where(r => r.IsEmergency)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                foreach (var risk in recentRisks)
                {
                    var patientName = risk.PatientProfile?.FullName
                        ?? $"{risk.PatientProfile?.ApplicationUser?.FirstName} {risk.PatientProfile?.ApplicationUser?.LastName}".Trim();
                    activities.Add(new
                    {
                        type = "risk",
                        icon = "fa-triangle-exclamation",
                        color = "bg-danger",
                        title = string.IsNullOrWhiteSpace(patientName) ? "مريض" : patientName,
                        description = $"تقييم مخاطر طارئ — مستوى الخطورة: {risk.RiskLevel}",
                        timestamp = risk.CreatedAt,
                        status = risk.RiskLevel.ToString()
                    });
                }

                // Sort all by timestamp desc and take top 8
                var sorted = activities
                    .OrderByDescending(a => ((dynamic)a).timestamp)
                    .Take(8)
                    .ToList();

                return Json(new { success = true, activities = sorted });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activities");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: /AdminDashboard/GetTelemetryStats
        /// Returns real telemetry/progress bar stats from the database
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTelemetryStats()
        {
            try
            {
                // 1. Doctor availability rate
                var totalDoctors = await _uow.DoctorProfiles.CountAsync();
                var availableDoctors = await _uow.DoctorProfiles.CountAsync(d => d.IsAvailable);
                var doctorAvailabilityRate = totalDoctors > 0 
                    ? Math.Round((double)availableDoctors / totalDoctors * 100, 0) : 0;

                // 2. Bed occupancy rate
                var totalBeds = await _uow.HealthcareProviders.Table
                    .Where(p => p.IsActive)
                    .SumAsync(p => p.AvailableBeds ?? 0);
                // Estimate total capacity as available + active appointments today
                var todayStart = DateTime.UtcNow.Date;
                var todayEnd = todayStart.AddDays(1);
                var todayActiveAppointments = await _uow.Appointments.CountAsync(
                    a => a.AppointmentDate >= todayStart && a.AppointmentDate < todayEnd
                         && a.Status != AppointmentStatus.Cancelled);
                var estimatedTotalCapacity = totalBeds + todayActiveAppointments;
                var bedOccupancyRate = estimatedTotalCapacity > 0
                    ? Math.Round((double)todayActiveAppointments / estimatedTotalCapacity * 100, 0) : 0;

                // 3. Emergency response rate (accepted within 15 minutes)
                var totalEmergencies = await _uow.EmergencyRequests.CountAsync();
                var respondedQuickly = (await _uow.EmergencyRequests.Table
                    .Where(e => e.AcceptedAt != null)
                    .Select(e => new { e.RequestedAt, AcceptedAt = e.AcceptedAt!.Value })
                    .ToListAsync())
                    .Count(e => (e.AcceptedAt - e.RequestedAt).TotalMinutes <= 15);
                var emergencyResponseRate = totalEmergencies > 0
                    ? Math.Round((double)respondedQuickly / totalEmergencies * 100, 0) : 0;

                // 4. Patient satisfaction rate (from reviews)
                var totalReviews = await _uow.Reviews.Table.CountAsync();
                var avgRating = totalReviews > 0
                    ? await _uow.Reviews.Table.AverageAsync(r => r.Rating) : 0;
                var satisfactionRate = Math.Round(avgRating / 5.0 * 100, 0);

                return Json(new
                {
                    success = true,
                    doctorAvailability = doctorAvailabilityRate,
                    bedOccupancy = bedOccupancyRate,
                    emergencyResponse = emergencyResponseRate,
                    patientSatisfaction = satisfactionRate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching telemetry stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: /AdminDashboard/GetSymptomsStats
        /// Returns real symptoms distribution from risk assessments
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSymptomsStats()
        {
            try
            {
                var assessments = await _uow.RiskAssessments.Table
                    .Where(r => !string.IsNullOrEmpty(r.Symptoms))
                    .Select(r => r.Symptoms!)
                    .ToListAsync();

                // Parse symptoms and count frequencies
                var symptomCounts = new Dictionary<string, int>();
                foreach (var symptomStr in assessments)
                {
                    // Symptoms may be comma-separated or JSON array
                    var symptoms = symptomStr
                        .Replace("[", "").Replace("]", "").Replace("\"", "")
                        .Split(new[] { ',', '،', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s));

                    foreach (var symptom in symptoms)
                    {
                        if (symptomCounts.ContainsKey(symptom))
                            symptomCounts[symptom]++;
                        else
                            symptomCounts[symptom] = 1;
                    }
                }

                // Get top 5 symptoms, rest as "أخرى"
                var sortedSymptoms = symptomCounts
                    .OrderByDescending(x => x.Value)
                    .ToList();

                var topSymptoms = sortedSymptoms.Take(4).ToList();
                var othersCount = sortedSymptoms.Skip(4).Sum(x => x.Value);

                var labels = topSymptoms.Select(x => x.Key).ToList();
                var values = topSymptoms.Select(x => x.Value).ToList();

                if (othersCount > 0)
                {
                    labels.Add("أعراض أخرى");
                    values.Add(othersCount);
                }

                // Fallback if no data
                if (!labels.Any())
                {
                    labels = new List<string> { "لا توجد بيانات" };
                    values = new List<int> { 1 };
                }

                return Json(new { success = true, labels, values });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching symptoms stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetGovernorateFromRequest(EmergencyRequest r)
        {
            var address = r.HealthcareProvider?.Address;
            var description = r.Description;
            var lat = r.Latitude ?? r.PatientProfile?.Latitude;
            var lng = r.Longitude ?? r.PatientProfile?.Longitude;

            return GeoHelper.GetGovernorate(address, description, lat, lng);
        }

        /// <summary>
        /// GET: /AdminDashboard/GetGovernorateRiskStats
        /// Returns active critical cases count and risk threat level per Egyptian governorate
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetGovernorateRiskStats()
        {
            try
            {
                var activeRequests = await _uow.EmergencyRequests.Table
                    .Include(e => e.PatientProfile)
                    .Include(e => e.HealthcareProvider)
                    .Where(e => e.Status != Etmen_Domain.Enums.EmergencyRequestStatus.Completed)
                    .ToListAsync();

                var casesWithGov = activeRequests
                    .Select(r => new { Request = r, Governorate = GetGovernorateFromRequest(r) })
                    .ToList();

                var stats = casesWithGov
                    .GroupBy(x => x.Governorate)
                    .Select(g => {
                        var totalCount = g.Count();
                        var criticalCount = g.Count(x => x.Request.PriorityScore >= 80);
                        var threat = totalCount > 15 ? "خطر مرتفع جداً" :
                                     totalCount > 5 ? "متوسط الخطورة" : "مستقر";
                        return new
                        {
                            governorate = g.Key,
                            totalCases = totalCount,
                            criticalCases = criticalCount,
                            threatLevel = threat
                        };
                    })
                    .OrderByDescending(x => x.totalCases)
                    .ToList();

                return Json(new { success = true, stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: /AdminDashboard/GetDashboardProjectCharts
        /// Returns resource counts, governorate beds capacity, and appointments booking trends
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboardProjectCharts()
        {
            try
            {
                var hospitalsCount = await _uow.HealthcareProviders.CountAsync(p => p.Type == "Hospital" && p.IsActive);
                var clinicsCount = await _uow.HealthcareProviders.CountAsync(p => p.Type == "Clinic" && p.IsActive);
                var doctorsCount = await _uow.DoctorProfiles.CountAsync();

                var providers = await _uow.HealthcareProviders.Table
                    .Where(p => p.IsActive)
                    .Select(p => new { p.Address, p.Latitude, p.Longitude, p.AvailableBeds })
                    .ToListAsync();

                var bedsByGov = providers
                    .Select(p => new
                    {
                        Governorate = GeoHelper.GetGovernorate(p.Address, null, p.Latitude, p.Longitude),
                        Beds = p.AvailableBeds ?? 0
                    })
                    .GroupBy(g => g.Governorate)
                    .Select(g => new { governorate = g.Key, beds = g.Sum(x => x.Beds) })
                    .OrderByDescending(g => g.beds)
                    .ToList();

                var appointments = await _uow.Appointments.Table.ToListAsync();
                var appointmentsTrend = appointments
                    .GroupBy(a => a.AppointmentDate.DayOfWeek)
                    .Select(g => new { Day = g.Key.ToString(), Count = g.Count() })
                    .ToList();

                return Json(new
                {
                    success = true,
                    resourceDistribution = new { hospitals = hospitalsCount, clinics = clinicsCount, doctors = doctorsCount },
                    bedsByGov = bedsByGov,
                    appointmentsTrend = appointmentsTrend
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        /// <summary>
        /// GET: /AdminDashboard/GetActiveDispatchMap
        /// Returns accepted emergency requests with hospital + patient coordinates for the ambulance dispatch map
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetActiveDispatchMap()
        {
            try
            {
                // Accepted requests (ambulances currently en route)
                var accepted = await _uow.EmergencyRequests.Table
                    .Include(e => e.PatientProfile)
                    .Include(e => e.HealthcareProvider)
                    .Where(e => e.Status == Etmen_Domain.Enums.EmergencyRequestStatus.Accepted
                             && e.Latitude.HasValue && e.Longitude.HasValue
                             && e.HealthcareProvider != null)
                    .Select(e => new
                    {
                        requestId    = e.Id,
                        patientName  = (e.PatientProfile != null ? e.PatientProfile.FullName : "مريض") ?? "مريض",
                        emergencyType = e.EmergencyType ?? "طوارئ",
                        patientLat   = (double?)e.Latitude,
                        patientLng   = (double?)e.Longitude,
                        hospitalLat  = (double)e.HealthcareProvider!.Latitude,
                        hospitalLng  = (double)e.HealthcareProvider.Longitude,
                        hospitalName = e.HealthcareProvider.Name,
                        hospitalId   = e.HealthcareProvider.Id,
                        etaMinutes   = 0  // calculated client-side from distance
                    })
                    .ToListAsync();

                // All active hospitals
                var hospitals = await _uow.HealthcareProviders.Table
                    .Where(h => h.IsActive && (h.IsEmergencyCenter || h.Type == "Hospital"))
                    .Select(h => new
                    {
                        id            = h.Id,
                        name          = h.Name,
                        latitude      = (double)h.Latitude,
                        longitude     = (double)h.Longitude,
                        availableBeds = h.AvailableBeds ?? 0,
                        address       = h.Address
                    })
                    .ToListAsync();

                return Json(new { success = true, dispatches = accepted, hospitals });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active dispatch map data");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMaintenanceSettings(bool IsPatientMaintenanceActive, string PatientMaintenanceMessage, bool IsStaffMaintenanceActive, string StaffMaintenanceMessage)
        {
            try
            {
                Etmen_BLL.Helpers.MaintenanceSettingsHelper.Save(
                    IsPatientMaintenanceActive,
                    PatientMaintenanceMessage ?? "",
                    IsStaffMaintenanceActive,
                    StaffMaintenanceMessage ?? ""
                );
                TempData["Success"] = "تم تحديث إعدادات وضع الصيانة بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating maintenance settings");
                TempData["Error"] = "حدث خطأ أثناء حفظ إعدادات الصيانة";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
