using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IUnitOfWork uow,
            IEmailService emailService,
            IPdfReportService pdfService,
            ILogger<AppointmentService> logger)
        {
            _uow          = uow;
            _emailService = emailService;
            _pdfService   = pdfService;
            _logger       = logger;
        }

        public async Task<ServiceResult<AppointmentDto>> BookAppointmentAsync(string userId, BookingRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<AppointmentDto>.Failure("User ID is required.");

                if (dto == null)
                    return ServiceResult<AppointmentDto>.Failure("Booking request data is required.");

                if (dto.PatientProfileId <= 0)
                    return ServiceResult<AppointmentDto>.Failure("Valid patient profile ID is required.");

                if (dto.DoctorId <= 0)
                    return ServiceResult<AppointmentDto>.Failure("Valid doctor ID is required.");

                if (dto.SlotId <= 0)
                    return ServiceResult<AppointmentDto>.Failure("Valid slot ID is required.");

                if (dto.Date == default)
                    return ServiceResult<AppointmentDto>.Failure("Appointment date is required.");

                // Verify the patient exists and belongs to the user
                var patient = await _uow.PatientProfiles.GetByIdAsync(dto.PatientProfileId);
                if (patient == null)
                    return ServiceResult<AppointmentDto>.Failure("Patient profile not found.");

                // Verify the doctor exists
                var doctor = await _uow.DoctorProfiles.GetByIdAsync(dto.DoctorId);
                if (doctor == null)
                    return ServiceResult<AppointmentDto>.Failure("Doctor profile not found.");

                // Verify the slot exists and is available
                var slot = await _uow.AvailableSlots.GetByIdAsync(dto.SlotId);
                if (slot == null)
                    return ServiceResult<AppointmentDto>.Failure("Appointment slot not found.");

                if (slot.IsBooked)
                    return ServiceResult<AppointmentDto>.Failure("This slot is already booked.");

                if (slot.DoctorProfileId != dto.DoctorId)
                    return ServiceResult<AppointmentDto>.Failure("Slot does not belong to the selected doctor.");

                // Create the appointment from DTO
                var appointment = dto.Adapt<Appointment>();
                appointment.PatientProfileId = dto.PatientProfileId;
                appointment.DoctorProfileId = dto.DoctorId;
                appointment.AppointmentDate = dto.Date;
                appointment.StartTime = dto.StartTime;
                appointment.EndTime = dto.EndTime;
                appointment.Status = AppointmentStatus.Scheduled;
                appointment.CreatedAt = DateTime.UtcNow;

                // Mark the slot as booked
                slot.IsBooked = true;
                _uow.AvailableSlots.Update(slot);

                // Add the appointment
                await _uow.Appointments.AddAsync(appointment);
                await _uow.CompleteAsync();

                var appointmentDto = appointment.Adapt<AppointmentDto>();

                // ── Send booking confirmation emails (fire-and-forget) ──────
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Refresh with navigation props for email
                        var patientName  = patient.FullName   ?? "المريض";
                        var doctorName   = doctor.FullName    ?? "الطبيب";
                        var patientEmail = patient.ApplicationUser?.Email;
                        var doctorEmail  = doctor.ApplicationUser?.Email;
                        var spec         = doctor.Specialization;

                        // Generate appointment PDF
                        var pdfBytes = await _pdfService.GenerateAppointmentPdfAsync(
                            patientName, doctorName, spec,
                            appointment.AppointmentDate, appointment.StartTime, appointment.EndTime,
                            appointment.Notes);

                        // Email patient
                        if (!string.IsNullOrWhiteSpace(patientEmail))
                            await _emailService.SendAppointmentConfirmationEmailAsync(
                                patientEmail, patientName, doctorName, patientName,
                                appointment.AppointmentDate, appointment.StartTime, appointment.EndTime,
                                appointment.Notes, isDoctor: false, pdfBytes);

                        // Email doctor
                        if (!string.IsNullOrWhiteSpace(doctorEmail))
                            await _emailService.SendAppointmentConfirmationEmailAsync(
                                doctorEmail, $"د. {doctorName}", doctorName, patientName,
                                appointment.AppointmentDate, appointment.StartTime, appointment.EndTime,
                                appointment.Notes, isDoctor: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send booking confirmation emails for appointment {Id}", appointment.Id);
                    }
                });

                return ServiceResult<AppointmentDto>.Created(appointmentDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<AppointmentDto>.Failure($"Failed to book appointment: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<AppointmentDto>>> GetPatientAppointmentsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<IEnumerable<AppointmentDto>>.Failure("User ID is required.");

                // Get the patient profile associated with the user
                var user = await _uow.Users.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult<IEnumerable<AppointmentDto>>.Failure("User not found.");

                // Find patient profile for this user
                var patientProfile = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patientProfile == null)
                    return ServiceResult<IEnumerable<AppointmentDto>>.Failure("Patient profile not found for this user.");

                var appointments = await _uow.Appointments.GetByPatientIdAsync(patientProfile.Id);
                var appointmentDtos = appointments.Adapt<List<AppointmentDto>>();

                return ServiceResult<IEnumerable<AppointmentDto>>.Success(appointmentDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<AppointmentDto>>.Failure($"Failed to retrieve patient appointments: {ex.Message}");
            }
        }

        public async Task<ServiceResult<AppointmentDto>> GetAppointmentByIdAsync(string userId, int appointmentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<AppointmentDto>.Failure("User ID is required.");

                if (appointmentId <= 0)
                    return ServiceResult<AppointmentDto>.Failure("Valid appointment ID is required.");

                var appointment = await _uow.Appointments.GetWithDetailsAsync(appointmentId);
                if (appointment == null)
                    return ServiceResult<AppointmentDto>.NotFound("Appointment not found.");

                // Verify the user has access to this appointment
                if (appointment.PatientProfile?.ApplicationUser?.Id != userId)
                    return ServiceResult<AppointmentDto>.Forbidden("You do not have permission to view this appointment.");

                var appointmentDto = appointment.Adapt<AppointmentDto>();
                return ServiceResult<AppointmentDto>.Success(appointmentDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<AppointmentDto>.Failure($"Failed to retrieve appointment: {ex.Message}");
            }
        }

        public async Task<ServiceResult> CancelAppointmentAsync(string userId, int appointmentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult.Failure("User ID is required.");

                if (appointmentId <= 0)
                    return ServiceResult.Failure("Valid appointment ID is required.");

                var appointment = await _uow.Appointments.GetWithDetailsAsync(appointmentId);
                if (appointment == null)
                    return ServiceResult.NotFound("Appointment not found.");

                // Verify the user has access to this appointment
                if (appointment.PatientProfile?.ApplicationUser?.Id != userId)
                    return ServiceResult.Forbidden("You do not have permission to cancel this appointment.");

                // Check if appointment can be cancelled
                if (appointment.Status == AppointmentStatus.Cancelled)
                    return ServiceResult.Failure("This appointment is already cancelled.", 409);

                if (appointment.Status == AppointmentStatus.Completed)
                    return ServiceResult.Failure("Cannot cancel a completed appointment.", 409);

                // Mark appointment as cancelled
                appointment.Status = AppointmentStatus.Cancelled;
                appointment.Notes = "Cancelled by patient";
                appointment.UpdatedAt = DateTime.UtcNow;
                _uow.Appointments.Update(appointment);

                // ── Send cancellation emails (fire-and-forget) ─────────────
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var patientProfile = appointment.PatientProfile;
                        var doctorProfile  = appointment.DoctorProfile;
                        var patientName    = patientProfile?.FullName  ?? "المريض";
                        var doctorName     = doctorProfile?.FullName   ?? "الطبيب";
                        var patientEmail   = patientProfile?.ApplicationUser?.Email;
                        var doctorEmail    = doctorProfile?.ApplicationUser?.Email;

                        if (!string.IsNullOrWhiteSpace(patientEmail))
                            await _emailService.SendAppointmentCancellationEmailAsync(
                                patientEmail, patientName, doctorName, patientName,
                                appointment.AppointmentDate, appointment.StartTime, isDoctor: false);

                        if (!string.IsNullOrWhiteSpace(doctorEmail))
                            await _emailService.SendAppointmentCancellationEmailAsync(
                                doctorEmail, $"د. {doctorName}", doctorName, patientName,
                                appointment.AppointmentDate, appointment.StartTime, isDoctor: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send cancellation emails for appointment {Id}", appointment.Id);
                    }
                });

                // Free up the slot if it exists
                if (appointment.AppointmentDate != default)
                {
                    var slots = await _uow.AvailableSlots.GetAllAsync();
                    var slot = slots.FirstOrDefault(s =>
                        s.DoctorProfileId == appointment.DoctorProfileId &&
                        s.SlotDate == appointment.AppointmentDate &&
                        s.SlotStart == appointment.StartTime &&
                        s.IsBooked);

                    if (slot != null)
                    {
                        slot.IsBooked = false;
                        _uow.AvailableSlots.Update(slot);
                    }
                }

                await _uow.CompleteAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to cancel appointment: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsAsync(int doctorId, DateTime date)
        {
            try
            {
                if (doctorId <= 0)
                    return ServiceResult<IEnumerable<AvailableSlotDto>>.Failure("Valid doctor ID is required.");

                if (date == default)
                    return ServiceResult<IEnumerable<AvailableSlotDto>>.Failure("Valid date is required.");

                // Verify doctor exists
                var doctor = await _uow.DoctorProfiles.GetByIdAsync(doctorId);
                if (doctor == null)
                    return ServiceResult<IEnumerable<AvailableSlotDto>>.Failure("Doctor not found.");

                // Get all slots for the doctor on the specified date
                var slots = await _uow.AvailableSlots.GetAllAsync();
                var availableSlots = slots
                    .Where(s => s.DoctorProfileId == doctorId &&
                                s.SlotDate.Date == date.Date &&
                                !s.IsBooked)
                    .OrderBy(s => s.SlotStart)
                    .ToList();

                var slotDtos = availableSlots.Adapt<List<AvailableSlotDto>>();
                return ServiceResult<IEnumerable<AvailableSlotDto>>.Success(slotDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<AvailableSlotDto>>.Failure($"Failed to retrieve available slots: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<AppointmentDto>>> GetUpcomingAppointmentsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<IEnumerable<AppointmentDto>>.Failure("User ID is required.");

                // Get the patient profile associated with the user
                var user = await _uow.Users.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult<IEnumerable<AppointmentDto>>.Failure("User not found.");

                // Find patient profile for this user
                var patientProfile = await _uow.PatientProfiles.GetByUserIdAsync(userId);
                if (patientProfile == null)
                    return ServiceResult<IEnumerable<AppointmentDto>>.Failure("Patient profile not found for this user.");

                var appointments = await _uow.Appointments.GetUpcomingAppointmentsAsync(patientProfile.Id);
                var appointmentDtos = appointments.Adapt<List<AppointmentDto>>();

                return ServiceResult<IEnumerable<AppointmentDto>>.Success(appointmentDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<AppointmentDto>>.Failure($"Failed to retrieve upcoming appointments: {ex.Message}");
            }
        }
    }
}