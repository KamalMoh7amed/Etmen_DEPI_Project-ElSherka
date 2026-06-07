using Etmen_BLL.Repositories.IServices;
using Etmen_BLL.DTOs.Admin;
using Etmen_PL.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    /// <summary>
    /// Admin Providers Controller
    /// Registers and manages healthcare centers
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminProvidersController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminProvidersController> _logger;

        public AdminProvidersController(
            IAdminService adminService,
            ILogger<AdminProvidersController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /AdminProviders/Index
        /// Lists provider centers with locations
        /// TODO: Parse pageNumber from query parameter (default 1)
        /// TODO: Call _adminService.GetAllProvidersAsync(pageNumber)
        /// TODO: Return View with PaginatedResult<ProviderListItemDto>
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            try
            {
                pageNumber = Math.Max(pageNumber, 1);

                var result = await _adminService.GetAllProvidersAsync(pageNumber);
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error loading providers";
                    return RedirectToAction("Index", "AdminDashboard");
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving providers");
                TempData["Error"] = "خطأ في تحميل المراكز الصحية";
                return RedirectToAction("Index", "AdminDashboard");
            }
        }

        /// <summary>
        /// GET: /AdminProviders/Create
        /// Form to register a hospital/clinic profile
        /// TODO: Return View with new CreateProviderViewModel
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new CreateProviderViewModel();
            return View(viewModel);
        }

        /// <summary>
        /// POST: /AdminProviders/Create
        /// Submits details to register a new provider
        /// TODO: Validate ModelState
        /// TODO: Call _adminService.CreateProviderAsync(dto)
        /// TODO: Redirect to Index on success
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProviderViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                var dto = new CreateProviderDto
                {
                    Name = viewModel.Name,
                    Type = viewModel.ProviderType ?? string.Empty,
                    Address = viewModel.Address,
                    Phone = viewModel.PhoneNumber,
                    AvailableBeds = viewModel.AvailableBeds,
                    Latitude = viewModel.Latitude,
                    Longitude = viewModel.Longitude,
                    IsEmergencyCenter = viewModel.AvailableBeds.GetValueOrDefault() > 0
                };

                var result = await _adminService.CreateProviderAsync(dto);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error creating provider");
                    return View(viewModel);
                }

                _logger.LogInformation("Provider {ProviderName} created", viewModel.Name);
                TempData["Success"] = "تم إنشاء المركز الصحي بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider");
                ModelState.AddModelError(string.Empty, "خطأ في إنشاء المركز الصحي");
                return View(viewModel);
            }
        }

        /// <summary>
        /// GET: /AdminProviders/Edit
        /// Renders edit provider interface
        /// TODO: Validate id parameter
        /// TODO: Call _adminService.GetProviderByIdAsync(id)
        /// TODO: Return View with provider data
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                var result = await _adminService.GetProviderByIdAsync(id);
                if (!result.IsSuccess || result.Data is null)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Provider not found";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new CreateProviderViewModel
                {
                    Name = result.Data.Name,
                    ProviderType = result.Data.Type,
                    Address = result.Data.Address,
                    PhoneNumber = result.Data.Phone,
                    AvailableBeds = result.Data.AvailableBeds,
                    Latitude = result.Data.Latitude,
                    Longitude = result.Data.Longitude,
                    IsActive = result.Data.IsActive
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading provider");
                TempData["Error"] = "خطأ في تحميل المركز الصحي";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminProviders/Edit
        /// Saves updates to provider coordinates, beds, and status
        /// TODO: Validate ModelState and id
        /// TODO: Call _adminService.UpdateProviderAsync(id, dto)
        /// TODO: Redirect to Index on success
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateProviderViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            try
            {
                if (id <= 0)
                    return BadRequest();

                var dto = new UpdateProviderDto
                {
                    Id = id,
                    Name = viewModel.Name,
                    Type = viewModel.ProviderType ?? string.Empty,
                    Address = viewModel.Address,
                    Phone = viewModel.PhoneNumber,
                    AvailableBeds = viewModel.AvailableBeds,
                    Latitude = viewModel.Latitude,
                    Longitude = viewModel.Longitude,
                    IsEmergencyCenter = viewModel.AvailableBeds.GetValueOrDefault() > 0,
                    IsActive = viewModel.IsActive
                };

                var result = await _adminService.UpdateProviderAsync(id, dto);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error updating provider");
                    return View(viewModel);
                }

                _logger.LogInformation("Provider {ProviderId} updated", id);
                TempData["Success"] = "تم تحديث المركز الصحي بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider");
                ModelState.AddModelError(string.Empty, "خطأ في تحديث المركز الصحي");
                return View(viewModel);
            }
        }

        /// <summary>
        /// POST: /AdminProviders/Delete
        /// Deletes a registered provider center
        /// TODO: Validate id parameter
        /// TODO: Call _adminService.DeleteProviderAsync(id)
        /// TODO: Redirect to Index on success
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest();

                var result = await _adminService.DeleteProviderAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Error deleting provider";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Provider {ProviderId} deleted", id);
                TempData["Success"] = "تم حذف المركز الصحي بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting provider");
                TempData["Error"] = "خطأ في حذف المركز الصحي";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
