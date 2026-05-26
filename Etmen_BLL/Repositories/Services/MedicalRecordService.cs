using Etmen_BLL.DTOs.Medical;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_Domain.Entities;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class MedicalRecordService : IMedicalRecordService
    {
        private readonly IUnitOfWork _uow;

        public MedicalRecordService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetByPatientAsync(string userId)
        {
            var patientResult = await GetPatientIdAsync(userId);
            if (!patientResult.IsSuccess)
                return ServiceResult<IEnumerable<MedicalRecordDto>>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var records = await _uow.MedicalRecords.GetByPatientIdAsync(patientResult.Data);
            return ServiceResult<IEnumerable<MedicalRecordDto>>.Success(records.Select(Map));
        }

        public async Task<ServiceResult<MedicalRecordDto>> GetByIdAsync(string userId, int recordId)
        {
            var patientResult = await GetPatientIdAsync(userId);
            if (!patientResult.IsSuccess)
                return ServiceResult<MedicalRecordDto>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var record = await _uow.MedicalRecords.GetByIdAsync(recordId);
            if (record is null || record.PatientProfileId != patientResult.Data)
                return ServiceResult<MedicalRecordDto>.NotFound("Medical record was not found.");

            return ServiceResult<MedicalRecordDto>.Success(Map(record));
        }

        public async Task<ServiceResult<MedicalRecordDto>> GetLatestAsync(string userId)
        {
            var patientResult = await GetPatientIdAsync(userId);
            if (!patientResult.IsSuccess)
                return ServiceResult<MedicalRecordDto>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var record = await _uow.MedicalRecords.GetLatestByPatientIdAsync(patientResult.Data);
            return record is null
                ? ServiceResult<MedicalRecordDto>.NotFound("No medical records were found.")
                : ServiceResult<MedicalRecordDto>.Success(Map(record));
        }

        public async Task<ServiceResult<MedicalRecordDto>> CreateAsync(string userId, MedicalRecordCreateDto dto)
        {
            var validationErrors = Validate(dto).ToList();
            if (validationErrors.Count > 0)
                return ServiceResult<MedicalRecordDto>.Failure(validationErrors);

            var patientResult = await ResolveWritablePatientIdAsync(userId, dto.PatientId);
            if (!patientResult.IsSuccess)
                return ServiceResult<MedicalRecordDto>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var record = new MedicalRecord
            {
                PatientProfileId = patientResult.Data,
                RecordDate = NormalizeDate(dto.RecordDate),
                SystolicBP = dto.SystolicBP,
                DiastolicBP = dto.DiastolicBP,
                BloodSugar = dto.BloodSugar,
                HeartRate = dto.HeartRate,
                Temperature = dto.Temperature,
                OxygenSaturation = dto.OxygenSaturation,
                Symptoms = Normalize(dto.Symptoms),
                Notes = BuildClinicalNotes(dto),
                CreatedAt = DateTime.UtcNow
            };

            await _uow.MedicalRecords.AddAsync(record);
            await _uow.CompleteAsync();

            return ServiceResult<MedicalRecordDto>.Created(Map(record));
        }

        public async Task<ServiceResult> DeleteAsync(string userId, int recordId)
        {
            var patientResult = await GetPatientIdAsync(userId);
            if (!patientResult.IsSuccess)
                return ServiceResult.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var record = await _uow.MedicalRecords.GetByIdAsync(recordId);
            if (record is null || record.PatientProfileId != patientResult.Data)
                return ServiceResult.NotFound("Medical record was not found.");

            _uow.MedicalRecords.Remove(record);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
                return ServiceResult<IEnumerable<MedicalRecordDto>>.Failure("Start date must be before or equal to end date.");

            var patientResult = await GetPatientIdAsync(userId);
            if (!patientResult.IsSuccess)
                return ServiceResult<IEnumerable<MedicalRecordDto>>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var records = await _uow.MedicalRecords.GetByDateRangeAsync(patientResult.Data, startDate.Date, endDate.Date.AddDays(1).AddTicks(-1));
            return ServiceResult<IEnumerable<MedicalRecordDto>>.Success(records.Select(Map));
        }

        public async Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetWithAbnormalValuesAsync(string userId)
        {
            var patientResult = await GetPatientIdAsync(userId);
            if (!patientResult.IsSuccess)
                return ServiceResult<IEnumerable<MedicalRecordDto>>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var records = await _uow.MedicalRecords.GetWithAbnormalValuesAsync(patientResult.Data);
            return ServiceResult<IEnumerable<MedicalRecordDto>>.Success(records.Select(Map));
        }

        private async Task<ServiceResult<int>> GetPatientIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult<int>.Unauthorized("User id is required.");

            var patient = await _uow.PatientProfiles.GetByUserIdAsync(userId);
            return patient is null
                ? ServiceResult<int>.NotFound("Patient profile was not found.")
                : ServiceResult<int>.Success(patient.Id);
        }

        private async Task<ServiceResult<int>> ResolveWritablePatientIdAsync(string userId, int requestedPatientId)
        {
            var ownPatient = await GetPatientIdAsync(userId);
            if (!ownPatient.IsSuccess)
                return ownPatient;

            if (requestedPatientId <= 0 || requestedPatientId == ownPatient.Data)
                return ownPatient;

            return ServiceResult<int>.Forbidden("You cannot create records for another patient.");
        }

        private static IEnumerable<string> Validate(MedicalRecordCreateDto dto)
        {
            if (dto is null)
            {
                yield return "Medical record payload is required.";
                yield break;
            }

            if (dto.RecordDate > DateTime.UtcNow.AddMinutes(5))
                yield return "Record date cannot be in the future.";

            if (dto.SystolicBP is < 40 or > 300)
                yield return "Systolic blood pressure is outside the accepted clinical range.";
            if (dto.DiastolicBP is < 30 or > 200)
                yield return "Diastolic blood pressure is outside the accepted clinical range.";
            if (dto.BloodSugar is < 20 or > 1000)
                yield return "Blood sugar is outside the accepted clinical range.";
            if (dto.HeartRate is < 20 or > 250)
                yield return "Heart rate is outside the accepted clinical range.";
            if (dto.Temperature is < 30 or > 45)
                yield return "Temperature is outside the accepted clinical range.";
            if (dto.OxygenSaturation is < 50 or > 100)
                yield return "Oxygen saturation must be between 50 and 100.";
        }

        private static MedicalRecordDto Map(MedicalRecord record) => new()
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

        private static DateTime NormalizeDate(DateTime date)
            => date == default ? DateTime.UtcNow : DateTime.SpecifyKind(date, DateTimeKind.Utc);

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? BuildClinicalNotes(MedicalRecordCreateDto dto)
        {
            var parts = new List<string>();
            Add("Diagnosis", dto.Diagnosis);
            Add("Treatment", dto.Treatment);
            if (dto.PrescribedMedications?.Count > 0)
                Add("Medications", string.Join(", ", dto.PrescribedMedications.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim())));
            Add("Notes", dto.Notes);

            return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);

            void Add(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add($"{label}: {value.Trim()}");
            }
        }
    }
}
