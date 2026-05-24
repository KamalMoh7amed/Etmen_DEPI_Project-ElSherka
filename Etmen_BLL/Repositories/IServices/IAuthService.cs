using Etmen_BLL.DTOs.Auth;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    
    public interface IAuthService
    {
        Task<ServiceResult<AuthResult>> RegisterAsync(RegisterDto dto);

        Task<ServiceResult<AuthResult>> LoginAsync(LoginDto dto);

        Task<ServiceResult> VerifyEmailAsync(string userId, string token);

        Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto dto);

        Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto);

        Task<ServiceResult> DeactivateAccountAsync(string userId);

        Task<bool> IsEmailTakenAsync(string email);
    }
}
