

namespace Etmen_BLL.Repositories.IServices
{
    
    public interface IDoctorService
    {
        // ── Profile ───────────────────────────────────────────────────────────────

        Task<ServiceResult<DoctorProfileDto>> GetProfileAsync(string userId);

        Task<ServiceResult<DoctorProfileDto>> UpdateProfileAsync(string userId, DoctorProfileDto dto);

        // ── Dashboard ─────────────────────────────────────────────────────────────

        Task<ServiceResult<DoctorDashboardDto>> GetDashboardAsync(string userId);

        Task<ServiceResult<DoctorStatisticsDto>> GetStatisticsAsync(string userId);

        // ── Availability Slots ────────────────────────────────────────────────────

        Task<ServiceResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsAsync(int doctorId);

        Task<ServiceResult<AvailableSlotDto>> AddSlotAsync(string userId, CreateAvailableSlotDto dto);

        Task<ServiceResult> BulkAddSlotsAsync(string userId, BulkCreateSlotsDto dto);

        Task<ServiceResult> DeleteSlotAsync(string userId, int slotId);

        // ── Appointments (doctor view) ────────────────────────────────────────────

        Task<ServiceResult<IEnumerable<DoctorAppointmentDto>>> GetAppointmentsAsync(string userId);

        Task<ServiceResult<DoctorAppointmentDto>> GetAppointmentAsync(string userId, int appointmentId);

        Task<ServiceResult> UpdateAppointmentStatusAsync(string userId, int appointmentId, UpdateAppointmentStatusDto dto);

        // ── Patient Records ───────────────────────────────────────────────────────

        Task<ServiceResult<IEnumerable<PatientSearchDto>>> SearchPatientsAsync(string searchTerm);

        Task<ServiceResult<MedicalRecordDto>> AddMedicalRecordForPatientAsync(string doctorUserId, MedicalRecordCreateDto dto);
    }
}
