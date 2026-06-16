using Etmen_BLL.DTOs.HospitalStaff;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Hospital;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Hospital Queue Controller
    /// Monitors incoming ambulances and manages bed availability
    /// </summary>
    [Authorize(Roles = "HospitalStaff")]
    public class HospitalQueueController : Controller
    {
        private readonly IHospitalStaffService _hospitalStaffService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<HospitalQueueController> _logger;

        public HospitalQueueController(
            IHospitalStaffService hospitalStaffService,
            IUnitOfWork uow,
            ILogger<HospitalQueueController> logger)
        {
            _hospitalStaffService = hospitalStaffService;
            _uow = uow;
            _logger = logger;
        }

        private async Task<int?> GetCurrentProviderIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var profile = await _uow.StaffProfiles.Table.FirstOrDefaultAsync(sp => sp.ApplicationUserId == userId);
            return profile?.HealthcareProviderId;
        }

        /// <summary>
        /// GET: /HospitalQueue/Index
        /// Lists active ambulance triages
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var providerId = await GetCurrentProviderIdAsync();
                if (!providerId.HasValue)
                {
                    TempData["Error"] = "حسابك غير مرتبط بأي منشأة طبية. يرجى مراجعة مسؤول النظام.";
                    return RedirectToAction("AccessDenied", "Account");
                }

                var result = await _hospitalStaffService.GetQueueAsync(providerId.Value);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "حدث خطأ أثناء تحميل قائمة الانتظار.";
                    return View(new HospitalQueueViewModel());
                }

                var viewModel = MapQueue(result.Data);

                var transferRequests = await _uow.EmergencyRequests.Table
                    .Include(e => e.AssignedDoctor)
                    .Where(e => e.HealthcareProviderId == providerId.Value && e.EmergencyType == "DoctorTransfer")
                    .ToListAsync();

                foreach (var item in viewModel.Items)
                {
                    if (item.EmergencyType == "DoctorTransfer")
                    {
                        var req = transferRequests.FirstOrDefault(r => r.Id == item.RequestId);
                        if (req != null)
                        {
                            var doctorProfile = await _uow.DoctorProfiles.Table
                                .FirstOrDefaultAsync(d => d.ApplicationUserId == req.AssignedDoctorUserId);
                            item.ReferringDoctorName = doctorProfile?.FullName ?? (req.AssignedDoctor != null ? $"{req.AssignedDoctor.FirstName} {req.AssignedDoctor.LastName}".Trim() : "طبيب معالج");
                            item.Notes = req.Description;
                        }
                    }
                }

                var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId.Value);
                if (provider != null)
                {
                    viewModel.ProviderName = provider.Name;
                    viewModel.ProviderAddress = provider.Address;
                    viewModel.AvailableBeds = provider.AvailableBeds;
                    viewModel.ProviderLatitude = provider.Latitude;
                    viewModel.ProviderLongitude = provider.Longitude;
                }

                var adminUser = await _uow.Users.FirstOrDefaultAsync(u => u.Email == "admin@etmen.com");
                if (adminUser != null)
                {
                    viewModel.AdminUserId = adminUser.Id;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hospital queue");
                TempData["Error"] = "خطأ في تحميل قائمة الانتظار";
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        /// <summary>
        /// GET: /HospitalQueue/Details
        /// Displays detailed medical context of the emergency patient
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "معرّف طلب الطوارئ غير صحيح.";
                    return RedirectToAction(nameof(Index));
                }

                var providerId = await GetCurrentProviderIdAsync();
                if (!providerId.HasValue)
                {
                    TempData["Error"] = "حسابك غير مرتبط بأي منشأة طبية.";
                    return RedirectToAction("AccessDenied", "Account");
                }

                var result = await _hospitalStaffService.GetRequestDetailAsync(id, providerId.Value);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "لم يتم العثور على طلب الطوارئ.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = MapDetail(result.Data);

                var request = await _uow.EmergencyRequests.Table
                    .Include(r => r.AssignedDoctor)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (request != null)
                {
                    viewModel.AssignedDoctorUserId = request.AssignedDoctorUserId;
                    if (request.EmergencyType == "DoctorTransfer" && !string.IsNullOrEmpty(request.AssignedDoctorUserId))
                    {
                        var doctorProfile = await _uow.DoctorProfiles.Table
                            .FirstOrDefaultAsync(d => d.ApplicationUserId == request.AssignedDoctorUserId);
                        viewModel.ReferringDoctorName = doctorProfile?.FullName ?? (request.AssignedDoctor != null ? $"{request.AssignedDoctor.FirstName} {request.AssignedDoctor.LastName}".Trim() : "طبيب معالج");
                    }
                }

                var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId.Value);
                if (provider != null)
                {
                    ViewBag.HospitalLat = provider.Latitude;
                    ViewBag.HospitalLng = provider.Longitude;
                }

                var allDoctors = await _uow.DoctorProfiles.Table
                    .Include(d => d.ApplicationUser)
                    .Where(d => d.IsOnboarded && !string.IsNullOrEmpty(d.OnboardingDataJson))
                    .ToListAsync();
                
                viewModel.AvailableDoctors = allDoctors.Where(d => {
                    try
                    {
                        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(d.OnboardingDataJson!);
                        if (data != null && data.TryGetValue("HealthcareProviderId", out var hpIdVal) && int.TryParse(hpIdVal.ToString(), out var hpId))
                        {
                            return hpId == providerId.Value;
                        }
                    }
                    catch {}
                    return false;
                }).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving request details");
                TempData["Error"] = "خطأ في تحميل التفاصيل";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /HospitalQueue/Respond
        /// Hospital staff accepts or rejects the request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(HospitalRespondViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index));

            try
            {
                var providerId = await GetCurrentProviderIdAsync();
                if (!providerId.HasValue || viewModel.ProviderId != providerId.Value)
                {
                    TempData["Error"] = "ليس لديك صلاحية لتنفيذ هذا الإجراء.";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                var dto = new HospitalStaffEmergencyRespondDto
                {
                    RequestId = viewModel.RequestId,
                    ProviderId = viewModel.ProviderId,
                    Status = viewModel.Status,
                    ResponseNotes = viewModel.ResponseNotes
                };

                var result = await _hospitalStaffService.RespondToRequestAsync(dto, userId);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "خطأ في الاستجابة للطلب.";
                    return RedirectToAction(nameof(Details), new { id = viewModel.RequestId });
                }

                if (viewModel.Status == "Accepted" && !string.IsNullOrEmpty(viewModel.AssignedDoctorUserId))
                {
                    var request = await _uow.EmergencyRequests.GetByIdAsync(viewModel.RequestId);
                    if (request != null)
                    {
                        request.AssignedDoctorUserId = viewModel.AssignedDoctorUserId;
                        request.DoctorAssignedAt = DateTime.UtcNow;
                        request.DoctorsNotified = true;
                        request.DoctorsNotifiedAt = DateTime.UtcNow;
                        _uow.EmergencyRequests.Update(request);
                        await _uow.CompleteAsync();
                    }
                }

                _logger.LogInformation("Response {Status} provided to emergency request {RequestId}", viewModel.Status, viewModel.RequestId);
                TempData["Success"] = "تم تسجيل الرد وتعيين الطبيب بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to request");
                TempData["Error"] = "خطأ في تسجيل الرد";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /HospitalQueue/UpdateBeds
        /// Modifies the hospital's available emergency beds configuration
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBeds(HospitalBedsUpdateViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index));

            try
            {
                var providerId = await GetCurrentProviderIdAsync();
                if (!providerId.HasValue || viewModel.ProviderId != providerId.Value)
                {
                    TempData["Error"] = "غير مصرح لك بتعديل بيانات هذا المستشفى.";
                    return RedirectToAction(nameof(Index));
                }

                var dto = new HospitalStaffBedsUpdateDto
                {
                    ProviderId = viewModel.ProviderId,
                    AvailableBeds = viewModel.AvailableBeds
                };

                var result = await _hospitalStaffService.UpdateBedsAsync(dto);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "خطأ في تحديث عدد الأسرة.";
                    return RedirectToAction(nameof(Index));
                }

                // Log staff activity
                var staffUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var staffProfile = await _uow.StaffProfiles.Table.FirstOrDefaultAsync(sp => sp.ApplicationUserId == staffUser);
                if (staffProfile != null)
                {
                    await _hospitalStaffService.LogActivityAsync(staffProfile.Id, "UpdateBeds", $"تم تحديث الأسرة الشاغرة إلى {viewModel.AvailableBeds}");
                }

                _logger.LogInformation("Hospital beds updated for provider {ProviderId}", viewModel.ProviderId);
                TempData["Success"] = "تم تحديث الأسرة المتاحة بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating beds");
                TempData["Error"] = "خطأ في تحديث الأسرة";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /HospitalQueue/RequestSupport
        /// Sends an urgent support request alert to the Admin
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSupport(int providerId)
        {
            try
            {
                var currentProviderId = await GetCurrentProviderIdAsync();
                if (!currentProviderId.HasValue || providerId != currentProviderId.Value)
                {
                    TempData["Error"] = "غير مصرح لك بطلب دعم لهذا المستشفى.";
                    return RedirectToAction(nameof(Index));
                }

                var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
                if (provider == null)
                {
                    TempData["Error"] = "المستشفى غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                var adminUser = await _uow.Users.FirstOrDefaultAsync(u => u.Email == "admin@etmen.com");
                if (adminUser != null)
                {
                    var alert = new Alert
                    {
                        UserId = adminUser.Id,
                        Title = $"طلب دعم عاجل: {provider.Name}",
                        Message = $"أرسل طاقم {provider.Name} طلب دعم عاجل لغرفة العمليات المركزية لإدارة الأزمات نتيجة ضغط العمل ونقص الأسرة.",
                        AlertType = "Emergency",
                        Status = AlertStatus.Unread,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _uow.Alerts.AddAsync(alert);
                    await _uow.CompleteAsync();
                }

                // Log staff activity
                var staffUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var staffProfile = await _uow.StaffProfiles.Table.FirstOrDefaultAsync(sp => sp.ApplicationUserId == staffUser);
                if (staffProfile != null)
                {
                    await _hospitalStaffService.LogActivityAsync(staffProfile.Id, "RequestSupport", "تم إرسال طلب دعم عاجل للأدمن");
                }

                TempData["Success"] = "تم إرسال طلب الدعم العاجل للأدمن بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending support request to admin");
                TempData["Error"] = "خطأ في إرسال طلب الدعم";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /HospitalQueue/UpdateProfile
        /// Modifies the hospital's name, region/address, and beds configuration
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(int providerId, string providerName, string providerAddress, int availableBeds)
        {
            try
            {
                var currentProviderId = await GetCurrentProviderIdAsync();
                if (!currentProviderId.HasValue || providerId != currentProviderId.Value)
                {
                    TempData["Error"] = "غير مصرح لك بتعديل بيانات هذا المستشفى.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(providerName))
                {
                    TempData["Error"] = "اسم المستشفى مطلوب";
                    return RedirectToAction(nameof(Index));
                }

                var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
                if (provider == null)
                {
                    TempData["Error"] = "المستشفى غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                var oldName = provider.Name;
                provider.Name = providerName.Trim();
                provider.Address = string.IsNullOrWhiteSpace(providerAddress) ? provider.Address : providerAddress.Trim();
                provider.AvailableBeds = availableBeds >= 0 ? availableBeds : 0;

                _uow.HealthcareProviders.Update(provider);
                await _uow.CompleteAsync();

                // Log staff activity
                var staffUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var staffProfile = await _uow.StaffProfiles.Table.FirstOrDefaultAsync(sp => sp.ApplicationUserId == staffUser);
                if (staffProfile != null)
                {
                    await _hospitalStaffService.LogActivityAsync(staffProfile.Id, "UpdateProfile", $"تم تعديل ملف المستشفى (الاسم القديم: {oldName})");
                }

                _logger.LogInformation("Hospital profile updated for provider {ProviderId}", providerId);
                TempData["Success"] = "تم تحديث بيانات المستشفى بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider profile");
                TempData["Error"] = "خطأ أثناء تحديث بيانات المستشفى";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /HospitalQueue/GetPendingCount
        /// Endpoint to poll for new pending emergency requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPendingCount()
        {
            try
            {
                var providerId = await GetCurrentProviderIdAsync();
                if (!providerId.HasValue) return Json(new { success = false, count = 0 });

                var result = await _hospitalStaffService.GetQueueAsync(providerId.Value);
                if (result.IsSuccess && result.Data != null)
                {
                    return Json(new { success = true, count = result.Data.PendingCount });
                }
                return Json(new { success = false, count = 0 });
            }
            catch
            {
                return Json(new { success = false, count = 0 });
            }
        }

        /// <summary>
        /// GET: /HospitalQueue/ExportReport
        /// Exports active hospital queue statistics and case details as a CSV spreadsheet
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportReport()
        {
            try
            {
                var providerId = await GetCurrentProviderIdAsync();
                if (!providerId.HasValue)
                {
                    TempData["Error"] = "غير مصرح لك بتحميل تقارير هذه المنشأة.";
                    return RedirectToAction("AccessDenied", "Account");
                }

                var result = await _hospitalStaffService.GetQueueAsync(providerId.Value);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = "فشل في إنشاء التقرير";
                    return RedirectToAction(nameof(Index));
                }

                var queue = result.Data;
                var csv = new System.Text.StringBuilder();
                
                csv.AppendLine($"تقرير الطوارئ والفرز اليومي - {queue.ProviderName}");
                csv.AppendLine($"تاريخ الاستخراج,{DateTime.Now:g}");
                csv.AppendLine($"الأسرة الشاغرة,{queue.AvailableBeds}");
                csv.AppendLine($"الحالات المعلقة,{queue.PendingCount}");
                csv.AppendLine($"الحالات المقبولة,{queue.AcceptedCount}");
                csv.AppendLine($"الحالات المتدهورة,{queue.EscalatedCount}");
                csv.AppendLine();

                csv.AppendLine("معرف الطلب,اسم المريض,رقم الهاتف,نوع الطوارئ,درجة الأولوية,الحالة,زمن الانتظار (دقيقة),تاريخ الطلب");

                foreach (var item in queue.Items)
                {
                    var statusText = item.Status == EmergencyRequestStatus.Pending ? "معلق" :
                                     item.Status == EmergencyRequestStatus.Accepted ? "مقبول" :
                                     item.Status == EmergencyRequestStatus.Escalated ? "متدهور!" : item.Status.ToString();
                    
                    csv.AppendLine($"{item.RequestId},{item.PatientName},{item.PatientPhone},{item.EmergencyType},{item.PriorityScore}%,{statusText},{item.WaitingMinutes},{item.RequestedAt:g}");
                }

                var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
                var fileName = $"EmergencyReport_{queue.ProviderId ?? 0}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(bytes, "text/csv; charset=utf-8", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting emergency CSV report");
                TempData["Error"] = "حدث خطأ أثناء تصدير التقرير";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── Accept / Decline Invitation Actions ───────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AcceptInvitation(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "توكن الدعوة غير صالح.";
                return RedirectToAction("Login", "Account");
            }

            var profile = await _uow.StaffProfiles.Table
                .Include(p => p.HealthcareProvider)
                .FirstOrDefaultAsync(p => p.InvitationToken == token);

            if (profile == null)
            {
                TempData["Error"] = "رابط الدعوة غير صحيح أو انتهت صلاحيته.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Token = token;
            ViewBag.ProviderName = profile.HealthcareProvider.Name;
            ViewBag.RoleName = profile.RoleType == StaffRoleType.Receptionist ? "موظف استقبال" : "موظف طوارئ وفرز";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAccept(string token)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _hospitalStaffService.AcceptInvitationAsync(token, userId);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.ErrorMessage ?? "فشل قبول الدعوة.";
                return RedirectToAction("Login", "Account");
            }

            TempData["Success"] = "تم قبول الدعوة وتفعيل حسابك بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineInvitation(string token)
        {
            var profile = await _uow.StaffProfiles.Table.FirstOrDefaultAsync(p => p.InvitationToken == token);
            if (profile != null)
            {
                await _hospitalStaffService.CancelInvitationAsync(profile.Id);
            }

            var signInManager = HttpContext.RequestServices.GetService(typeof(SignInManager<ApplicationUser>)) as SignInManager<ApplicationUser>;
            if (signInManager != null)
            {
                await signInManager.SignOutAsync();
            }

            TempData["Success"] = "تم رفض الدعوة بنجاح.";
            return RedirectToAction("Login", "Account");
        }

        // ── Private Map Helpers ───────────────────────────────────────────────

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
                PriorityScore = item.PriorityScore,
                Latitude = item.Latitude,
                Longitude = item.Longitude
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
