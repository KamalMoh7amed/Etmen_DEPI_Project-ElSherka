using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Lab Results Controller
    /// Renders lab result reports and handles OCR document uploads
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class LabResultsController : Controller
    {
        private readonly ILabService _labService;
        private readonly IPatientService _patientService;
        private readonly IPdfReportService _pdfReportService;
        private readonly ILogger<LabResultsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LabResultsController(
            ILabService labService,
            IPatientService patientService,
            IPdfReportService pdfReportService,
            ILogger<LabResultsController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _labService = labService;
            _patientService = patientService;
            _pdfReportService = pdfReportService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// GET: /LabResults/Index
        /// Displays timeline of lab uploads
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = "فشل تحميل ملف المريض";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                var labResult = await _labService.GetPatientLabResultsAsync(profileResult.Data.Id);
                var results = labResult.IsSuccess ? labResult.Data : new List<Etmen_BLL.DTOs.Lab.LabResultDto>();

                ViewBag.LabUpload = new LabUploadViewModel { PatientId = profileResult.Data.Id };
                return View(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab results");
                TempData["Error"] = "خطأ في تحميل نتائج الاختبارات";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// POST: /LabResults/Upload
        /// Submits a PDF/image lab report with OCR processing flag
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(LabUploadViewModel viewModel)
        {
            if (!ModelState.IsValid || viewModel.LabFile == null)
            {
                TempData["Error"] = "الملف والبيانات المطلوبة لرفع النتيجة غير صالحة";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                // Validate file type (PDF, JPG, PNG only)
                var extension = Path.GetExtension(viewModel.LabFile.FileName).ToLower();
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "نوع الملف غير مدعوم. المسموح به هو PDF و JPG و PNG فقط";
                    return RedirectToAction(nameof(Index));
                }

                // Validate file size (max 10MB)
                if (viewModel.LabFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "حجم الملف كبير جداً. الحد الأقصى هو 10 ميجابايت";
                    return RedirectToAction(nameof(Index));
                }

                // Save file to wwwroot/uploads/lab-results/{userId}/{filename}
                var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "lab-results", userId);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(viewModel.LabFile.FileName)}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.LabFile.CopyToAsync(fileStream);
                }

                var relativePath = $"/uploads/lab-results/{userId}/{uniqueFileName}";

                var uploadDto = new Etmen_BLL.DTOs.Lab.LabUploadDto
                {
                    PatientId = viewModel.PatientId,
                    TestName = viewModel.TestName,
                    TestDate = viewModel.TestDate,
                    FilePath = relativePath,
                    UseOcr = viewModel.UseOcr
                };

                var result = await _labService.UploadLabResultAsync(uploadDto);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Lab result uploaded and saved to: {Path} for user {UserId}", relativePath, userId);
                    TempData["Success"] = "تم تحميل نتيجة الاختبار بنجاح" + (viewModel.UseOcr ? " وجاري تحليلها بالذكاء الاصطناعي" : "");
                }
                else
                {
                    // Cleanup file if DB upload failed
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    TempData["Error"] = result.ErrorMessage ?? "فشل تسجيل نتيجة التحليل في النظام";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading lab result");
                TempData["Error"] = "خطأ في تحميل نتيجة الاختبار";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /LabResults/DownloadPdf/{id}
        /// Generates and downloads the PDF version of a lab result
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid lab result ID");

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var profileResult = await _patientService.GetProfileAsync(userId);
                if (!profileResult.IsSuccess || profileResult.Data == null)
                {
                    TempData["Error"] = "ملف المريض غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                var labResult = await _labService.GetLabResultByIdAsync(id);
                if (!labResult.IsSuccess || labResult.Data == null)
                {
                    TempData["Error"] = "نتيجة التحليل غير موجودة";
                    return RedirectToAction(nameof(Index));
                }

                if (labResult.Data.PatientId != profileResult.Data.Id)
                {
                    _logger.LogWarning("User {UserId} unauthorized download attempt of lab result {LabId}", userId, id);
                    return Forbid();
                }

                var data = labResult.Data;
                var pdfBytes = await _pdfReportService.GenerateLabReportPdfAsync(
                    profileResult.Data.FullName ?? "المريض",
                    data.TestName,
                    data.TestDate,
                    data.Results,
                    data.OcrExtractedData
                );

                var cleanTestName = data.TestName.Replace(" ", "_");
                var fileName = $"Lab_Result_{cleanTestName}_{id}.pdf";
                
                _logger.LogInformation("Lab result PDF download triggered for result {LabId} by user {UserId}", id, userId);
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating lab result PDF for ID {LabId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل ملف تقرير التحليل";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
