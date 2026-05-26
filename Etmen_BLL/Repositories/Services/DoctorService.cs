using Etmen_BLL.DTOs.Doctor;
using Etmen_BLL.DTOs.Medical;
using Etmen_BLL.DTOs.Nearby;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.EntityFrameworkCore;
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

        public async Task<ServiceResult<DoctorProfileDto>> GetProfileAsync(string userId)
        {
            var doctor = await _uow.DoctorProfiles.Table
                .Include(d => d.ApplicationUser)
                .FirstOrDefaultAsync(d => d.ApplicationUserId == userId);

            return doctor is null
                ? ServiceResult<DoctorProfileDto>.NotFound("Doctor profile was not found.")
                : ServiceResult<DoctorProfileDto>.Success(MapProfile(doctor));
        }

        public async Task<ServiceResult<DoctorProfileDto>> UpdateProfileAsync(string userId, DoctorProfileDto dto)
        {
            if (dto is null)
                return ServiceResult<DoctorProfileDto>.Failure("Doctor profile payload is required.");

            var errors = ValidateProfile(dto).ToList();
            if (errors.Count > 0)
                return ServiceResult<DoctorProfileDto>.Failure(errors);

            var doctor = await _uow.DoctorProfiles.GetByUserIdAsync(userId);
            if (doctor is null)
                return ServiceResult<DoctorProfileDto>.NotFound("Doctor profile was not found.");

            doctor.FullName = Normalize(dto.FullName);
            doctor.Specialization = Normalize(dto.Specialization);
            doctor.LicenseNumber = Normalize(dto.LicenseNumber);
            doctor.YearsOfExperience = dto.YearsOfExperience;
            doctor.Bio = Normalize(dto.Bio);
            doctor.ConsultationFee = dto.ConsultationFee;
            doctor.IsAvailable = dto.IsAvailable;
            doctor.UpdatedAt = DateTime.UtcNow;

            _uow.DoctorProfiles.Update(doctor);
            await _uow.CompleteAsync();

            var updated = await _uow.DoctorProfiles.Table.Include(d => d.ApplicationUser).FirstAsync(d => d.Id == doctor.Id);
            return ServiceResult<DoctorProfileDto>.Success(MapProfile(updated));
        }

        public async Task<ServiceResult<DoctorDashboardDto>> GetDashboardAsync(string userId)
        {
            var doctorResult = await GetDoctorIdAsync(userId);
            if (!doctorResult.IsSuccess)
                return ServiceResult<DoctorDashboardDto>.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            var doctor = await _uow.DoctorProfiles.GetByIdAsync(doctorResult.Data);
            var appointments = (await _uow.Appointments.GetByDoctorIdAsync(doctorResult.Data)).ToList();
            var today = DateTime.UtcNow.Date;

            var upcoming = appointments
                .Where(a => a.AppointmentDate.Date >= today && a.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .Take(8)
                .Select(MapUpcoming)
                .ToList();

            var recentPatients = appointments
                .GroupBy(a => a.PatientProfileId)
                .Select(g => new RecentPatientDto
                {
                    PatientId = g.Key,
                    PatientName = g.First().PatientProfile?.FullName ?? string.Empty,
                    LastVisitDate = g.Max(a => a.AppointmentDate),
                    LastDiagnosis = g.OrderByDescending(a => a.AppointmentDate).FirstOrDefault()?.Notes,
                    TotalVisits = g.Count()
                })
                .OrderByDescending(p => p.LastVisitDate)
                .Take(8)
                .ToList();

            var dashboard = new DoctorDashboardDto
            {
                DoctorName = doctor?.FullName ?? string.Empty,
                Specialization = doctor?.Specialization,
                TodayAppointmentsCount = appointments.Count(a => a.AppointmentDate.Date == today),
                PendingAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Scheduled),
                TotalPatientsCount = appointments.Select(a => a.PatientProfileId).Distinct().Count(),
                AverageRating = null,
                UpcomingAppointments = upcoming,
                RecentPatients = recentPatients
            };

            return ServiceResult<DoctorDashboardDto>.Success(dashboard);
        }

        public async Task<ServiceResult<DoctorStatisticsDto>> GetStatisticsAsync(string userId)
        {
            var doctorResult = await GetDoctorIdAsync(userId);
            if (!doctorResult.IsSuccess)
                return ServiceResult<DoctorStatisticsDto>.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            var doctor = await _uow.DoctorProfiles.GetByIdAsync(doctorResult.Data);
            var appointments = (await _uow.Appointments.GetByDoctorIdAsync(doctorResult.Data)).ToList();
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var completed = appointments.Count(a => a.Status == AppointmentStatus.Completed);

            var stats = new DoctorStatisticsDto
            {
                TotalAppointments = appointments.Count,
                CompletedAppointments = completed,
                CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                NoShowAppointments = appointments.Count(a => a.Status == AppointmentStatus.NoShow),
                CompletionRate = appointments.Count == 0 ? 0 : Math.Round(completed * 100m / appointments.Count, 2),
                TotalPatients = appointments.Select(a => a.PatientProfileId).Distinct().Count(),
                NewPatientsThisMonth = appointments
                    .Where(a => a.CreatedAt >= monthStart)
                    .Select(a => a.PatientProfileId)
                    .Distinct()
                    .Count(),
                AverageConsultationFee = doctor?.ConsultationFee,
                PeriodStart = appointments.Count == 0 ? monthStart : appointments.Min(a => a.AppointmentDate),
                PeriodEnd = now
            };

            return ServiceResult<DoctorStatisticsDto>.Success(stats);
        }

        public async Task<ServiceResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsAsync(int doctorId)
        {
            if (!await _uow.DoctorProfiles.AnyAsync(d => d.Id == doctorId))
                return ServiceResult<IEnumerable<AvailableSlotDto>>.NotFound("Doctor profile was not found.");

            var slots = await _uow.AvailableSlots.GetAvailableSlotsAsync(doctorId, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddMonths(3));
            return ServiceResult<IEnumerable<AvailableSlotDto>>.Success(slots.Select(MapSlot));
        }

        public async Task<ServiceResult<AvailableSlotDto>> AddSlotAsync(string userId, CreateAvailableSlotDto dto)
        {
            var doctorResult = await GetDoctorIdAsync(userId);
            if (!doctorResult.IsSuccess)
                return ServiceResult<AvailableSlotDto>.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            var errors = ValidateSlot(dto).ToList();
            if (errors.Count > 0)
                return ServiceResult<AvailableSlotDto>.Failure(errors);

            if (await SlotOverlapsAsync(doctorResult.Data, dto.SlotDate.Date, dto.SlotStart, dto.SlotEnd))
                return ServiceResult<AvailableSlotDto>.Conflict("The slot overlaps an existing slot.");

            var slot = new AvailableSlot
            {
                DoctorProfileId = doctorResult.Data,
                SlotDate = dto.SlotDate.Date,
                SlotStart = dto.SlotStart,
                SlotEnd = dto.SlotEnd,
                IsBooked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.AvailableSlots.AddAsync(slot);
            await _uow.CompleteAsync();
            return ServiceResult<AvailableSlotDto>.Created(MapSlot(slot));
        }

        public async Task<ServiceResult> BulkAddSlotsAsync(string userId, BulkCreateSlotsDto dto)
        {
            var doctorResult = await GetDoctorIdAsync(userId);
            if (!doctorResult.IsSuccess)
                return ServiceResult.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            var errors = ValidateBulkSlots(dto).ToList();
            if (errors.Count > 0)
                return ServiceResult.Failure(errors);

            var slots = new List<AvailableSlot>();
            for (var date = dto.StartDate.Date; date <= dto.EndDate.Date; date = date.AddDays(1))
            {
                if (dto.ExcludedDays.Contains(date.DayOfWeek))
                    continue;

                for (var start = dto.DailyStartTime; start.Add(TimeSpan.FromMinutes(dto.SlotDurationMinutes)) <= dto.DailyEndTime; start = start.Add(TimeSpan.FromMinutes(dto.SlotDurationMinutes)))
                {
                    var end = start.Add(TimeSpan.FromMinutes(dto.SlotDurationMinutes));
                    if (await SlotOverlapsAsync(doctorResult.Data, date, start, end))
                        continue;

                    slots.Add(new AvailableSlot
                    {
                        DoctorProfileId = doctorResult.Data,
                        SlotDate = date,
                        SlotStart = start,
                        SlotEnd = end,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (slots.Count == 0)
                return ServiceResult.Conflict("No new slots could be created because they overlap existing availability.");

            await _uow.AvailableSlots.AddRangeAsync(slots);
            await _uow.CompleteAsync();
            return ServiceResult.Success(201);
        }

        public async Task<ServiceResult> DeleteSlotAsync(string userId, int slotId)
        {
            var doctorResult = await GetDoctorIdAsync(userId);
            if (!doctorResult.IsSuccess)
                return ServiceResult.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            var slot = await _uow.AvailableSlots.GetByIdAsync(slotId);
            if (slot is null || slot.DoctorProfileId != doctorResult.Data)
                return ServiceResult.NotFound("Available slot was not found.");
            if (slot.IsBooked)
                return ServiceResult.Conflict("Booked slots cannot be deleted.");

            _uow.AvailableSlots.Remove(slot);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<IEnumerable<DoctorAppointmentDto>>> GetAppointmentsAsync(string userId)
        {
            var doctorResult = await GetDoctorIdAsync(userId);
            if (!doctorResult.IsSuccess)
                return ServiceResult<IEnumerable<DoctorAppointmentDto>>.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            var appointments = await _uow.Appointments.GetByDoctorIdAsync(doctorResult.Data);
            return ServiceResult<IEnumerable<DoctorAppointmentDto>>.Success(appointments.Select(MapAppointment));
        }

        public async Task<ServiceResult<DoctorAppointmentDto>> GetAppointmentAsync(string userId, int appointmentId)
        {
            var doctorResult = await GetDoctorIdAsync(userId);
            if (!doctorResult.IsSuccess)
                return ServiceResult<DoctorAppointmentDto>.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            var appointment = await _uow.Appointments.GetWithDetailsAsync(appointmentId);
            if (appointment is null || appointment.DoctorProfileId != doctorResult.Data)
                return ServiceResult<DoctorAppointmentDto>.NotFound("Appointment was not found.");

            return ServiceResult<DoctorAppointmentDto>.Success(MapAppointment(appointment));
        }

        public async Task<ServiceResult> UpdateAppointmentStatusAsync(string userId, int appointmentId, UpdateAppointmentStatusDto dto)
        {
            var doctorResult = await GetDoctorIdAsync(userId);
            if (!doctorResult.IsSuccess)
                return ServiceResult.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            var appointment = await _uow.Appointments.GetByIdAsync(appointmentId);
            if (appointment is null || appointment.DoctorProfileId != doctorResult.Data)
                return ServiceResult.NotFound("Appointment was not found.");

            if (dto is null || !Enum.TryParse<AppointmentStatus>(dto.Status, true, out var status))
                return ServiceResult.Failure("Appointment status is invalid.");

            appointment.Status = status;
            appointment.Notes = Normalize(dto.Notes) ?? appointment.Notes;
            appointment.UpdatedAt = DateTime.UtcNow;
            _uow.Appointments.Update(appointment);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<IEnumerable<PatientSearchDto>>> SearchPatientsAsync(string searchTerm)
        {
            var term = Normalize(searchTerm);
            if (term is null)
                return ServiceResult<IEnumerable<PatientSearchDto>>.Failure("Search term is required.");

            var patients = await _uow.PatientProfiles.Table
                .Include(p => p.ApplicationUser)
                .Where(p =>
                    (p.FullName != null && p.FullName.Contains(term)) ||
                    (p.ApplicationUser.PhoneNumber != null && p.ApplicationUser.PhoneNumber.Contains(term)) ||
                    (p.ApplicationUser.Email != null && p.ApplicationUser.Email.Contains(term)))
                .OrderBy(p => p.FullName)
                .Take(25)
                .Select(p => new PatientSearchDto
                {
                    SearchTerm = $"{p.Id} | {p.FullName ?? "Unnamed patient"} | {p.ApplicationUser.PhoneNumber ?? p.ApplicationUser.Email}",
                    FilterBy = "Result"
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<PatientSearchDto>>.Success(patients);
        }

        public async Task<ServiceResult<MedicalRecordDto>> AddMedicalRecordForPatientAsync(string doctorUserId, MedicalRecordCreateDto dto)
        {
            var doctorResult = await GetDoctorIdAsync(doctorUserId);
            if (!doctorResult.IsSuccess)
                return ServiceResult<MedicalRecordDto>.Failure(doctorResult.ErrorMessage ?? "Doctor profile not found.", doctorResult.StatusCode);

            if (dto is null || dto.PatientId <= 0)
                return ServiceResult<MedicalRecordDto>.Failure("Patient id is required.");
            if (!await _uow.PatientProfiles.AnyAsync(p => p.Id == dto.PatientId))
                return ServiceResult<MedicalRecordDto>.NotFound("Patient profile was not found.");

            var record = new MedicalRecord
            {
                PatientProfileId = dto.PatientId,
                RecordDate = dto.RecordDate == default ? DateTime.UtcNow : dto.RecordDate,
                SystolicBP = dto.SystolicBP,
                DiastolicBP = dto.DiastolicBP,
                BloodSugar = dto.BloodSugar,
                HeartRate = dto.HeartRate,
                Temperature = dto.Temperature,
                OxygenSaturation = dto.OxygenSaturation,
                Symptoms = Normalize(dto.Symptoms),
                Notes = BuildDoctorRecordNotes(dto, doctorResult.Data),
                CreatedAt = DateTime.UtcNow
            };

            await _uow.MedicalRecords.AddAsync(record);
            await _uow.CompleteAsync();
            return ServiceResult<MedicalRecordDto>.Created(MapMedicalRecord(record));
        }

        private async Task<ServiceResult<int>> GetDoctorIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult<int>.Unauthorized("User id is required.");

            var doctor = await _uow.DoctorProfiles.GetByUserIdAsync(userId);
            return doctor is null
                ? ServiceResult<int>.NotFound("Doctor profile was not found.")
                : ServiceResult<int>.Success(doctor.Id);
        }

        private async Task<bool> SlotOverlapsAsync(int doctorId, DateTime date, TimeSpan start, TimeSpan end)
        {
            var slots = await _uow.AvailableSlots.GetSlotsByDateRangeAsync(doctorId, date, date);
            return slots.Any(s => start < s.SlotEnd && end > s.SlotStart);
        }

        private static IEnumerable<string> ValidateProfile(DoctorProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                yield return "Doctor full name is required.";
            if (dto.YearsOfExperience is < 0 or > 80)
                yield return "Years of experience is outside the accepted range.";
            if (dto.ConsultationFee is < 0)
                yield return "Consultation fee cannot be negative.";
        }

        private static IEnumerable<string> ValidateSlot(CreateAvailableSlotDto dto)
        {
            if (dto is null)
            {
                yield return "Slot payload is required.";
                yield break;
            }

            if (dto.SlotDate.Date < DateTime.UtcNow.Date)
                yield return "Slot date cannot be in the past.";
            if (dto.SlotStart >= dto.SlotEnd)
                yield return "Slot start time must be before end time.";
            if ((dto.SlotEnd - dto.SlotStart).TotalMinutes < 10)
                yield return "Slot duration must be at least 10 minutes.";
        }

        private static IEnumerable<string> ValidateBulkSlots(BulkCreateSlotsDto dto)
        {
            if (dto is null)
            {
                yield return "Bulk slot payload is required.";
                yield break;
            }

            if (dto.StartDate.Date < DateTime.UtcNow.Date)
                yield return "Start date cannot be in the past.";
            if (dto.StartDate.Date > dto.EndDate.Date)
                yield return "Start date must be before or equal to end date.";
            if (dto.DailyStartTime >= dto.DailyEndTime)
                yield return "Daily start time must be before end time.";
            if (dto.SlotDurationMinutes < 10 || dto.SlotDurationMinutes > 240)
                yield return "Slot duration must be between 10 and 240 minutes.";
        }

        private static DoctorProfileDto MapProfile(DoctorProfile doctor) => new()
        {
            Id = doctor.Id,
            FullName = doctor.FullName ?? string.Empty,
            Specialization = doctor.Specialization,
            LicenseNumber = doctor.LicenseNumber,
            YearsOfExperience = doctor.YearsOfExperience,
            Bio = doctor.Bio,
            ConsultationFee = doctor.ConsultationFee,
            IsAvailable = doctor.IsAvailable,
            Email = doctor.ApplicationUser?.Email,
            PhoneNumber = doctor.ApplicationUser?.PhoneNumber,
            CreatedAt = doctor.CreatedAt,
            UpdatedAt = doctor.UpdatedAt
        };

        private static AvailableSlotDto MapSlot(AvailableSlot slot) => new()
        {
            Id = slot.Id,
            DoctorId = slot.DoctorProfileId,
            Date = slot.SlotDate,
            StartTime = slot.SlotStart,
            EndTime = slot.SlotEnd,
            IsBooked = slot.IsBooked
        };

        private static DoctorAppointmentDto MapAppointment(Appointment appointment) => new()
        {
            Id = appointment.Id,
            PatientId = appointment.PatientProfileId,
            PatientName = appointment.PatientProfile?.FullName ?? string.Empty,
            PatientPhone = appointment.PatientProfile?.ApplicationUser?.PhoneNumber,
            PatientEmail = appointment.PatientProfile?.ApplicationUser?.Email,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status.ToString(),
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt
        };

        private static UpcomingAppointmentDto MapUpcoming(Appointment appointment) => new()
        {
            Id = appointment.Id,
            PatientName = appointment.PatientProfile?.FullName ?? string.Empty,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status.ToString(),
            Notes = appointment.Notes
        };

        private static MedicalRecordDto MapMedicalRecord(MedicalRecord record) => new()
        {
            Id = record.Id,
            RecordDate = record.RecordDate,
            SystolicBP = record.SystolicBP,
            DiastolicBP = record.DiastolicBP,
            BloodSugar = record.BloodSugar,
            HeartRate = record.HeartRate,
            Temperature = record.Temperature,
            OxygenSaturation = record.OxygenSaturation,
            Symptoms = record.Symptoms,
            Notes = record.Notes
        };

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? BuildDoctorRecordNotes(MedicalRecordCreateDto dto, int doctorId)
        {
            var parts = new List<string> { $"RecordedByDoctorId: {doctorId}" };
            Add("Diagnosis", dto.Diagnosis);
            Add("Treatment", dto.Treatment);
            if (dto.PrescribedMedications?.Count > 0)
                Add("Medications", string.Join(", ", dto.PrescribedMedications.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim())));
            Add("Notes", dto.Notes);
            return string.Join(Environment.NewLine, parts);

            void Add(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add($"{label}: {value.Trim()}");
            }
        }
    }
}
