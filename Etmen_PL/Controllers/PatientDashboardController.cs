using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Patient Dashboard Controller
    /// Displays patient landing dashboard metrics
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientDashboardController> _logger;

        public PatientDashboardController(
            IPatientService patientService,
            ILogger<PatientDashboardController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /PatientDashboard/Index
        /// Renders patient home dashboard panels
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt to PatientDashboard");
                    return RedirectToAction("Login", "Account");
                }

                var result = await _patientService.GetDashboardAsync(userId);
                if (!result.IsSuccess || result.Data == null)
                {
                    _logger.LogWarning("Failed to retrieve dashboard for user {UserId}: {Message}", userId, result.ErrorMessage);
                    TempData["Error"] = result.ErrorMessage ?? "حدث خطأ أثناء تحميل لوحة التحكم";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("PatientDashboard Index accessed for user {UserId}", userId);
                
                var viewModel = new PatientDashboardViewModel
                {
                    PatientName = result.Data.PatientName,
                    LatestRiskAssessment = result.Data.LatestRiskAssessment,
                    UnreadAlertsCount = result.Data.UnreadAlertsCount,
                    UpcomingAppointmentsCount = result.Data.UpcomingAppointmentsCount,
                    LatestBmi = result.Data.LatestBmi,
                    LatestBmiCategory = result.Data.LatestBmiCategory,
                    UpcomingAppointments = result.Data.UpcomingAppointments,
                    RecentAlerts = result.Data.RecentAlerts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient dashboard");
                TempData["Error"] = "حدث خطأ أثناء تحميل لوحة التحكم";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
