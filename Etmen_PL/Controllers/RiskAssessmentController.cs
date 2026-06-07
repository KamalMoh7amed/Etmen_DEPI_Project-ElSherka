using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Risk Assessment Controller
    /// Processes self-assessments and displays recommendations.
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class RiskAssessmentController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<RiskAssessmentController> _logger;

        public RiskAssessmentController(
            IPatientService patientService,
            ILogger<RiskAssessmentController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /RiskAssessment/Index
        /// Renders assessment inputs questionnaire.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View(new RiskAssessmentInputViewModel());
        }

        /// <summary>
        /// POST: /RiskAssessment/Index
        /// Computes risk and schedules triage when escalation is needed.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RiskAssessmentInputViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return RedirectToAction("Login", "Account");

                var dto = new RiskInputDto
                {
                    Symptoms = viewModel.Symptoms,
                    HeartRate = viewModel.HeartRate,
                    SystolicBP = viewModel.SystolicBP,
                    DiastolicBP = viewModel.DiastolicBP,
                    Temperature = viewModel.Temperature,
                    OxygenSaturation = viewModel.OxygenSaturation,
                    BloodSugar = viewModel.BloodSugar,
                    Latitude = viewModel.Latitude,
                    Longitude = viewModel.Longitude
                };

                var result = await _patientService.AssessRiskAsync(userId, dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    _logger.LogWarning("Failed to assess risk for user {UserId}: {Message}", userId, result.ErrorMessage);
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "تعذر تقييم مستوى المخاطر.");
                    return View(viewModel);
                }

                _logger.LogInformation("Risk assessment submitted for user {UserId}", userId);
                TempData["RiskResult"] = JsonSerializer.Serialize(result.Data);
                return RedirectToAction(nameof(Result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assessing risk");
                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تقييم المخاطر.");
                return View(viewModel);
            }
        }

        /// <summary>
        /// GET: /RiskAssessment/Result
        /// Renders calculated risk category and recommendations.
        /// </summary>
        [HttpGet]
        public IActionResult Result()
        {
            try
            {
                var resultJson = TempData["RiskResult"]?.ToString();
                if (string.IsNullOrWhiteSpace(resultJson))
                    return RedirectToAction(nameof(Index));

                var result = JsonSerializer.Deserialize<RiskResultDto>(resultJson);
                if (result == null)
                    return RedirectToAction(nameof(Index));

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering risk result");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /RiskAssessment/History
        /// Lists previous risk scores.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> History()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return RedirectToAction("Login", "Account");

                var result = await _patientService.GetRiskHistoryAsync(userId);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve risk history for user {UserId}: {Message}", userId, result.ErrorMessage);
                    TempData["Error"] = result.ErrorMessage ?? "تعذر تحميل سجل تقييمات المخاطر.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                return View(result.Data ?? Enumerable.Empty<RiskResultDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving risk history");
                TempData["Error"] = "حدث خطأ أثناء تحميل سجل المخاطر.";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }
    }
}
