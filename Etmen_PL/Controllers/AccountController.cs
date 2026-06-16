using Etmen_BLL.DTOs.Auth;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_PL.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Etmen_PL.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHospitalStaffService _hospitalStaffService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IHospitalStaffService hospitalStaffService,
            IUnitOfWork uow,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _signInManager = signInManager;
            _userManager = userManager;
            _hospitalStaffService = hospitalStaffService;
            _uow = uow;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────
        // STEP 1 — Select Role (entry point for new users)
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SelectRole()
        {
            if (User.Identity?.IsAuthenticated == true)
                return await RedirectByRoleAsync();

            return View();
        }

        // ─────────────────────────────────────────────────────────
        // STEP 2 — Register (with role pre-selected)
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string role = "Patient")
        {
            if (User.Identity?.IsAuthenticated == true)
                return await RedirectByRoleAsync();

            var allowedRoles = new[] { "Patient", "Doctor" };
            if (!allowedRoles.Contains(role))
                role = "Patient";

            return View(new RegisterDto { Role = role });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (await _authService.IsEmailTakenAsync(dto.Email))
            {
                ModelState.AddModelError(nameof(dto.Email), "هذا البريد الإلكتروني مسجل بالفعل.");
                return View(dto);
            }

            var result = await _authService.RegisterAsync(dto);

            if (result.IsSuccess)
            {
                _logger.LogInformation("New {Role} registered: {Email}", dto.Role, dto.Email);

                // For Patients: Auto-login and redirect to dashboard
                if (dto.Role == "Patient")
                {
                    var user = await _userManager.FindByEmailAsync(dto.Email);
                    if (user != null)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return await RedirectByRoleAsync("Patient");
                    }
                }

                // For Doctors: Redirect to email verification notice
                TempData["Email"] = dto.Email;
                TempData["RegisteredRole"] = dto.Role;
                return RedirectToAction(nameof(VerifyEmailNotice));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return View(dto);
        }

        // ─────────────────────────────────────────────────────────
        // Registration for Invited Staff via Link
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterStaff(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "رابط الدعوة غير صالح.";
                return RedirectToAction(nameof(Login));
            }

            var profile = await _uow.StaffProfiles.Table
                .Include(p => p.ApplicationUser)
                .Include(p => p.HealthcareProvider)
                .FirstOrDefaultAsync(p => p.InvitationToken == token);

            if (profile == null)
            {
                TempData["Error"] = "رابط الدعوة هذا غير صحيح أو تم إلغاؤه.";
                return RedirectToAction(nameof(Login));
            }

            if (profile.InvitationTokenExpiry.HasValue && profile.InvitationTokenExpiry.Value < DateTime.UtcNow)
            {
                TempData["Error"] = "انتهت صلاحية رابط الدعوة (صالح لمدة 24 ساعة للرابط السريع أو 7 أيام لدعوة البريد).";
                return RedirectToAction(nameof(Login));
            }

            var model = new RegisterStaffViewModel
            {
                Token = token,
                Email = profile.ApplicationUser?.Email ?? string.Empty
            };

            ViewBag.ProviderName = profile.HealthcareProvider.Name;
            ViewBag.IsEmailPrefilled = !string.IsNullOrEmpty(model.Email);

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterStaff(RegisterStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var profile = await _uow.StaffProfiles.Table
                .Include(p => p.ApplicationUser)
                .Include(p => p.HealthcareProvider)
                .FirstOrDefaultAsync(p => p.InvitationToken == model.Token);

            if (profile == null)
            {
                ModelState.AddModelError(string.Empty, "رابط الدعوة غير صحيح.");
                return View(model);
            }

            ApplicationUser? user = null;
            if (profile.ApplicationUser != null)
            {
                user = profile.ApplicationUser;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.MustChangePassword = false;

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (!resetResult.Succeeded)
                {
                    foreach (var error in resetResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }

                await _userManager.UpdateAsync(user);
            }
            else
            {
                if (await _userManager.FindByEmailAsync(model.Email) != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني هذا مسجل بالفعل في النظام.");
                    return View(model);
                }

                user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true,
                    IsActive = true,
                    MustChangePassword = false
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, "HospitalStaff");
            }

            var acceptResult = await _hospitalStaffService.AcceptInvitationAsync(model.Token, user.Id);
            if (!acceptResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, acceptResult.ErrorMessage ?? "فشل قبول الدعوة.");
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["Success"] = "تم إنشاء الحساب وقبول الدعوة بنجاح!";
            return await RedirectByRoleAsync("HospitalStaff");
        }

        // ─────────────────────────────────────────────────────────
        // Force Password Change on First Login
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public IActionResult ForcePasswordChange()
        {
            return View(new ForcePasswordChangeViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForcePasswordChange(ForcePasswordChangeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);

            // Relogin user to refresh cookie
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "تم تحديث كلمة المرور بنجاح. يمكنك المتابعة الآن.";
            return await RedirectByRoleAsync();
        }

        // ─────────────────────────────────────────────────────────
        // Email Verification
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmailNotice()
        {
            ViewBag.Email = TempData["Email"]?.ToString() ?? string.Empty;
            ViewBag.Role  = TempData["RegisteredRole"]?.ToString() ?? "Patient";
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return BadRequest("رابط التحقق غير صالح.");

            var result = await _authService.VerifyEmailAsync(userId, token);

            ViewBag.Success = result.IsSuccess;
            ViewBag.Message = result.IsSuccess
                ? "تم التحقق من بريدك الإلكتروني بنجاح. يمكنك تسجيل الدخول الآن."
                : result.Errors.FirstOrDefault() ?? "فشل التحقق. الرابط منتهي أو غير صالح.";

            return View();
        }

        // ─────────────────────────────────────────────────────────
        // Login
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return await RedirectByRoleAsync();

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginDto());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _authService.LoginAsync(dto);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User logged in: {Email}", dto.Email);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return await RedirectByRoleAsync(result.Data?.Role);
            }

            if (result.Errors.Any(e => e.Contains("مقفل") || e.Contains("locked")))
            {
                _logger.LogWarning("Locked-out login attempt for: {Email}", dto.Email);
                return RedirectToAction(nameof(Lockout));
            }

            ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault()
                ?? "البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            return View(dto);
        }

        // ─────────────────────────────────────────────────────────
        // Logout
        // ─────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var email = User.Identity?.Name;
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out: {Email}", email);
            return RedirectToAction(nameof(Login));
        }

        // ─────────────────────────────────────────────────────────
        // Forgot / Reset Password
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View(new ForgotPasswordDto());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _authService.ForgotPasswordAsync(dto);

            TempData["Email"] = dto.Email;
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            ViewBag.Email = TempData["Email"]?.ToString() ?? string.Empty;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return BadRequest("رابط الاستعادة غير صالح.");

            return View(new ResetPasswordDto { Token = token, Email = email });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _authService.ResetPasswordAsync(dto);

            if (result.IsSuccess)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return View(dto);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        // ─────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────

        private async Task<IActionResult> RedirectByRoleAsync(string? role = null)
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var userObj = await _userManager.FindByIdAsync(userId);
                if (userObj != null)
                {
                    if (userObj.MustChangePassword)
                    {
                        return RedirectToAction("ForcePasswordChange");
                    }

                    if (await _userManager.IsInRoleAsync(userObj, "HospitalStaff"))
                    {
                        var profile = await _uow.StaffProfiles.Table.FirstOrDefaultAsync(sp => sp.ApplicationUserId == userObj.Id);
                        if (profile != null && !profile.IsInvitationAccepted)
                        {
                            return RedirectToAction("AcceptInvitation", "HospitalQueue", new { token = profile.InvitationToken });
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                if (User.IsInRole("Doctor"))         role = "Doctor";
                else if (User.IsInRole("Admin"))     role = "Admin";
                else if (User.IsInRole("CrisisAdmin")) role = "CrisisAdmin";
                else if (User.IsInRole("HospitalStaff")) role = "HospitalStaff";
                else                                 role = "Patient";
            }

            return role switch
            {
                "Doctor"        => RedirectToAction("Index", "DoctorDashboard"),
                "Admin"         => RedirectToAction("Index", "AdminDashboard"),
                "CrisisAdmin"   => RedirectToAction("Index", "AdminDashboard"),
                "HospitalStaff" => RedirectToAction("Index", "HospitalQueue"),
                _               => RedirectToAction("Index", "PatientDashboard"),
            };
        }
    }
}
