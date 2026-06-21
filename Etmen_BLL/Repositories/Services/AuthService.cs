using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly string _baseUrl;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork uow,
            ILogger<AuthService> logger,
            IEmailService emailService,
            IConfiguration configuration,
            IBackgroundTaskQueue taskQueue)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _uow           = uow;
            _logger        = logger;
            _emailService  = emailService;
            _baseUrl       = configuration["AppSettings:BaseUrl"] ?? "https://localhost:7001";
            _taskQueue    = taskQueue;
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

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Build activation link and send email
            var encodedToken  = Uri.EscapeDataString(token);
            var activationLink = $"{_baseUrl}/Account/VerifyEmail?userId={user.Id}&token={encodedToken}";

            await _taskQueue.QueueBackgroundWorkItemAsync(async token => 
                await _emailService.SendAccountActivationEmailAsync(
                    user.Email!, $"{dto.FirstName} {dto.LastName}".Trim(), activationLink, role));

            _logger.LogInformation(
                "New {Role} registered: {Email}, activation email sent.", role, user.Email);

            return ServiceResult<AuthResult>.Created(new AuthResult
            {
                Success = true,
                Message = "تم إنشاء الحساب بنجاح. يرجى التحقق من بريدك الإلكتروني لتفعيله.",
                UserId  = user.Id,
                Role    = role,
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

            if (result.IsNotAllowed || !user.EmailConfirmed)
            {
                // Generate a new email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Build activation link and send email
                var encodedToken  = Uri.EscapeDataString(token);
                var activationLink = $"{_baseUrl}/Account/VerifyEmail?userId={user.Id}&token={encodedToken}";

                // Find role
                var userRoles = await _userManager.GetRolesAsync(user);
                var role = userRoles.FirstOrDefault() ?? "Patient";

                await _taskQueue.QueueBackgroundWorkItemAsync(async token => 
                    await _emailService.SendAccountActivationEmailAsync(
                        user.Email!, $"{user.FirstName} {user.LastName}".Trim(), activationLink, role));

                _logger.LogInformation(
                    "Resent activation email to unconfirmed user: {Email}", user.Email);

                return ServiceResult<AuthResult>.Failure("الحساب غير مفعل. لقد أرسلنا رابط تفعيل جديد إلى بريدك الإلكتروني، يرجى تفعيله قبل تسجيل الدخول.", 403);
            }

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

            // Send role-specific welcome email after successful verification
            var roles   = await _userManager.GetRolesAsync(user);
            var role    = roles.FirstOrDefault() ?? "Patient";
            var name    = $"{user.FirstName} {user.LastName}".Trim();
            await _taskQueue.QueueBackgroundWorkItemAsync(async token => 
                await _emailService.SendWelcomeEmailAsync(user.Email!, name, role));

            return ServiceResult.Success();
        }


        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // Always return success to prevent email enumeration
            if (user is null || !user.IsEmailVerified)
                return ServiceResult.Success();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            user.ResetPasswordToken       = token;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(2);
            await _userManager.UpdateAsync(user);

            // Build reset link and send email
            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(dto.Email);
            var resetLink    = $"{_baseUrl}/Account/ResetPassword?token={encodedToken}&email={encodedEmail}";
            var name         = $"{user.FirstName} {user.LastName}".Trim();

            await _taskQueue.QueueBackgroundWorkItemAsync(async token => 
                await _emailService.SendPasswordResetEmailAsync(user.Email!, name, resetLink));

            _logger.LogInformation("Password reset email sent to {Email}.", dto.Email);
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

        public async Task<ServiceResult> ResendActivationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return ServiceResult.NotFound("المستخدم غير موجود.");

            if (user.EmailConfirmed)
                return ServiceResult.Failure("البريد الإلكتروني مؤكد بالفعل.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Patient";

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var activationLink = $"{_baseUrl}/Account/VerifyEmail?userId={user.Id}&token={encodedToken}";

            await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
                await _emailService.SendAccountActivationEmailAsync(
                    user.Email!, $"{user.FirstName} {user.LastName}".Trim(), activationLink, role));

            _logger.LogInformation("Resent activation email to: {Email}", user.Email);

            return ServiceResult.Success();
        }

        public async Task<bool> IsEmailTakenAsync(string email) =>
            await _userManager.FindByEmailAsync(email) is not null;
    }
}
