namespace Etmen_BLL.Repositories.IServices
{
        public interface IAppointmentService
    {
        Task<ServiceResult<AppointmentDto>> BookAppointmentAsync(string userId, BookingRequestDto dto);

        Task<ServiceResult<IEnumerable<AppointmentDto>>> GetPatientAppointmentsAsync(string userId);

        Task<ServiceResult<AppointmentDto>> GetAppointmentByIdAsync(string userId, int appointmentId);

        Task<ServiceResult> CancelAppointmentAsync(string userId, int appointmentId);

        Task<ServiceResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsAsync(int doctorId, DateTime date);

        Task<ServiceResult<IEnumerable<AppointmentDto>>> GetUpcomingAppointmentsAsync(string userId);
    }
}
