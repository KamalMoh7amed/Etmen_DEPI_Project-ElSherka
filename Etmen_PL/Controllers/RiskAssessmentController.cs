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
        private readonly ICrisisService _crisisService;
        private readonly ILogger<RiskAssessmentController> _logger;

        public RiskAssessmentController(
            IRiskService riskService,
            IPatientService patientService,
            IPdfReportService pdfReportService,
            ICriticalIntelligenceService criticalIntelligenceService,
            ICrisisService crisisService,
            ILogger<RiskAssessmentController> logger)
        {
            _riskService    = riskService;
            _patientService = patientService;
            _pdfReportService = pdfReportService;
            _criticalIntelligenceService = criticalIntelligenceService;
            _crisisService = crisisService;
            _logger         = logger;
        }

        /// <summary>
        /// GET: /RiskAssessment/Index — Renders assessment inputs questionnaire.
        /// Accepts optional query params from the dashboard "آخر قياس طبي" to pre-fill vitals.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(decimal? hr, decimal? sbp, decimal? dbp, decimal? temp, decimal? spo2, decimal? bs)
        {
            var activeCrisisResult = await _crisisService.GetActiveCrisisAsync();
            if (activeCrisisResult.IsSuccess && activeCrisisResult.Data != null)
            {
                ViewBag.ActiveCrisis = activeCrisisResult.Data;
            }

            var model = new RiskAssessmentInputViewModel
            {
                HeartRate = hr,
                SystolicBP = sbp,
                DiastolicBP = dbp,
                Temperature = temp,
                OxygenSaturation = spo2,
                BloodSugar = bs
            };
            return View(model);
        }

        /// <summary>
        /// POST: /RiskAssessment/Index
        /// Computes risk, stores result, sends email alert if High/Emergency (via RiskService)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RiskAssessmentInputViewModel viewModel)
        {
            var activeCrisisResult = await _crisisService.GetActiveCrisisAsync();
            var isCrisisMode = activeCrisisResult.IsSuccess && activeCrisisResult.Data != null && activeCrisisResult.Data.IsActive && activeCrisisResult.Data.SystemMode == Etmen_Domain.Enums.SystemMode.Crisis;
            
            if (activeCrisisResult.IsSuccess && activeCrisisResult.Data != null)
            {
                ViewBag.ActiveCrisis = activeCrisisResult.Data;
            }

            if (!isCrisisMode)
            {
                if (!viewModel.HeartRate.HasValue) ModelState.AddModelError(nameof(viewModel.HeartRate), "يرجى إدخال معدل نبضات القلب");
                if (!viewModel.SystolicBP.HasValue) ModelState.AddModelError(nameof(viewModel.SystolicBP), "يرجى إدخال ضغط الدم الانقباضي");
                if (!viewModel.DiastolicBP.HasValue) ModelState.AddModelError(nameof(viewModel.DiastolicBP), "يرجى إدخال ضغط الدم الانبساطي");
                if (!viewModel.Temperature.HasValue) ModelState.AddModelError(nameof(viewModel.Temperature), "يرجى إدخال درجة حرارة الجسم");
                if (!viewModel.OxygenSaturation.HasValue) ModelState.AddModelError(nameof(viewModel.OxygenSaturation), "يرجى إدخال نسبة تشبّع الأكسجين");
                if (!viewModel.BloodSugar.HasValue) ModelState.AddModelError(nameof(viewModel.BloodSugar), "يرجى إدخال مستوى السكر في الدم");
            }

            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError(string.Empty, "يجب تسجيل الدخول أولاً. يرجى إعادة تسجيل الدخول.");
                    return View(viewModel);
                }

                // Get patient profile
                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    ModelState.AddModelError(string.Empty,
                        "لم يتم العثور على ملفك الطبي. يرجى التأكد من إنشاء ملف طبي أولاً من خلال صفحة الملف الشخصي.");
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
                    ModelState.AddModelError(string.Empty, calcResult.ErrorMessage ?? "فشل تقييم المخاطر. يرجى التحقق من البيانات المدخلة.");
                    return View(viewModel);
                }

                var saveResult = await _riskService.SaveRiskAssessmentAsync(profileResult.Data.Id, calcResult.Data);
                if (!saveResult.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty,
                        $"تم حساب التقييم بنجاح لكن فشل حفظه في النظام: {saveResult.ErrorMessage ?? "خطأ غير معروف"}. يرجى المحاولة مرة أخرى.");
                    return View(viewModel);
                }

                // Pass result to Result view via TempData
                TempData["RiskResult"] = JsonSerializer.Serialize(calcResult.Data);

                _logger.LogInformation("Risk assessment submitted by user {UserId}, level: {Level}",
                    userId, calcResult.Data.RiskLevel);

                return RedirectToAction(nameof(Result));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during risk assessment for user");
                ModelState.AddModelError(string.Empty, $"خطأ في البيانات: {ex.Message}");
                return View(viewModel);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while saving risk assessment");
                ModelState.AddModelError(string.Empty,
                    "حدث خطأ في حفظ البيانات في قاعدة البيانات. يرجى المحاولة مرة أخرى أو التواصل مع الدعم الفني.");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error assessing risk");
                ModelState.AddModelError(string.Empty,
                    $"حدث خطأ غير متوقع أثناء تقييم المخاطر. يرجى المحاولة مرة أخرى. إذا استمر الخطأ، يرجى التواصل مع الدعم الفني. الخطأ: {ex.Message}");
                return View(viewModel);
            }
        }

        /// <summary>GET: /RiskAssessment/Result — Renders calculated risk category and recommendations</summary>
        [HttpGet]
        public async Task<IActionResult> Result(int? id)
        {
            try
            {
                RiskResultDto? result = null;
                if (id.HasValue)
                {
                    var historyResult = await _riskService.GetRiskAssessmentByIdAsync(id.Value);
                    if (historyResult.IsSuccess && historyResult.Data != null)
                    {
                        result = historyResult.Data;
                    }
                }

                if (result == null)
                {
                    var resultJson = TempData["RiskResult"]?.ToString();
                    if (string.IsNullOrWhiteSpace(resultJson))
                        return RedirectToAction(nameof(Index));

                    result = JsonSerializer.Deserialize<RiskResultDto>(resultJson);
                }

                if (result == null)
                {
                    return RedirectToAction(nameof(Index));
                }

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
