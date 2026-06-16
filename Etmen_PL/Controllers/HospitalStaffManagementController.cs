using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Etmen_PL.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    public class HospitalStaffManagementController : Controller
    {
        private readonly IHospitalStaffService _hospitalStaffService;
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDoctorService _doctorService;
        private readonly ILogger<HospitalStaffManagementController> _logger;

        public HospitalStaffManagementController(
            IHospitalStaffService hospitalStaffService,
            IUnitOfWork uow,
            UserManager<ApplicationUser> userManager,
            IDoctorService doctorService,
            ILogger<HospitalStaffManagementController> logger)
        {
            _hospitalStaffService = hospitalStaffService;
            _uow = uow;
            _userManager = userManager;
            _doctorService = doctorService;
            _logger = logger;
        }

        private async Task<bool> IsAuthorizedForProviderAsync(int providerId)
        {
            if (User.IsInRole("Admin")) return true;

            if (User.IsInRole("Doctor"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return false;

                var doctorResult = await _doctorService.GetProfileAsync(userId);
                if (doctorResult.IsSuccess && doctorResult.Data != null)
                {
                    var doctor = doctorResult.Data;
                    if (!string.IsNullOrEmpty(doctor.OnboardingDataJson))
                    {
                        var data = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(doctor.OnboardingDataJson);
                        if (data != null && data.TryGetValue("HealthcareProviderId", out var hpIdVal) && int.TryParse(hpIdVal.ToString(), out var hpId))
                        {
                            return hpId == providerId;
                        }
                    }
                }
            }
            return false;
        }

        private async Task<bool> IsAuthorizedForProfileAsync(int profileId)
        {
            if (User.IsInRole("Admin")) return true;

            var profile = await _uow.StaffProfiles.GetByIdAsync(profileId);
            if (profile == null) return false;

            return await IsAuthorizedForProviderAsync(profile.HealthcareProviderId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(int providerId, string email, StaffRoleType role, StaffShiftType shift, string redirectUrl)
        {
            if (!await IsAuthorizedForProviderAsync(providerId))
            {
                TempData["Error"] = "غير مصرح لك بإدارة موظفي هذا المركز الصحي.";
                return LocalRedirect(redirectUrl);
            }

            try
            {
                var senderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var inviteUrlPrefix = $"{Request.Scheme}://{Request.Host}/Account/RegisterStaff";

                var result = await _hospitalStaffService.InviteStaffAsync(providerId, email.Trim(), role, shift, senderUserId, inviteUrlPrefix);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "حدث خطأ أثناء إرسال الدعوة.";
                }
                else
                {
                    TempData["Success"] = "تم إرسال دعوة الانضمام للموظف بنجاح وجاري إشعاره بالبريد.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting staff to provider {ProviderId}", providerId);
                TempData["Error"] = "خطأ غير متوقع أثناء إرسال الدعوة.";
            }

            return LocalRedirect(redirectUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resend(int profileId, string redirectUrl)
        {
            if (!await IsAuthorizedForProfileAsync(profileId))
            {
                TempData["Error"] = "غير مصرح لك بإدارة هذا الموظف.";
                return LocalRedirect(redirectUrl);
            }

            try
            {
                var inviteUrlPrefix = $"{Request.Scheme}://{Request.Host}/Account/RegisterStaff";
                var result = await _hospitalStaffService.ResendInvitationAsync(profileId, inviteUrlPrefix);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل إعادة إرسال الدعوة.";
                }
                else
                {
                    TempData["Success"] = "تم إعادة إرسال بريد الدعوة بنجاح.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending invitation for profile {ProfileId}", profileId);
                TempData["Error"] = "خطأ غير متوقع أثناء إعادة إرسال الدعوة.";
            }

            return LocalRedirect(redirectUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int profileId, string redirectUrl)
        {
            if (!await IsAuthorizedForProfileAsync(profileId))
            {
                TempData["Error"] = "غير مصرح لك بإدارة هذا الموظف.";
                return LocalRedirect(redirectUrl);
            }

            try
            {
                var result = await _hospitalStaffService.CancelInvitationAsync(profileId);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل إلغاء الدعوة.";
                }
                else
                {
                    TempData["Success"] = "تم إلغاء الدعوة بنجاح.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling invitation for profile {ProfileId}", profileId);
                TempData["Error"] = "خطأ غير متوقع أثناء إلغاء الدعوة.";
            }

            return LocalRedirect(redirectUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStaff(int profileId, StaffRoleType role, StaffShiftType shift, string redirectUrl)
        {
            if (!await IsAuthorizedForProfileAsync(profileId))
            {
                TempData["Error"] = "غير مصرح لك بإدارة هذا الموظف.";
                return LocalRedirect(redirectUrl);
            }

            try
            {
                var result = await _hospitalStaffService.UpdateStaffAsync(profileId, role, shift);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل تعديل بيانات الموظف.";
                }
                else
                {
                    TempData["Success"] = "تم تحديث دور الموظف وورديته بنجاح.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff profile {ProfileId}", profileId);
                TempData["Error"] = "خطأ غير متوقع أثناء تعديل بيانات الموظف.";
            }

            return LocalRedirect(redirectUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int profileId, string redirectUrl)
        {
            if (!await IsAuthorizedForProfileAsync(profileId))
            {
                TempData["Error"] = "غير مصرح لك بإدارة هذا الموظف.";
                return LocalRedirect(redirectUrl);
            }

            try
            {
                var result = await _hospitalStaffService.RemoveStaffAsync(profileId);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل حذف الموظف.";
                }
                else
                {
                    TempData["Success"] = "تم إخراج الموظف وفك ارتباطه بنجاح.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing staff profile {ProfileId}", profileId);
                TempData["Error"] = "خطأ غير متوقع أثناء إزالة الموظف.";
            }

            return LocalRedirect(redirectUrl);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateInviteLink(int providerId, StaffRoleType role, StaffShiftType shift)
        {
            if (!await IsAuthorizedForProviderAsync(providerId))
            {
                return Json(new { success = false, message = "غير مصرح لك بإدارة هذا المركز الصحي." });
            }

            try
            {
                var senderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var inviteUrlPrefix = $"{Request.Scheme}://{Request.Host}/Account/RegisterStaff";

                var result = await _hospitalStaffService.GenerateInviteLinkAsync(providerId, role, shift, senderUserId, inviteUrlPrefix);
                if (result.IsSuccess)
                {
                    return Json(new { success = true, link = result.Data });
                }
                return Json(new { success = false, message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invite link for provider {ProviderId}", providerId);
                return Json(new { success = false, message = "خطأ غير متوقع أثناء توليد الرابط." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Stats(int providerId)
        {
            if (!await IsAuthorizedForProviderAsync(providerId))
            {
                TempData["Error"] = "غير مصرح لك بطلب إحصائيات هذا المركز الصحي.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var result = await _hospitalStaffService.GetStatsAsync(providerId);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل في تحميل التقارير.";
                    return RedirectToAction("Index", "Home");
                }
                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for provider {ProviderId}", providerId);
                TempData["Error"] = "حدث خطأ غير متوقع أثناء تحميل التقارير.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logs(int providerId)
        {
            if (!await IsAuthorizedForProviderAsync(providerId))
            {
                TempData["Error"] = "غير مصرح لك باستعراض سجل أنشطة هذا المركز الصحي.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var result = await _hospitalStaffService.GetLogsAsync(providerId);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل في تحميل سجل العمليات.";
                    return RedirectToAction("Index", "Home");
                }
                ViewBag.ProviderId = providerId;
                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs for provider {ProviderId}", providerId);
                TempData["Error"] = "حدث خطأ غير متوقع أثناء تحميل سجل العمليات.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
