using Etmen_BLL.Repositories.IServices;
using Etmen_BLL.DTOs.Admin;
using Etmen_PL.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Etmen_Domain.Entities;
using Etmen_DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
        private readonly IHospitalStaffService _hospitalStaffService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminProvidersController> _logger;

        public AdminProvidersController(
            IAdminService adminService,
            IHospitalStaffService hospitalStaffService,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork uow,
            IEmailService emailService,
            ILogger<AdminProvidersController> logger)
        {
            _adminService = adminService;
            _hospitalStaffService = hospitalStaffService;
            _userManager = userManager;
            _uow = uow;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /AdminProviders/Index
        /// Lists provider centers with locations
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
                    IsEmergencyCenter = viewModel.IsEmergencyCenter
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
                    IsEmergencyCenter = result.Data.IsEmergencyCenter,
                    IsActive = result.Data.IsActive
                };

                await PopulateStaffMembersAsync(id);
                await PopulateDoctorsAsync(id);
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
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateProviderViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                await PopulateStaffMembersAsync(id);
                await PopulateDoctorsAsync(id);
                return View(viewModel);
            }

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
                    IsEmergencyCenter = viewModel.IsEmergencyCenter,
                    IsActive = viewModel.IsActive
                };

                var result = await _adminService.UpdateProviderAsync(id, dto);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error updating provider");
                    await PopulateStaffMembersAsync(id);
                    await PopulateDoctorsAsync(id);
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
                await PopulateStaffMembersAsync(id);
                await PopulateDoctorsAsync(id);
                return View(viewModel);
            }
        }

        /// <summary>
        /// POST: /AdminProviders/Delete
        /// Deletes a registered provider center
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
        private async Task PopulateStaffMembersAsync(int providerId)
        {
            ViewBag.ProviderId = providerId;
            var staffResult = await _hospitalStaffService.GetStaffMembersAsync(providerId);
            ViewBag.StaffMembers = staffResult.IsSuccess ? staffResult.Data : new List<Etmen_BLL.DTOs.HospitalStaff.StaffProfileDto>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctorAffiliation(int providerId, int? doctorId, string? firstName, string? lastName, string? email, string? phoneNumber, string? specialization, string? affiliationRole, bool isEmergencyDoctor, bool isOwner)
        {
            try
            {
                if (providerId <= 0)
                    return BadRequest();

                DoctorProfile? doctorProfile = null;

                if (doctorId.HasValue && doctorId.Value > 0)
                {
                    // Adding existing doctor
                    doctorProfile = await _uow.DoctorProfiles.GetByIdAsync(doctorId.Value);
                    if (doctorProfile == null)
                    {
                        TempData["Error"] = "طبيب غير موجود";
                        return RedirectToAction(nameof(Edit), new { id = providerId });
                    }
                }
                else
                {
                    // Auto-registering a new doctor
                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                    {
                        TempData["Error"] = "الاسم الأول والأخير والبريد الإلكتروني مطلوبين لتسجيل طبيب جديد.";
                        return RedirectToAction(nameof(Edit), new { id = providerId });
                    }

                    var existingUser = await _userManager.FindByEmailAsync(email);
                    if (existingUser != null)
                    {
                        // Check if they already have a DoctorProfile
                        doctorProfile = await _uow.DoctorProfiles.Table.FirstOrDefaultAsync(d => d.ApplicationUserId == existingUser.Id);
                        if (doctorProfile == null)
                        {
                            // If they exist but don't have a doctor profile, create one
                            doctorProfile = new DoctorProfile
                            {
                                ApplicationUserId = existingUser.Id,
                                FullName = $"{firstName} {lastName}".Trim(),
                                Specialization = specialization,
                                CreatedAt = DateTime.UtcNow,
                                IsOnboarded = true
                            };
                            await _uow.DoctorProfiles.AddAsync(doctorProfile);
                            await _uow.CompleteAsync();
                        }
                    }
                    else
                    {
                        // Create new user
                        var user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FirstName = firstName,
                            LastName = lastName,
                            PhoneNumber = phoneNumber,
                            IsEmailVerified = true, // auto-verify
                            CreatedAt = DateTime.UtcNow
                        };

                        var tempPassword = "Doc@" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        var result = await _userManager.CreateAsync(user, tempPassword);
                        if (!result.Succeeded)
                        {
                            TempData["Error"] = "فشل إنشاء حساب الطبيب الجديد: " + string.Join(", ", result.Errors.Select(e => e.Description));
                            return RedirectToAction(nameof(Edit), new { id = providerId });
                        }

                        await _userManager.AddToRoleAsync(user, "Doctor");

                        doctorProfile = new DoctorProfile
                        {
                            ApplicationUserId = user.Id,
                            FullName = $"{firstName} {lastName}".Trim(),
                            Specialization = specialization,
                            CreatedAt = DateTime.UtcNow,
                            IsOnboarded = true
                        };

                        await _uow.DoctorProfiles.AddAsync(doctorProfile);
                        await _uow.CompleteAsync();

                        // Send email with credentials
                        try
                        {
                            var emailBody = $@"
                                <h3>مرحباً بك دكتور {firstName} {lastName} في منصة اطمئن</h3>
                                <p>تم تسجيل حسابك كطبيب من قبل إدارة النظام وربطك بالمركز الطبي.</p>
                                <p><strong>بيانات تسجيل الدخول المؤقتة:</strong></p>
                                <ul>
                                    <li>البريد الإلكتروني: {email}</li>
                                    <li>كلمة المرور المؤقتة: {tempPassword}</li>
                                </ul>
                                <p>يرجى تسجيل الدخول وتغيير كلمة المرور فوراً.</p>";
                            
                            await _emailService.SendEmailAsync(email, $"{firstName} {lastName}", "بيانات حساب الطبيب الجديد - منصة اطمئن", emailBody);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send credentials email to doctor");
                        }
                    }
                }

                // Create the DoctorProvider affiliation
                var existingAffiliation = await _uow.DoctorProviders.GetAffiliationAsync(doctorProfile.Id, providerId);
                if (existingAffiliation != null)
                {
                    existingAffiliation.IsEmergencyDoctor = isEmergencyDoctor;
                    existingAffiliation.AffiliationRole = affiliationRole;
                    existingAffiliation.IsOwner = isOwner;
                    _uow.DoctorProviders.Update(existingAffiliation);
                }
                else
                {
                    var affiliation = new DoctorProvider
                    {
                        DoctorProfileId = doctorProfile.Id,
                        HealthcareProviderId = providerId,
                        IsEmergencyDoctor = isEmergencyDoctor,
                        AffiliationRole = affiliationRole,
                        IsOwner = isOwner
                    };
                    await _uow.DoctorProviders.AddAsync(affiliation);
                }

                await _uow.CompleteAsync();
                TempData["Success"] = "تم ربط الطبيب بالمنشأة بنجاح.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding doctor affiliation");
                TempData["Error"] = "حدث خطأ غير متوقع أثناء ربط الطبيب.";
            }

            return RedirectToAction(nameof(Edit), new { id = providerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDoctorAffiliation(int providerId, int doctorId)
        {
            try
            {
                if (providerId <= 0 || doctorId <= 0)
                    return BadRequest();

                var affiliation = await _uow.DoctorProviders.GetAffiliationAsync(doctorId, providerId);
                if (affiliation != null)
                {
                    _uow.DoctorProviders.Remove(affiliation);
                    await _uow.CompleteAsync();
                    TempData["Success"] = "تم إلغاء ربط الطبيب بالمنشأة بنجاح.";
                }
                else
                {
                    TempData["Error"] = "الارتباط غير موجود.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing doctor affiliation");
                TempData["Error"] = "حدث خطأ غير متوقع أثناء إلغاء الربط.";
            }

            return RedirectToAction(nameof(Edit), new { id = providerId });
        }

        private async Task PopulateDoctorsAsync(int providerId)
        {
            var affiliations = await _uow.DoctorProviders.GetByProviderIdAsync(providerId);
            ViewBag.AffiliatedDoctors = affiliations.ToList();

            var allDoctors = await _uow.DoctorProfiles.Table
                .Include(d => d.ApplicationUser)
                .ToListAsync();

            var affiliatedDoctorIds = affiliations.Select(a => a.DoctorProfileId).ToHashSet();
            ViewBag.AllDoctors = allDoctors.Where(d => !affiliatedDoctorIds.Contains(d.Id)).ToList();
        }
    }
}
