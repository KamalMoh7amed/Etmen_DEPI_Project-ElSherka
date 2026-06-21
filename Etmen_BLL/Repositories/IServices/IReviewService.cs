using Etmen_BLL.DTOs.Review;

namespace Etmen_BLL.Repositories.IServices
{
    public interface IReviewService
    {
        Task<ServiceResult<ReviewDto>> AddReviewAsync(string userId, CreateReviewDto dto);
        Task<ServiceResult<List<ReviewDto>>> GetDoctorReviewsAsync(int doctorId);
        Task<ServiceResult<List<ReviewDto>>> GetProviderReviewsAsync(int providerId);
        Task<ServiceResult<double>> GetDoctorAverageRatingAsync(int doctorId);
        Task<ServiceResult<double>> GetProviderAverageRatingAsync(int providerId);
        Task<ServiceResult<bool>> CanPatientReviewDoctorAsync(string userId, int doctorId);
    }
}
