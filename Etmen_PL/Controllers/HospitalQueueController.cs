using Etmen_BLL.DTOs.HospitalStaff;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Hospital;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>
        /// GET: /HospitalQueue/Index
        /// Lists active ambulance triages
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int? providerId = null)
        {
            try
            {
                if (!providerId.HasValue)
                {
                    var firstCenter = await _uow.HealthcareProviders.FirstOrDefaultAsync(p => p.IsEmergencyCenter && p.IsActive);
                    if (firstCenter != null)
                    {
                        providerId = firstCenter.Id;
                    }
                }

                var result = await _hospitalStaffService.GetQueueAsync(providerId);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error loading hospital queue";
                    return View(new HospitalQueueViewModel());
                }

                var viewModel = MapQueue(result.Data);

                // Fetch extra provider info
                if (providerId.HasValue)
                {
                    var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId.Value);
                    if (provider != null)
                    {
                        viewModel.ProviderName = provider.Name;
                        viewModel.ProviderAddress = provider.Address;
                        viewModel.AvailableBeds = provider.AvailableBeds;
                    }
                }

                // Fetch admin user ID for communication/chat
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
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// GET: /HospitalQueue/Details
        /// Displays detailed medical context of the emergency patient
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id, int? providerId = null)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid emergency request id";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _hospitalStaffService.GetRequestDetailAsync(id, providerId);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Emergency request not found";
                    return RedirectToAction(nameof(Index), new { providerId });
                }

                var viewModel = MapDetail(result.Data);

                // Fetch extra request details (Assigned Doctor)
                var request = await _uow.EmergencyRequests.GetByIdAsync(id);
                if (request != null)
                {
                    viewModel.AssignedDoctorUserId = request.AssignedDoctorUserId;
                }

                // If no providerId passed, fallback to assigned provider ID of the request
                var activeProviderId = providerId ?? result.Data.AssignedProviderId;
                if (activeProviderId.HasValue)
                {
                    var provider = await _uow.HealthcareProviders.GetByIdAsync(activeProviderId.Value);
                    if (provider != null)
                    {
                        ViewBag.HospitalLat = provider.Latitude;
                        ViewBag.HospitalLng = provider.Longitude;
                    }

                    // Fetch available doctors of this provider
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
                                return hpId == activeProviderId.Value;
                            }
                        }
                        catch {}
                        return false;
                    }).ToList();
                }

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
                    TempData["Error"] = result.ErrorMessage ?? "Error responding to request";
                    return RedirectToAction(nameof(Details), new { id = viewModel.RequestId, providerId = viewModel.ProviderId });
                }

                // Save doctor assignment
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
                return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });
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
                var dto = new HospitalStaffBedsUpdateDto
                {
                    ProviderId = viewModel.ProviderId,
                    AvailableBeds = viewModel.AvailableBeds
                };

                var result = await _hospitalStaffService.UpdateBedsAsync(dto);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error updating beds";
                    return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });
                }

                _logger.LogInformation("Hospital beds updated for provider {ProviderId}", viewModel.ProviderId);
                TempData["Success"] = "تم تحديث الأسرة المتاحة بنجاح";
                return RedirectToAction(nameof(Index), new { providerId = viewModel.ProviderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating beds");
                TempData["Error"] = "خطأ في تحديث الأسرة";
                return RedirectToAction(nameof(Index));
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

                TempData["Success"] = "تم إرسال طلب الدعم العاجل للأدمن بنجاح";
                return RedirectToAction(nameof(Index), new { providerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending support request to admin");
                TempData["Error"] = "خطأ في إرسال طلب الدعم";
                return RedirectToAction(nameof(Index), new { providerId });
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
                if (providerId <= 0)
                {
                    TempData["Error"] = "معرف المستشفى غير صحيح";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(providerName))
                {
                    TempData["Error"] = "اسم المستشفى مطلوب";
                    return RedirectToAction(nameof(Index), new { providerId });
                }

                var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
                if (provider == null)
                {
                    TempData["Error"] = "المستشفى غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                provider.Name = providerName.Trim();
                provider.Address = string.IsNullOrWhiteSpace(providerAddress) ? provider.Address : providerAddress.Trim();
                provider.AvailableBeds = availableBeds >= 0 ? availableBeds : 0;

                _uow.HealthcareProviders.Update(provider);
                await _uow.CompleteAsync();

                _logger.LogInformation("Hospital profile updated for provider {ProviderId}", providerId);
                TempData["Success"] = "تم تحديث بيانات المستشفى بنجاح";
                return RedirectToAction(nameof(Index), new { providerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider profile");
                TempData["Error"] = "خطأ أثناء تحديث بيانات المستشفى";
                return RedirectToAction(nameof(Index), new { providerId });
            }
        }

        /// <summary>
        /// GET: /HospitalQueue/GetPendingCount
        /// Endpoint to poll for new pending emergency requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPendingCount(int? providerId = null)
        {
            try
            {
                var result = await _hospitalStaffService.GetQueueAsync(providerId);
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
        public async Task<IActionResult> ExportReport(int? providerId = null)
        {
            try
            {
                var result = await _hospitalStaffService.GetQueueAsync(providerId);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = "فشل في إنشاء التقرير";
                    return RedirectToAction(nameof(Index), new { providerId });
                }

                var queue = result.Data;
                var csv = new System.Text.StringBuilder();
                
                // Write Header Metadata
                csv.AppendLine($"تقرير الطوارئ والفرز اليومي - {queue.ProviderName}");
                csv.AppendLine($"تاريخ الاستخراج,{DateTime.Now:g}");
                csv.AppendLine($"الأسرة الشاغرة,{queue.AvailableBeds}");
                csv.AppendLine($"الحالات المعلقة,{queue.PendingCount}");
                csv.AppendLine($"الحالات المقبولة,{queue.AcceptedCount}");
                csv.AppendLine($"الحالات المتدهورة,{queue.EscalatedCount}");
                csv.AppendLine();

                // Write Cases Table Headers
                csv.AppendLine("معرف الطلب,اسم المريض,رقم الهاتف,نوع الطوارئ,درجة الأولوية,الحالة,زمن الانتظار (دقيقة),تاريخ الطلب");

                // Write Cases Table Rows
                foreach (var item in queue.Items)
                {
                    var statusText = item.Status == Etmen_Domain.Enums.EmergencyRequestStatus.Pending ? "معلق" :
                                     item.Status == Etmen_Domain.Enums.EmergencyRequestStatus.Accepted ? "مقبول" :
                                     item.Status == Etmen_Domain.Enums.EmergencyRequestStatus.Escalated ? "متدهور!" : item.Status.ToString();
                    
                    csv.AppendLine($"{item.RequestId},{item.PatientName},{item.PatientPhone},{item.EmergencyType},{item.PriorityScore}%,{statusText},{item.WaitingMinutes},{item.RequestedAt:g}");
                }

                // File download with UTF-8 byte order mark (BOM) to support Arabic characters in Excel
                var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
                var fileName = $"EmergencyReport_{queue.ProviderId ?? 0}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(bytes, "text/csv; charset=utf-8", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting emergency CSV report");
                TempData["Error"] = "حدث خطأ أثناء تصدير التقرير";
                return RedirectToAction(nameof(Index), new { providerId });
            }
        }
    }
}
