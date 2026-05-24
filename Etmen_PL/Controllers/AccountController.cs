using Etmen_BLL.DTOs.Auth;
using Etmen_BLL.Repositories.IServices;
using Etmen_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Etmen_PL.Controllers
{
    
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _signInManager = signInManager;
            _logger = logger;
        }

       
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToHome();

            return View(new RegisterDto());
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
                _logger.LogInformation("New user registered: {Email}", dto.Email);
                TempData["Email"] = dto.Email;
                return RedirectToAction(nameof(VerifyEmailNotice));
            }

            // Map BLL errors to ModelState
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return View(dto);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmailNotice()
        {
            ViewBag.Email = TempData["Email"]?.ToString() ?? string.Empty;
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


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToHome();

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

                return RedirectToHome();
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

        

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

       
        private IActionResult RedirectToHome() =>
            RedirectToAction("Index", "Home", new { area = "" });
    }
}
