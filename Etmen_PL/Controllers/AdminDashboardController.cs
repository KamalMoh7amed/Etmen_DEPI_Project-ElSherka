using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Admin Dashboard Controller
    /// System overview telemetry dashboard
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            IAdminService adminService,
            ILogger<AdminDashboardController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /AdminDashboard/Index
        /// Shows active users, appointments, and crisis status
        /// TODO: Call _adminService.GetDashboardAsync()
        /// TODO: Return View(viewModel)
        /// </summary>
        [HttpGet]
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
    }
}
