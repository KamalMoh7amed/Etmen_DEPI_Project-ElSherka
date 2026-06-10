using Etmen_BLL.Repositories.IServices;
using Etmen_BLL.DTOs.Crisis;
using Etmen_PL.Models.ViewModels.Admin;
using Etmen_PL.Models.ViewModels.Crisis;
using Etmen_Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Admin Crisis Controller
    /// Configures epidemics, symptom weights, outbreak maps, and approves escalations
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminCrisisController : Controller
    {
        private readonly ICrisisService _crisisService;
        private readonly ICriticalIntelligenceService _criticalIntelligenceService;
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminCrisisController> _logger;

        public AdminCrisisController(
            ICrisisService crisisService,
            ICriticalIntelligenceService criticalIntelligenceService,
            IAdminService adminService,
            ILogger<AdminCrisisController> logger)
        {
            _crisisService = crisisService;
            _criticalIntelligenceService = criticalIntelligenceService;
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /AdminCrisis/Index
        /// Lists all configured crises (active/inactive)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _crisisService.GetAllCrisesAsync();
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error loading crises";
                    return RedirectToAction("Index", "AdminDashboard");
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving crises");
                TempData["Error"] = "خطأ في تحميل الأزمات";
                return RedirectToAction("Index", "AdminDashboard");
            }
        }

        /// <summary>
        /// GET: /AdminCrisis/Create
        /// Form to configure a new epidemic
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new CreateCrisisViewModel();
            return View(viewModel);
        }

        /// <summary>
        /// POST: /AdminCrisis/Create
        /// Saves new crisis template
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCrisisViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                if (!TryBuildCreateCrisisDto(viewModel, out var dto))
                    return View(viewModel);

                var result = await _crisisService.CreateCrisisAsync(dto);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error creating crisis");
                    return View(viewModel);
                }

                _logger.LogInformation("Crisis {CrisisName} created", viewModel.CrisisName);
                TempData["Success"] = "تم إنشاء الأزمة بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating crisis");
                ModelState.AddModelError(string.Empty, "خطأ في إنشاء الأزمة");
                return View(viewModel);
            }
        }

        /// <summary>
        /// GET: /AdminCrisis/Edit
        /// Form to edit crisis fields
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                var result = await _crisisService.GetCrisisByIdAsync(id);
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Crisis not found";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new CreateCrisisViewModel
                {
                    CrisisName = result.Data.CrisisName,
                    CrisisType = result.Data.CrisisType.ToString(),
                    SystemMode = result.Data.SystemMode.ToString(),
                    StartDate = result.Data.StartDate,
                    EndDate = result.Data.EndDate
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading crisis");
                TempData["Error"] = "خطأ في تحميل الأزمة";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminCrisis/Edit
        /// Submits edits to crisis thresholds
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateCrisisViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                if (id <= 0)
                    return BadRequest();

                if (!TryBuildEditCrisisDto(id, viewModel, out var dto))
                    return View(viewModel);

                var result = await _crisisService.UpdateCrisisAsync(id, dto);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error updating crisis");
                    return View(viewModel);
                }

                _logger.LogInformation("Crisis {CrisisId} updated", id);
                TempData["Success"] = "تم تحديث الأزمة بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating crisis");
                ModelState.AddModelError(string.Empty, "خطأ في تحديث الأزمة");
                return View(viewModel);
            }
        }

        /// <summary>
        /// GET: /AdminCrisis/Details
        /// Shows crisis configuration, weights, and stats
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                var crisisResult = await _crisisService.GetCrisisByIdAsync(id);
                if (!crisisResult.IsSuccess || crisisResult.Data is null)
                {
                    TempData["Error"] = crisisResult.ErrorMessage ?? "Crisis not found";
                    return RedirectToAction(nameof(Index));
                }

                var statsResult = await _crisisService.GetCrisisStatsAsync(id);
                if (statsResult.IsSuccess)
                    ViewData["Stats"] = statsResult.Data;

                return View(crisisResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving crisis details");
                TempData["Error"] = "خطأ في تحميل تفاصيل الأزمة";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminCrisis/Activate
        /// Activates crisis mode for a crisis configuration
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                var result = await _crisisService.ActivateCrisisAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error activating crisis";
                    return RedirectToAction(nameof(Details), new { id });
                }

                _logger.LogInformation("Crisis {CrisisId} activated", id);
                TempData["Success"] = "تم تفعيل الأزمة بنجاح";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating crisis");
                TempData["Error"] = "خطأ في تفعيل الأزمة";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// POST: /AdminCrisis/Deactivate
        /// Deactivates a crisis configuration
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                var result = await _crisisService.DeactivateCrisisAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error deactivating crisis";
                    return RedirectToAction(nameof(Details), new { id });
                }

                _logger.LogInformation("Crisis {CrisisId} deactivated", id);
                TempData["Success"] = "تم إلغاء تفعيل الأزمة بنجاح";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating crisis");
                TempData["Error"] = "خطأ في إلغاء تفعيل الأزمة";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// POST: /AdminCrisis/AddSymptom
        /// Associates a symptom weight with a crisis template
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSymptom(int crisisId, SymptomWeightViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Details), new { crisisId });

            try
            {
                if (crisisId <= 0)
                    return BadRequest();

                var dto = new SymptomWeightDto
                {
                    SymptomName = viewModel.SymptomName,
                    Weight = viewModel.Weight,
                    IsEmergencySymptom = viewModel.IsEmergencySymptom
                };

                var result = await _crisisService.AddSymptomAsync(crisisId, dto);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error adding symptom";
                    return RedirectToAction(nameof(Details), new { id = crisisId });
                }

                _logger.LogInformation("Symptom {SymptomName} added to crisis {CrisisId}", viewModel.SymptomName, crisisId);
                TempData["Success"] = "تم إضافة الأعراض بنجاح";
                return RedirectToAction(nameof(Details), new { id = crisisId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding symptom");
                TempData["Error"] = "خطأ في إضافة الأعراض";
                return RedirectToAction(nameof(Details), new { id = crisisId });
            }
        }

        /// <summary>
        /// POST: /AdminCrisis/UpdateSymptom
        /// Updates a symptom's weight multiplier
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSymptom(int crisisId, string symptomName, SymptomWeightViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Details), new { crisisId });

            try
            {
                if (crisisId <= 0 || string.IsNullOrWhiteSpace(symptomName))
                    return BadRequest();

                var dto = new SymptomWeightDto
                {
                    SymptomName = viewModel.SymptomName,
                    Weight = viewModel.Weight,
                    IsEmergencySymptom = viewModel.IsEmergencySymptom
                };

                var result = await _crisisService.UpdateSymptomAsync(crisisId, symptomName, dto);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error updating symptom";
                    return RedirectToAction(nameof(Details), new { id = crisisId });
                }

                _logger.LogInformation("Symptom {SymptomName} updated for crisis {CrisisId}", symptomName, crisisId);
                TempData["Success"] = "تم تحديث الأعراض بنجاح";
                return RedirectToAction(nameof(Details), new { id = crisisId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating symptom");
                TempData["Error"] = "خطأ في تحديث الأعراض";
                return RedirectToAction(nameof(Details), new { id = crisisId });
            }
        }

        /// <summary>
        /// POST: /AdminCrisis/RemoveSymptom
        /// Deletes a symptom association
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSymptom(int crisisId, string symptomName)
        {
            try
            {
                if (crisisId <= 0 || string.IsNullOrWhiteSpace(symptomName))
                    return BadRequest();

                var result = await _crisisService.RemoveSymptomAsync(crisisId, symptomName);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error removing symptom";
                    return RedirectToAction(nameof(Details), new { id = crisisId });
                }

                _logger.LogInformation("Symptom {SymptomName} removed from crisis {CrisisId}", symptomName, crisisId);
                TempData["Success"] = "تم حذف الأعراض بنجاح";
                return RedirectToAction(nameof(Details), new { id = crisisId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing symptom");
                TempData["Error"] = "خطأ في حذف الأعراض";
                return RedirectToAction(nameof(Details), new { id = crisisId });
            }
        }

        /// <summary>
        /// GET: /AdminCrisis/CommandCenter
        /// Dashboard showing real-time dispatch wait times
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CommandCenter()
        {
            try
            {
                var result = await _criticalIntelligenceService.GetCommandCenterAsync();
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error loading command center";
                    return RedirectToAction("Index", "AdminDashboard");
                }

                var viewModel = new CrisisCommandCenterViewModel
                {
                    ActiveCriticalCases = result.Data.ActiveCriticalCases,
                    WaitingForHospital = result.Data.WaitingForHospital,
                    HospitalAccepted = result.Data.HospitalAccepted,
                    WaitingForDoctor = result.Data.WaitingForDoctor,
                    DoctorAssigned = result.Data.DoctorAssigned,
                    AverageWaitingMinutes = result.Data.AverageWaitingMinutes,
                    Cases = result.Data.Cases
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving command center");
                TempData["Error"] = "خطأ في تحميل مركز التحكم";
                return RedirectToAction("Index", "AdminDashboard");
            }
        }

        /// <summary>
        /// GET: /AdminCrisis/Heatmap
        /// Shows map of critical clusters and outbreak zones
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Heatmap(int? crisisId = null)
        {
            try
            {
                var result = await _criticalIntelligenceService.GetCrisisHeatmapAsync(crisisId);
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error loading heatmap";
                    return RedirectToAction("Index", "AdminDashboard");
                }

                var viewModel = new CrisisHeatmapViewModel
                {
                    CrisisId = result.Data.CrisisId,
                    TotalGeoTaggedCriticalCases = result.Data.TotalGeoTaggedCriticalCases,
                    Points = result.Data.Points,
                    Zones = result.Data.Zones
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving heatmap");
                TempData["Error"] = "خطأ في تحميل الخريطة";
                return RedirectToAction("Index", "AdminDashboard");
            }
        }

        /// <summary>
        /// POST: /AdminCrisis/Approve
        /// Admin approval for newly escalated outbreak zones
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                var result = await _adminService.ApproveCrisisAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error approving crisis";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Crisis {CrisisId} approved", id);
                TempData["Success"] = "تم الموافقة على الأزمة بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving crisis");
                TempData["Error"] = "خطأ في الموافقة على الأزمة";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminCrisis/Reject
        /// Rejects an escalated zone request with a reason
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "يجب توفير سبب الرفض";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (id <= 0)
                    return BadRequest();

                var result = await _adminService.RejectCrisisAsync(id, reason);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error rejecting crisis";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Crisis {CrisisId} rejected", id);
                TempData["Success"] = "تم رفض الأزمة بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting crisis");
                TempData["Error"] = "خطأ في رفض الأزمة";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool TryBuildCreateCrisisDto(CreateCrisisViewModel viewModel, out CreateCrisisDto dto)
        {
            dto = new CreateCrisisDto();

            if (!TryParseEnums(viewModel.CrisisType, viewModel.SystemMode, out var crisisType, out var systemMode))
                return false;

            dto = new CreateCrisisDto
            {
                CrisisName = viewModel.CrisisName,
                CrisisType = crisisType,
                SystemMode = systemMode,
                Description = viewModel.Description,
                StartDate = viewModel.StartDate,
                EndDate = viewModel.EndDate,
                EmergencyThreshold = viewModel.EmergencyThreshold,
                HighRiskThreshold = viewModel.HighRiskThreshold,
                MediumRiskThreshold = viewModel.MediumRiskThreshold
            };

            return true;
        }

        private bool TryBuildEditCrisisDto(int id, CreateCrisisViewModel viewModel, out EditCrisisDto dto)
        {
            dto = new EditCrisisDto();

            if (!TryParseEnums(viewModel.CrisisType, viewModel.SystemMode, out var crisisType, out var systemMode))
                return false;

            dto = new EditCrisisDto
            {
                Id = id,
                CrisisName = viewModel.CrisisName,
                CrisisType = crisisType,
                SystemMode = systemMode,
                Description = viewModel.Description,
                EndDate = viewModel.EndDate,
                EmergencyThreshold = viewModel.EmergencyThreshold,
                HighRiskThreshold = viewModel.HighRiskThreshold,
                MediumRiskThreshold = viewModel.MediumRiskThreshold
            };

            return true;
        }

        private bool TryParseEnums(string crisisTypeValue, string systemModeValue, out CrisisType crisisType, out SystemMode systemMode)
        {
            var isCrisisTypeValid = Enum.TryParse(crisisTypeValue, true, out crisisType);
            var isSystemModeValid = Enum.TryParse(systemModeValue, true, out systemMode);

            if (!isCrisisTypeValid)
                ModelState.AddModelError(nameof(CreateCrisisViewModel.CrisisType), "Invalid crisis type");

            if (!isSystemModeValid)
                ModelState.AddModelError(nameof(CreateCrisisViewModel.SystemMode), "Invalid system mode");

            return isCrisisTypeValid && isSystemModeValid;
        }
    }
}
