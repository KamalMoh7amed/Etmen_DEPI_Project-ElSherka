using Etmen_BLL.Repositories.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Alerts Controller for Patient
    /// Handles viewing, marking read/unread, and deletion of health alerts
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class AlertsController : Controller
    {
        private readonly IAlertService _alertService;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(
            IAlertService alertService,
            ILogger<AlertsController> logger)
        {
            _alertService = alertService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /Alerts
        /// Lists all alerts for the logged-in patient
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var result = await _alertService.GetUserAlertsAsync(userId);
                var alerts = result.IsSuccess ? result.Data : new List<Etmen_BLL.DTOs.Alert.AlertDto>();

                _logger.LogInformation("Patient alerts listing accessed by user {UserId}", userId);
                return View(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patient alerts page");
                TempData["Error"] = "حدث خطأ أثناء تحميل التنبيهات الصحية";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// POST: /Alerts/MarkAsRead
        /// Marks a single alert as read (suitable for AJAX requests)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid alert ID");

                var result = await _alertService.MarkAsReadAsync(id);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Alert {AlertId} marked as read", id);
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = result.ErrorMessage ?? "فشل تحديث حالة التنبيه" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking alert {AlertId} as read", id);
                return Json(new { success = false, message = "حدث خطأ في الخادم" });
            }
        }

        /// <summary>
        /// POST: /Alerts/MarkAllAsRead
        /// Marks all alerts as read for the current patient
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _alertService.MarkAllAsReadAsync(userId);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("All alerts marked as read for user {UserId}", userId);
                    TempData["Success"] = "تم تحديد جميع التنبيهات كمقروءة";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = result.ErrorMessage ?? "فشل تحديث التنبيهات";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all alerts as read");
                TempData["Error"] = "حدث خطأ أثناء معالجة الطلب";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Alerts/Delete
        /// Deletes an alert
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid alert ID");

                var result = await _alertService.DeleteAlertAsync(id);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Alert {AlertId} deleted", id);
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = result.ErrorMessage ?? "فشل حذف التنبيه" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert {AlertId}", id);
                return Json(new { success = false, message = "حدث خطأ في الخادم" });
            }
        }

        /// <summary>
        /// GET: /Alerts/GetUnreadCount
        /// Returns JSON with number of unread alerts for Layout polling
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Json(new { count = 0 });

                var result = await _alertService.GetUnreadCountAsync(userId);
                return Json(new { count = result.IsSuccess ? result.Data : 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unread alert count");
                return Json(new { count = 0 });
            }
        }
    }
}
