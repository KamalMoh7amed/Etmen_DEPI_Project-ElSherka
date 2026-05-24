using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class NearbyService : INearbyService
    {
        private readonly IUnitOfWork _uow;

        public NearbyService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<List<ProviderDto>>> SearchNearbyProvidersAsync(NearbySearchDto dto)
        {
            throw new NotImplementedException("SearchNearbyProvidersAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<AvailableSlotDto>>> GetAvailableSlotsByProviderAsync(int providerId)
        {
            throw new NotImplementedException("GetAvailableSlotsByProviderAsync is not implemented yet.");
        }

        public Task<ServiceResult> BookAppointmentAsync(BookingRequestDto dto)
        {
            throw new NotImplementedException("BookAppointmentAsync is not implemented yet.");
        }

    }
}