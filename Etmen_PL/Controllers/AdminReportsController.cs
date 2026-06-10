using System.Text;
using Etmen_BLL.DTOs.Admin;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Admin Reports Controller
    /// Generates system, epidemiological, and operational reports
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminReportsController> _logger;

        public AdminReportsController(
            IAdminService adminService,
            ILogger<AdminReportsController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /AdminReports/Index
        /// Shows available report templates (system, epidemiology, operational)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _adminService.GetReportsAsync();
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error loading reports";
                    return RedirectToAction("Index", "AdminDashboard");
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports");
                TempData["Error"] = "خطأ في تحميل التقارير";
                return RedirectToAction("Index", "AdminDashboard");
            }
        }

        /// <summary>
        /// GET: /AdminReports/SystemReport
        /// Generates user count, appointment, and crisis summary report
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SystemReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var reports = await GetReportsForRangeAsync("System", startDate, endDate);
                return View("Report", reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating system report");
                TempData["Error"] = "خطأ في إنشاء التقرير";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /AdminReports/EpidemiologyReport
        /// Generates disease frequency and patient outcome report
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EpidemiologyReport(int? crisisId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var reports = await GetReportsForRangeAsync("Crisis", startDate, endDate);
                if (crisisId.HasValue)
                    ViewData["CrisisId"] = crisisId.Value;

                return View("Report", reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating epidemiology report");
                TempData["Error"] = "خطأ في إنشاء التقرير";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /AdminReports/OperationalReport
        /// Shows dispatch times and hospital utilization metrics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OperationalReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var reports = await GetReportsForRangeAsync("Emergencies", startDate, endDate);
                return View("Report", reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating operational report");
                TempData["Error"] = "خطأ في إنشاء التقرير";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminReports/Export
        /// Exports report data to CSV or Excel
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Export(string reportType, DateTime? startDate, DateTime? endDate)
        {
            if (string.IsNullOrWhiteSpace(reportType))
            {
                TempData["Error"] = "يجب تحديد نوع التقرير";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var reports = await GetReportsForRangeAsync(reportType, startDate, endDate);
                var csv = BuildReportsCsv(reports);
                var fileName = $"{reportType.ToLowerInvariant()}-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

                _logger.LogInformation("Report {ReportType} exported", reportType);
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                TempData["Error"] = "خطأ في تصدير التقرير";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminReports/Schedule
        /// Updates report-related system configuration
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Schedule(SystemConfigViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index));

            try
            {
                var dto = new SystemConfigDto
                {
                    EnableCrisisMode = viewModel.EnableCrisisMode,
                    EnableAIChat = viewModel.EnableAIChat,
                    EnableOCR = viewModel.EnableOCR,
                    EnableFamilyLinking = viewModel.EnableFamilyLinking,
                    EnableEmergencyRequests = viewModel.EnableEmergencyRequests,
                    MaxLoginAttempts = viewModel.MaxLoginAttempts,
                    LockoutDurationMinutes = viewModel.LockoutDurationMinutes,
                    SessionTimeoutMinutes = 30
                };

                var result = await _adminService.UpdateSystemConfigAsync(dto);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error scheduling report";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Report schedule configuration updated");
                TempData["Success"] = "تم جدولة التقرير بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling report");
                TempData["Error"] = "خطأ في جدولة التقرير";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<List<AdminReportDto>> GetReportsForRangeAsync(string reportType, DateTime? startDate, DateTime? endDate)
        {
            var result = await _adminService.GetReportsAsync(1, 100);
            if (!result.IsSuccess || result.Data is null)
                return new List<AdminReportDto>();

            var reports = result.Data.Items.AsEnumerable();
            if (!reportType.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                reports = reports.Where(r => r.ReportType.Equals(reportType, StringComparison.OrdinalIgnoreCase));
            }

            if (startDate.HasValue)
                reports = reports.Where(r => r.EndDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                reports = reports.Where(r => r.StartDate.Date <= endDate.Value.Date);

            return reports.ToList();
        }

        private static string BuildReportsCsv(IEnumerable<AdminReportDto> reports)
        {
            var builder = new StringBuilder();
            builder.AppendLine("ReportType,StartDate,EndDate,TotalRecords,FileUrl,GeneratedAt");

            foreach (var report in reports)
            {
                builder.AppendLine(string.Join(',',
                    EscapeCsv(report.ReportType),
                    report.StartDate.ToString("O"),
                    report.EndDate.ToString("O"),
                    report.TotalRecords,
                    EscapeCsv(report.FileUrl),
                    report.GeneratedAt.ToString("O")));
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
                return value;

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
