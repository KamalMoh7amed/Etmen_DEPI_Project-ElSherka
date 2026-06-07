using Etmen_BLL.Repositories.IServices;
using Etmen_BLL.DTOs.Admin;
using Etmen_PL.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Admin Users Controller
    /// Manages user profiles and permissions
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            IAdminService adminService,
            ILogger<AdminUsersController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /AdminUsers/Index
        /// Lists system users with status toggles
        /// TODO: Parse pageNumber from query parameter (default 1)
        /// TODO: Call _adminService.GetAllUsersAsync(pageNumber)
        /// TODO: Return View with PaginatedResult<UserListItemDto>
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            try
            {
                pageNumber = Math.Max(pageNumber, 1);

                var result = await _adminService.GetAllUsersAsync(pageNumber);
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error loading users";
                    return RedirectToAction("Index", "AdminDashboard");
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                TempData["Error"] = "خطأ في تحميل المستخدمين";
                return RedirectToAction("Index", "AdminDashboard");
            }
        }

        /// <summary>
        /// POST: /AdminUsers/UpdateStatus
        /// Activates or deactivates a user account
        /// TODO: Validate ModelState
        /// TODO: Call _adminService.UpdateUserStatusAsync(userId, dto)
        /// TODO: Redirect to Index on success
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string userId, bool isActive, string? reason)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "Invalid user id";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var dto = new UpdateUserStatusDto
                {
                    UserId = userId,
                    IsActive = isActive,
                    Reason = reason
                };

                var numericUserId = int.TryParse(userId, out var parsedUserId) ? parsedUserId : 0;
                var result = await _adminService.UpdateUserStatusAsync(numericUserId, dto);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error updating user status";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("User {UserId} status updated", userId);
                TempData["Success"] = "تم تحديث حالة المستخدم بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status");
                TempData["Error"] = "خطأ في تحديث حالة المستخدم";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminUsers/BulkAction
        /// Applies actions on multiple users
        /// TODO: Validate ModelState
        /// TODO: Call _adminService.BulkUserActionAsync(dto)
        /// TODO: Redirect to Index on success
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string[] userIds, string action)
        {
            if (userIds == null || userIds.Length == 0)
            {
                TempData["Error"] = "يجب اختيار مستخدمين على الأقل";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var dto = new BulkUserActionDto
                {
                    UserIds = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList(),
                    Action = action
                };

                if (dto.UserIds.Count == 0)
                {
                    TempData["Error"] = "Invalid user ids";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _adminService.BulkUserActionAsync(dto);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error performing bulk action";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Bulk user action {Action} performed for {Count} users", action, userIds.Length);
                TempData["Success"] = "تم تنفيذ الإجراء بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action");
                TempData["Error"] = "خطأ في تنفيذ الإجراء";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminUsers/Delete
        /// Permanently deletes a user from the system
        /// TODO: Validate userId parameter
        /// TODO: Call _adminService.DeleteUserAsync(userId)
        /// TODO: Redirect to Index on success
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    TempData["Error"] = "Invalid user id";
                    return RedirectToAction(nameof(Index));
                }

                var result = int.TryParse(userId, out var numericUserId)
                    ? await _adminService.DeleteUserAsync(numericUserId)
                    : await _adminService.BulkUserActionAsync(new BulkUserActionDto
                    {
                        UserIds = new List<string> { userId },
                        Action = "delete"
                    });

                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error deleting user";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("User {UserId} deleted", userId);
                TempData["Success"] = "تم حذف المستخدم بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                TempData["Error"] = "خطأ في حذف المستخدم";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
