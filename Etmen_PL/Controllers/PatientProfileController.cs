using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Patient Profile Controller
    /// Renders and saves patient baseline medical metrics
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class PatientProfileController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientProfileController> _logger;

        public PatientProfileController(
            IPatientService patientService,
            ILogger<PatientProfileController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /PatientProfile/Index
        /// Renders patient profile editing page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var result = await _patientService.GetProfileAsync(userId);
                if (!result.IsSuccess || result.Data == null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "فشل تحميل الملف الشخصي";
                    return RedirectToAction("Index", "PatientDashboard");
                }

                var viewModel = new PatientProfileViewModel
                {
                    FullName = result.Data.FullName ?? string.Empty,
                    DateOfBirth = result.Data.DateOfBirth,
                    Gender = result.Data.Gender,
                    Height = result.Data.Height,
                    Weight = result.Data.Weight,
                    ActivityLevel = result.Data.ActivityLevel,
                    BloodType = result.Data.BloodType,
                    HasChronicDiseases = result.Data.HasChronicDiseases,
                    ChronicDiseasesNotes = result.Data.ChronicDiseasesNotes,
                    Allergies = result.Data.Allergies,
                    CurrentMedications = result.Data.CurrentMedications
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patient profile");
                TempData["Error"] = "خطأ في تحميل الملف الشخصي";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }

        /// <summary>
        /// POST: /PatientProfile/Index
        /// Updates patient metrics (weight, allergies, etc.)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PatientProfileViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var dto = new Etmen_BLL.DTOs.Patient.ProfileDto
                {
                    FullName = viewModel.FullName,
                    DateOfBirth = viewModel.DateOfBirth,
                    Gender = viewModel.Gender,
                    Height = viewModel.Height,
                    Weight = viewModel.Weight,
                    ActivityLevel = viewModel.ActivityLevel,
                    BloodType = viewModel.BloodType,
                    HasChronicDiseases = viewModel.HasChronicDiseases,
                    ChronicDiseasesNotes = viewModel.ChronicDiseasesNotes,
                    Allergies = viewModel.Allergies,
                    CurrentMedications = viewModel.CurrentMedications
                };

                var result = await _patientService.UpdateProfileAsync(userId, dto);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Patient profile updated for user {UserId}", userId);
                    TempData["Success"] = "تم تحديث الملف الشخصي بنجاح";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient profile");
                ModelState.AddModelError(string.Empty, "خطأ في تحديث الملف الشخصي");
                return View(viewModel);
            }
        }
    }
}
