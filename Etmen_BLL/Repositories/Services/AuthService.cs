using Etmen_BLL.DTOs.Auth;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Etmen_BLL.Repositories.Services
{
    
    public sealed class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork uow,
            ILogger<AuthService> logger)
        {
            _userManager    = userManager;
            _signInManager  = signInManager;
            _uow            = uow;
            _logger         = logger;
        }


        public async Task<ServiceResult<AuthResult>> RegisterAsync(RegisterDto dto)
        {
            // Guard: duplicate e-mail
            if (await IsEmailTakenAsync(dto.Email))
                return ServiceResult<AuthResult>.Conflict("البريد الإلكتروني مستخدم بالفعل.");

            var user = new ApplicationUser
            {
                UserName    = dto.Email,
                Email       = dto.Email,
                FirstName   = dto.FirstName,
                LastName    = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt   = DateTime.UtcNow,
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
                return ServiceResult<AuthResult>.Failure(createResult.Errors.Select(e => e.Description));

            var profile = new PatientProfile
            {
                ApplicationUserId = user.Id,
                FullName          = $"{dto.FirstName} {dto.LastName}".Trim(),
                CreatedAt         = DateTime.UtcNow,
            };

            await _uow.PatientProfiles.AddAsync(profile);
            await _uow.CompleteAsync();

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            _logger.LogInformation("New user registered: {Email}, verification token generated.", user.Email);


            return ServiceResult<AuthResult>.Created(new AuthResult
            {
                Success = true,
                Message = "تم إنشاء الحساب بنجاح. يرجى التحقق من بريدك الإلكتروني.",
                UserId  = user.Id,
            });
        }


        public async Task<ServiceResult<AuthResult>> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return ServiceResult<AuthResult>.Failure("بريد إلكتروني أو كلمة مرور غير صحيحة.", 401);

            if (!user.IsActive)
                return ServiceResult<AuthResult>.Forbidden("هذا الحساب معطّل. يرجى التواصل مع الدعم.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return ServiceResult<AuthResult>.Failure("الحساب مقفل مؤقتاً بسبب محاولات تسجيل دخول متعددة.", 429);

            if (!result.Succeeded)
                return ServiceResult<AuthResult>.Failure("بريد إلكتروني أو كلمة مرور غير صحيحة.", 401);

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} signed in successfully.", user.Email);

         
            return ServiceResult<AuthResult>.Success(new AuthResult
            {
                Success = true,
                Message = "تم تسجيل الدخول بنجاح.",
                UserId  = user.Id,
            });
        }

     
        public async Task<ServiceResult> VerifyEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return ServiceResult.NotFound("المستخدم غير موجود.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return ServiceResult.Failure(result.Errors.Select(e => e.Description));

            user.IsEmailVerified = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Email verified for user {UserId}.", userId);
            return ServiceResult.Success();
        }


        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user is null || !user.IsEmailVerified)
                return ServiceResult.Success();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            user.ResetPasswordToken       = token;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(2);
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Password reset token generated for {Email}.", dto.Email);


            return ServiceResult.Success();
        }


        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null) return ServiceResult.NotFound();

            if (user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                return ServiceResult.Failure("انتهت صلاحية رابط إعادة تعيين كلمة المرور.");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.Password);
            if (!result.Succeeded)
                return ServiceResult.Failure(result.Errors.Select(e => e.Description));

            user.ResetPasswordToken       = null;
            user.ResetPasswordTokenExpiry = null;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Password reset successful for {Email}.", dto.Email);
            return ServiceResult.Success();
        }


        public async Task<ServiceResult> DeactivateAccountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return ServiceResult.NotFound();

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.Errors.Select(e => e.Description));
        }


        public async Task<bool> IsEmailTakenAsync(string email) =>
            await _userManager.FindByEmailAsync(email) is not null;
    }
}
