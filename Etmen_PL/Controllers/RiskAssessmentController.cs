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
    /// Processes self-assessments, calculates risk level, and triggers email alerts via RiskService
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class RiskAssessmentController : Controller
    {
        private readonly IRiskService _riskService;
        private readonly IPatientService _patientService;
        private readonly IPdfReportService _pdfReportService;
        private readonly ICriticalIntelligenceService _criticalIntelligenceService;
        private readonly ILogger<RiskAssessmentController> _logger;

        public RiskAssessmentController(
            IRiskService riskService,
            IPatientService patientService,
            IPdfReportService pdfReportService,
            ICriticalIntelligenceService criticalIntelligenceService,
            ILogger<RiskAssessmentController> logger)
        {
            _riskService    = riskService;
            _patientService = patientService;
            _pdfReportService = pdfReportService;
            _criticalIntelligenceService = criticalIntelligenceService;
            _logger         = logger;
        }

        /// <summary>GET: /RiskAssessment/Index — Renders assessment inputs questionnaire</summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View(new RiskAssessmentInputViewModel());
        }

        /// <summary>
        /// POST: /RiskAssessment/Index
        /// Computes risk, stores result, sends email alert if High/Emergency (via RiskService)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RiskAssessmentInputViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                // Get patient profile
                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    ModelState.AddModelError(string.Empty, "لم يتم العثور على ملفك الطبي.");
                    return View(viewModel);
                }

                var dto = new RiskInputDto
                {
                    Symptoms        = viewModel.Symptoms,
                    HeartRate       = viewModel.HeartRate,
                    SystolicBP      = viewModel.SystolicBP,
                    DiastolicBP     = viewModel.DiastolicBP,
                    Temperature     = viewModel.Temperature,
                    OxygenSaturation= viewModel.OxygenSaturation,
                    BloodSugar      = viewModel.BloodSugar,
                    Latitude        = viewModel.Latitude,
                    Longitude       = viewModel.Longitude,
                };

                // CalculateRiskAsync returns the result for display
                var calcResult = await _riskService.CalculateRiskAsync(dto);
                if (!calcResult.IsSuccess || calcResult.Data == null)
                {
                    ModelState.AddModelError(string.Empty, calcResult.ErrorMessage ?? "فشل تقييم المخاطر.");
                    return View(viewModel);
                }

                var saveResult = await _riskService.SaveRiskAssessmentAsync(profileResult.Data.Id, calcResult.Data);
                if (!saveResult.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, saveResult.ErrorMessage ?? "Failed to save risk assessment.");
                    return View(viewModel);
                }

                // Pass result to Result view via TempData
                TempData["RiskResult"] = JsonSerializer.Serialize(calcResult.Data);

                _logger.LogInformation("Risk assessment submitted by user {UserId}, level: {Level}",
                    userId, calcResult.Data.RiskLevel);

                return RedirectToAction(nameof(Result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assessing risk");
                ModelState.AddModelError(string.Empty, "خطأ في تقييم المخاطر. يُرجى المحاولة لاحقاً.");
                return View(viewModel);
            }
        }

        /// <summary>GET: /RiskAssessment/Result — Renders calculated risk category and recommendations</summary>
        [HttpGet]
        public IActionResult Result()
        {
            try
            {
                var resultJson = TempData["RiskResult"]?.ToString();
                if (string.IsNullOrWhiteSpace(resultJson))
                    return RedirectToAction(nameof(Index));

                var result = JsonSerializer.Deserialize<RiskResultDto>(resultJson);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering risk result");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>GET: /RiskAssessment/History — Lists previous risk assessments</summary>
        [HttpGet]
        public async Task<IActionResult> History()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = "لم يتم العثور على ملفك الطبي.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                var history = await _riskService.GetPatientRiskHistoryAsync(profileResult.Data.Id);
                return View(history.IsSuccess ? history.Data : new List<RiskResultDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving risk history");
                TempData["Error"] = "خطأ في تحميل سجل المخاطر";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// GET: /RiskAssessment/DownloadPdf/{id}
        /// Downloads a risk assessment report as PDF
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid assessment ID");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = "لم يتم العثور على ملفك الطبي.";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                // Verify ownership by checking history
                var historyResult = await _riskService.GetPatientRiskHistoryAsync(profileResult.Data.Id);
                if (!historyResult.IsSuccess || historyResult.Data == null)
                {
                    TempData["Error"] = "فشل التحقق من صلاحية الوصول لتقرير المخاطر.";
                    return RedirectToAction(nameof(History));
                }

                var assessmentDto = historyResult.Data.FirstOrDefault(a => a.Id == id);
                if (assessmentDto == null)
                {
                    _logger.LogWarning("User {UserId} unauthorized download attempt of risk assessment {AssessmentId}", userId, id);
                    return Forbid();
                }

                // Generate PDF
                var pdfBytes = await _pdfReportService.GenerateRiskReportPdfAsync(
                    profileResult.Data.FullName ?? "المريض",
                    assessmentDto.RiskLevel.ToString(),
                    assessmentDto.RiskScore,
                    assessmentDto.Recommendations,
                    assessmentDto.TriggeredSymptoms,
                    assessmentDto.AssessmentDate,
                    assessmentDto.IsEmergency
                );

                var fileName = $"Risk_Report_{assessmentDto.AssessmentDate:yyyyMMdd}_{id}.pdf";
                _logger.LogInformation("Risk assessment PDF download triggered for assessment {AssessmentId} by user {UserId}", id, userId);
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating risk assessment PDF for ID {AssessmentId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل ملف تقرير التقييم";
                return RedirectToAction(nameof(History));
            }
        }

        /// <summary>
        /// GET: /RiskAssessment/ExplainRisk/{id}
        /// Returns AI plain language explanation and metrics for a risk assessment
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExplainRisk(int id)
        {
            try
            {
                if (id <= 0)
                    return Json(new { success = false, message = "Invalid ID" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Unauthorized" });

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                    return Json(new { success = false, message = "Profile not found" });

                // Verify ownership by checking history
                var historyResult = await _riskService.GetPatientRiskHistoryAsync(profileResult.Data.Id);
                if (!historyResult.IsSuccess || historyResult.Data == null || !historyResult.Data.Any(a => a.Id == id))
                {
                    _logger.LogWarning("User {UserId} unauthorized AI explanation attempt of risk assessment {AssessmentId}", userId, id);
                    return Json(new { success = false, message = "Access denied" });
                }

                var explainResult = await _criticalIntelligenceService.ExplainRiskAssessmentAsync(id);
                if (explainResult.IsSuccess && explainResult.Data != null)
                {
                    _logger.LogInformation("AI Risk explanation fetched successfully for assessment {AssessmentId} by user {UserId}", id, userId);
                    return Json(new {
                        success = true,
                        summary = explainResult.Data.PlainLanguageSummary,
                        contributions = explainResult.Data.Contributions.Select(c => new {
                            factor = c.Factor,
                            weight = c.ImpactPercent / 100.0,
                            description = c.Explanation
                        }),
                        actions = explainResult.Data.ImmediateActions
                    });
                }

                return Json(new { success = false, message = explainResult.ErrorMessage ?? "فشل توليد الشرح بالذكاء الاصطناعي" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI risk explanation for assessment ID {AssessmentId}", id);
                return Json(new { success = false, message = "حدث خطأ أثناء الاتصال بالمساعد الذكي" });
            }
        }
    }
}
