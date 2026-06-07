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
            _userManager   = userManager;
            _signInManager = signInManager;
            _uow           = uow;
            _logger        = logger;
        }


        public async Task<ServiceResult<AuthResult>> RegisterAsync(RegisterDto dto)
        {
            // Guard: duplicate e-mail
            if (await IsEmailTakenAsync(dto.Email))
                return ServiceResult<AuthResult>.Conflict("البريد الإلكتروني مستخدم بالفعل.");

            // Validate role — only Patient or Doctor allowed through self-registration
            var allowedRoles = new[] { "Patient", "Doctor" };
            var role = allowedRoles.Contains(dto.Role) ? dto.Role : "Patient";

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

            // Assign role
            await _userManager.AddToRoleAsync(user, role);

            // Create the matching profile
            if (role == "Doctor")
            {
                var doctorProfile = new DoctorProfile
                {
                    ApplicationUserId = user.Id,
                    FullName          = $"{dto.FirstName} {dto.LastName}".Trim(),
                    CreatedAt         = DateTime.UtcNow,
                };
                await _uow.DoctorProfiles.AddAsync(doctorProfile);
            }
            else // Patient
            {
                var patientProfile = new PatientProfile
                {
                    ApplicationUserId = user.Id,
                    FullName          = $"{dto.FirstName} {dto.LastName}".Trim(),
                    CreatedAt         = DateTime.UtcNow,
                };
                await _uow.PatientProfiles.AddAsync(patientProfile);
            }

            await _uow.CompleteAsync();

            // For Patients: Auto-confirm email to allow immediate login
            // For Doctors: Require email verification for security
            if (role == "Patient")
            {
                user.IsEmailVerified = true;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("New Patient registered and auto-verified: {Email}", user.Email);

                return ServiceResult<AuthResult>.Created(new AuthResult
                {
                    Success = true,
                    Message = "تم إنشاء الحساب بنجاح. يمكنك الآن تسجيل الدخول.",
                    UserId  = user.Id,
                    Role    = role,
                });
            }
            else // Doctor
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                _logger.LogInformation(
                    "New Doctor registered: {Email}, verification token generated.", user.Email);

                return ServiceResult<AuthResult>.Created(new AuthResult
                {
                    Success = true,
                    Message = "تم إنشاء الحساب بنجاح. يرجى التحقق من بريدك الإلكتروني.",
                    UserId  = user.Id,
                    Role    = role,
                });
            }
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

            // Fetch the user's primary role for redirect purposes
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Patient";

            // Create the persistent authentication cookie/session
            await _signInManager.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("User {Email} signed in successfully as {Role}.", user.Email, primaryRole);

            return ServiceResult<AuthResult>.Success(new AuthResult
            {
                Success = true,
                Message = "تم تسجيل الدخول بنجاح.",
                UserId  = user.Id,
                Role    = primaryRole,
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

            if (string.IsNullOrWhiteSpace(dto.Token))
                return ServiceResult.Failure("رمز إعادة التعيين مطلوب.");

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
