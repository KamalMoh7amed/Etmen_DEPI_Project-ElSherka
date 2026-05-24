using Etmen_BLL.DTOs.Doctor;
using Etmen_BLL.DTOs.Medical;
using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using MedicalRecordCreateDto = Etmen_BLL.DTOs.Medical.MedicalRecordCreateDto;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class DoctorService : IDoctorService
    {
        private readonly IUnitOfWork _uow;

        public DoctorService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<DoctorProfileDto>> GetProfileAsync(string userId)
        {
            throw new NotImplementedException("GetProfileAsync is not implemented yet.");
        }

        public Task<ServiceResult<DoctorProfileDto>> UpdateProfileAsync(string userId, DoctorProfileDto dto)
        {
            throw new NotImplementedException("UpdateProfileAsync is not implemented yet.");
        }

        public Task<ServiceResult<DoctorDashboardDto>> GetDashboardAsync(string userId)
        {
            throw new NotImplementedException("GetDashboardAsync is not implemented yet.");
        }

        public Task<ServiceResult<DoctorStatisticsDto>> GetStatisticsAsync(string userId)
        {
            throw new NotImplementedException("GetStatisticsAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsAsync(int doctorId)
        {
            throw new NotImplementedException("GetAvailableSlotsAsync is not implemented yet.");
        }

        public Task<ServiceResult<AvailableSlotDto>> AddSlotAsync(string userId, CreateAvailableSlotDto dto)
        {
            throw new NotImplementedException("AddSlotAsync is not implemented yet.");
        }

        public Task<ServiceResult> BulkAddSlotsAsync(string userId, BulkCreateSlotsDto dto)
        {
            throw new NotImplementedException("BulkAddSlotsAsync is not implemented yet.");
        }

        public Task<ServiceResult> DeleteSlotAsync(string userId, int slotId)
        {
            throw new NotImplementedException("DeleteSlotAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<DoctorAppointmentDto>>> GetAppointmentsAsync(string userId)
        {
            throw new NotImplementedException("GetAppointmentsAsync is not implemented yet.");
        }

        public Task<ServiceResult<DoctorAppointmentDto>> GetAppointmentAsync(string userId, int appointmentId)
        {
            throw new NotImplementedException("GetAppointmentAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateAppointmentStatusAsync(string userId, int appointmentId, UpdateAppointmentStatusDto dto)
        {
            throw new NotImplementedException("UpdateAppointmentStatusAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<PatientSearchDto>>> SearchPatientsAsync(string searchTerm)
        {
            throw new NotImplementedException("SearchPatientsAsync is not implemented yet.");
        }

        public Task<ServiceResult<MedicalRecordDto>> AddMedicalRecordForPatientAsync(string doctorUserId, MedicalRecordCreateDto dto)
        {
            throw new NotImplementedException("AddMedicalRecordForPatientAsync is not implemented yet.");
        }

    }
}