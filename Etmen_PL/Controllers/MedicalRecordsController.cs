using Etmen_BLL.Repositories.IServices;
using Etmen_PL.Models.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Medical Records Controller
    /// Renders vitals logs and logs manual entries
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class MedicalRecordsController : Controller
    {
        private readonly IMedicalRecordService _medicalRecordService;
        private readonly ILogger<MedicalRecordsController> _logger;

        public MedicalRecordsController(
            IMedicalRecordService medicalRecordService,
            ILogger<MedicalRecordsController> logger)
        {
            _medicalRecordService = medicalRecordService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /MedicalRecords/Index
        /// Lists previous medical logs
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                _logger.LogInformation("Medical records list accessed for user {UserId}", userId);

                var recordsResult = await _medicalRecordService.GetByPatientAsync(userId);

                if (!recordsResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to fetch medical records for user {UserId}", userId);
                    ModelState.AddModelError(string.Empty, "Failed to load medical records");
                    return View(new List<Etmen_BLL.DTOs.Medical.MedicalRecordDto>());
                }

                return View(recordsResult.Data ?? new List<Etmen_BLL.DTOs.Medical.MedicalRecordDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical records");
                TempData["Error"] = "Error loading medical records";
                return View(new List<Etmen_BLL.DTOs.Medical.MedicalRecordDto>());
            }
        }

        /// <summary>
        /// GET: /MedicalRecords/Details
        /// Shows details of a specific medical record
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid record ID";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var recordResult = await _medicalRecordService.GetByIdAsync(userId, id);

                if (!recordResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to fetch medical record {RecordId} for user {UserId}", id, userId);
                    TempData["Error"] = "Record not found or access denied";
                    return RedirectToAction(nameof(Index));
                }

                return View(recordResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical record {RecordId}", id);
                TempData["Error"] = "Error loading medical record";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /MedicalRecords/Create
        /// Show form to create new medical record
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                return View(new MedicalRecordCreateViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create medical record form");
                TempData["Error"] = "Error loading form";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /MedicalRecords/Create
        /// Logs a manual patient vitals entry
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalRecordCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var recordDto = new Etmen_BLL.DTOs.Medical.MedicalRecordCreateDto
                {
                    PatientId = 0,
                    RecordDate = viewModel.RecordDate,
                    SystolicBP = viewModel.SystolicBP,
                    DiastolicBP = viewModel.DiastolicBP,
                    BloodSugar = viewModel.BloodSugar,
                    HeartRate = viewModel.HeartRate,
                    Temperature = viewModel.Temperature,
                    OxygenSaturation = viewModel.OxygenSaturation,
                    Symptoms = viewModel.Symptoms,
                    Diagnosis = viewModel.Diagnosis,
                    Treatment = viewModel.Treatment,
                    Notes = viewModel.Notes
                };

                var result = await _medicalRecordService.CreateAsync(userId, recordDto);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to create medical record for user {UserId}", userId);
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error);
                    return View(viewModel);
                }

                _logger.LogInformation("Medical record created for user {UserId}", userId);
                TempData["Success"] = "Medical record created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating medical record");
                ModelState.AddModelError(string.Empty, "Error creating medical record");
                return View(viewModel);
            }
        }

        /// <summary>
        /// GET: /MedicalRecords/Edit
        /// Show form to edit medical record
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid record ID";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var recordResult = await _medicalRecordService.GetByIdAsync(userId, id);

                if (!recordResult.IsSuccess)
                {
                    TempData["Error"] = "Record not found";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new MedicalRecordCreateViewModel
                {
                    SystolicBP = recordResult.Data?.SystolicBP,
                    DiastolicBP = recordResult.Data?.DiastolicBP,
                    BloodSugar = recordResult.Data?.BloodSugar,
                    HeartRate = recordResult.Data?.HeartRate,
                    Temperature = recordResult.Data?.Temperature,
                    OxygenSaturation = recordResult.Data?.OxygenSaturation,
                    Symptoms = recordResult.Data?.Symptoms,
                    RecordDate = recordResult.Data?.RecordDate ?? DateTime.Now,
                    Notes = recordResult.Data?.Notes
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for record {RecordId}", id);
                TempData["Error"] = "Error loading form";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /MedicalRecords/Edit
        /// Updates an existing medical record
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MedicalRecordCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid record ID";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // For now, delete and recreate (since API might not have full update)
                // A better approach would be if IMedicalRecordService had an UpdateAsync method
                var deleteResult = await _medicalRecordService.DeleteAsync(userId, id);
                if (!deleteResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to update medical record {RecordId} for user {UserId}", id, userId);
                    ModelState.AddModelError(string.Empty, "Failed to update record");
                    return View(viewModel);
                }

                var recordDto = new Etmen_BLL.DTOs.Medical.MedicalRecordCreateDto
                {
                    PatientId = 0,
                    RecordDate = viewModel.RecordDate,
                    SystolicBP = viewModel.SystolicBP,
                    DiastolicBP = viewModel.DiastolicBP,
                    BloodSugar = viewModel.BloodSugar,
                    HeartRate = viewModel.HeartRate,
                    Temperature = viewModel.Temperature,
                    OxygenSaturation = viewModel.OxygenSaturation,
                    Symptoms = viewModel.Symptoms,
                    Diagnosis = viewModel.Diagnosis,
                    Treatment = viewModel.Treatment,
                    Notes = viewModel.Notes
                };

                var createResult = await _medicalRecordService.CreateAsync(userId, recordDto);
                if (!createResult.IsSuccess)
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError(string.Empty, error);
                    return View(viewModel);
                }

                _logger.LogInformation("Medical record {RecordId} updated for user {UserId}", id, userId);
                TempData["Success"] = "Medical record updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating medical record {RecordId}", id);
                ModelState.AddModelError(string.Empty, "Error updating medical record");
                return View(viewModel);
            }
        }

        /// <summary>
        /// POST: /MedicalRecords/Delete
        /// Deletes a medical record
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid record ID";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _medicalRecordService.DeleteAsync(userId, id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to delete medical record {RecordId} for user {UserId}", id, userId);
                    TempData["Error"] = result.Errors.FirstOrDefault() ?? "Failed to delete record";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Medical record {RecordId} deleted for user {UserId}", id, userId);
                TempData["Success"] = "Medical record deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting medical record {RecordId}", id);
                TempData["Error"] = "Error deleting medical record";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /MedicalRecords/ByDateRange
        /// Get medical records filtered by date range
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ByDateRange(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                if (!startDate.HasValue || !endDate.HasValue)
                {
                    TempData["Error"] = "Date range must be specified";
                    return RedirectToAction(nameof(Index));
                }

                if (startDate > endDate)
                {
                    TempData["Error"] = "Start date must be before end date";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Fetching medical records for user {UserId} from {StartDate} to {EndDate}", 
                    userId, startDate, endDate);

                var recordsResult = await _medicalRecordService.GetByDateRangeAsync(userId, startDate.Value, endDate.Value);

                if (!recordsResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to fetch records by date range for user {UserId}", userId);
                    ModelState.AddModelError(string.Empty, "Failed to load records");
                    return View(nameof(Index), new List<Etmen_BLL.DTOs.Medical.MedicalRecordDto>());
                }

                return View(nameof(Index), recordsResult.Data ?? new List<Etmen_BLL.DTOs.Medical.MedicalRecordDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving records by date range");
                TempData["Error"] = "Error loading records";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /MedicalRecords/AbnormalValues
        /// Get medical records with abnormal vital values
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AbnormalValues()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                _logger.LogInformation("Fetching abnormal records for user {UserId}", userId);

                var recordsResult = await _medicalRecordService.GetWithAbnormalValuesAsync(userId);

                if (!recordsResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to fetch abnormal records for user {UserId}", userId);
                    ModelState.AddModelError(string.Empty, "Failed to load records");
                    return View(nameof(Index), new List<Etmen_BLL.DTOs.Medical.MedicalRecordDto>());
                }

                return View(nameof(Index), recordsResult.Data ?? new List<Etmen_BLL.DTOs.Medical.MedicalRecordDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving abnormal records");
                TempData["Error"] = "Error loading records";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
