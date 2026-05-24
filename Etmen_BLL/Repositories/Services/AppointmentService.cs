using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _uow;

        public AppointmentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<AppointmentDto>> BookAppointmentAsync(string userId, BookingRequestDto dto)
        {
            throw new NotImplementedException("BookAppointmentAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<AppointmentDto>>> GetPatientAppointmentsAsync(string userId)
        {
            throw new NotImplementedException("GetPatientAppointmentsAsync is not implemented yet.");
        }

        public Task<ServiceResult<AppointmentDto>> GetAppointmentByIdAsync(string userId, int appointmentId)
        {
            throw new NotImplementedException("GetAppointmentByIdAsync is not implemented yet.");
        }

        public Task<ServiceResult> CancelAppointmentAsync(string userId, int appointmentId)
        {
            throw new NotImplementedException("CancelAppointmentAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsAsync(int doctorId, DateTime date)
        {
            throw new NotImplementedException("GetAvailableSlotsAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<AppointmentDto>>> GetUpcomingAppointmentsAsync(string userId)
        {
            throw new NotImplementedException("GetUpcomingAppointmentsAsync is not implemented yet.");
        }

    }
}