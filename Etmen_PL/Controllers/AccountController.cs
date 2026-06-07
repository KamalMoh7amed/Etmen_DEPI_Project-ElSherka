using Etmen_BLL.DTOs.Auth;
using Etmen_BLL.Repositories.IServices;
using Etmen_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Etmen_PL.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _authService  = authService;
            _signInManager = signInManager;
            _userManager   = userManager;
            _logger        = logger;
        }

        // ─────────────────────────────────────────────────────────
        // STEP 1 — Select Role (entry point for new users)
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SelectRole()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

            return View();
        }

        // ─────────────────────────────────────────────────────────
        // STEP 2 — Register (with role pre-selected)
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string role = "Patient")
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

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
                TempData["Email"] = dto.Email;
                TempData["RegisteredRole"] = dto.Role;
                return RedirectToAction(nameof(VerifyEmailNotice));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return View(dto);
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
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

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

                // Redirect based on role from AuthResult
                return RedirectByRole(result.Data?.Role);
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
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return BadRequest("رابط إعادة التعيين غير صالح.");

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

        // ─────────────────────────────────────────────────────────
        // Misc
        // ─────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        // ─────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Redirects to the correct dashboard based on role.
        /// If role is null, reads it from the currently signed-in user's claims.
        /// </summary>
        private IActionResult RedirectByRole(string? role = null)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                // Determine from current claims
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
