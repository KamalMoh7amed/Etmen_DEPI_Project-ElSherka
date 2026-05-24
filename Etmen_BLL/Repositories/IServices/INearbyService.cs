using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    public interface INearbyService
    {
        Task<ServiceResult<List<ProviderDto>>> SearchNearbyProvidersAsync(NearbySearchDto dto);
        Task<ServiceResult<List<AvailableSlotDto>>> GetAvailableSlotsByProviderAsync(int providerId);
        Task<ServiceResult> BookAppointmentAsync(BookingRequestDto dto);
    }
}
