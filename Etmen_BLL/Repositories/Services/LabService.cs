using Etmen_BLL.DTOs.Lab;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class LabService : ILabService
    {
        private readonly IUnitOfWork _uow;

        public LabService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<LabResultDto>> GetLabResultByIdAsync(int labResultId)
        {
            var lab = await _uow.LabResults.GetByIdAsync(labResultId);
            return lab is null
                ? ServiceResult<LabResultDto>.NotFound("Lab result was not found.")
                : ServiceResult<LabResultDto>.Success(Map(lab));
        }

        public async Task<ServiceResult<List<LabResultDto>>> GetPatientLabResultsAsync(int patientId)
        {
            var patientResult = await EnsurePatientExistsAsync(patientId);
            if (!patientResult.IsSuccess)
                return ServiceResult<List<LabResultDto>>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var labs = await _uow.LabResults.GetByPatientIdAsync(patientId);
            return ServiceResult<List<LabResultDto>>.Success(labs.Select(Map).ToList());
        }

        public async Task<ServiceResult<List<LabResultDto>>> GetLabResultsByDateRangeAsync(int patientId, DateTime startDate, DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
                return ServiceResult<List<LabResultDto>>.Failure("Start date must be before or equal to end date.");

            var patientResult = await EnsurePatientExistsAsync(patientId);
            if (!patientResult.IsSuccess)
                return ServiceResult<List<LabResultDto>>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var labs = await _uow.LabResults.GetByDateRangeAsync(patientId, startDate.Date, endDate.Date.AddDays(1).AddTicks(-1));
            return ServiceResult<List<LabResultDto>>.Success(labs.Select(Map).ToList());
        }

        public async Task<ServiceResult<LabResultDto>> UploadLabResultAsync(LabUploadDto dto)
        {
            var validationErrors = await ValidateUploadAsync(dto);
            if (validationErrors.Count > 0)
                return ServiceResult<LabResultDto>.Failure(validationErrors);

            var lab = new LabResult
            {
                PatientProfileId = dto.PatientId,
                TestName = dto.TestName.Trim(),
                TestDate = NormalizeDate(dto.TestDate),
                FilePath = Normalize(dto.FilePath),
                FileUrl = BuildFileUrl(dto.FilePath),
                OcrExtractedData = dto.UseOcr ? "OCR processing pending." : null,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.LabResults.AddAsync(lab);
            await _uow.CompleteAsync();

            return ServiceResult<LabResultDto>.Created(Map(lab));
        }

        public async Task<ServiceResult> UpdateLabResultAsync(int labResultId, LabUploadDto dto)
        {
            var lab = await _uow.LabResults.GetByIdAsync(labResultId);
            if (lab is null)
                return ServiceResult.NotFound("Lab result was not found.");

            if (dto is null)
                return ServiceResult.Failure("Lab result payload is required.");

            if (string.IsNullOrWhiteSpace(dto.TestName))
                return ServiceResult.Failure("Test name is required.");

            if (dto.TestDate > DateTime.UtcNow.AddDays(1))
                return ServiceResult.Failure("Test date cannot be in the future.");

            if (dto.PatientId > 0 && dto.PatientId != lab.PatientProfileId)
            {
                var patientResult = await EnsurePatientExistsAsync(dto.PatientId);
                if (!patientResult.IsSuccess)
                    return ServiceResult.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

                lab.PatientProfileId = dto.PatientId;
            }

            lab.TestName = dto.TestName.Trim();
            lab.TestDate = NormalizeDate(dto.TestDate);
            lab.FilePath = Normalize(dto.FilePath);
            lab.FileUrl = BuildFileUrl(dto.FilePath);
            if (dto.UseOcr && string.IsNullOrWhiteSpace(lab.OcrExtractedData))
                lab.OcrExtractedData = "OCR processing pending.";

            _uow.LabResults.Update(lab);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteLabResultAsync(int labResultId)
        {
            var lab = await _uow.LabResults.GetByIdAsync(labResultId);
            if (lab is null)
                return ServiceResult.NotFound("Lab result was not found.");

            _uow.LabResults.Remove(lab);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<Dictionary<string, object>>> AnalyzeLabResultsAsync(int patientId)
        {
            var patientResult = await EnsurePatientExistsAsync(patientId);
            if (!patientResult.IsSuccess)
                return ServiceResult<Dictionary<string, object>>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var labs = (await _uow.LabResults.GetByPatientIdAsync(patientId)).ToList();
            var abnormal = labs.Where(IsAbnormal).ToList();
            var groupedByTest = labs
                .GroupBy(l => l.TestName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (object)new
                {
                    Count = g.Count(),
                    LatestDate = g.Max(x => x.TestDate),
                    AbnormalCount = g.Count(IsAbnormal)
                });

            var analysis = new Dictionary<string, object>
            {
                ["totalResults"] = labs.Count,
                ["abnormalResults"] = abnormal.Count,
                ["normalResults"] = labs.Count - abnormal.Count,
                ["ocrProcessedResults"] = labs.Count(l => !string.IsNullOrWhiteSpace(l.OcrExtractedData)),
                ["latestTestDate"] = labs.Count == 0 ? null! : labs.Max(l => l.TestDate),
                ["tests"] = groupedByTest
            };

            return ServiceResult<Dictionary<string, object>>.Success(analysis);
        }

        public async Task<ServiceResult<List<LabResultDto>>> GetAbnormalResultsAsync(int patientId)
        {
            var patientResult = await EnsurePatientExistsAsync(patientId);
            if (!patientResult.IsSuccess)
                return ServiceResult<List<LabResultDto>>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            var labs = await _uow.LabResults.GetByPatientIdAsync(patientId);
            return ServiceResult<List<LabResultDto>>.Success(labs.Where(IsAbnormal).Select(Map).ToList());
        }

        public async Task<ServiceResult<List<LabResultDto>>> SearchLabResultsAsync(string testName, int pageNumber = 1, int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var term = Normalize(testName);

            var query = _uow.LabResults.Table;
            if (term is not null)
                query = query.Where(l => l.TestName.Contains(term) || (l.Results != null && l.Results.Contains(term)));

            var labs = await query
                .OrderByDescending(l => l.TestDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ServiceResult<List<LabResultDto>>.Success(labs.Select(Map).ToList());
        }

        public async Task<ServiceResult<Dictionary<string, object>>> GetLabStatisticsAsync()
        {
            var labs = await _uow.LabResults.Table.ToListAsync();
            var now = DateTime.UtcNow;

            var stats = new Dictionary<string, object>
            {
                ["totalResults"] = labs.Count,
                ["resultsThisMonth"] = labs.Count(l => l.TestDate.Year == now.Year && l.TestDate.Month == now.Month),
                ["patientsWithResults"] = labs.Select(l => l.PatientProfileId).Distinct().Count(),
                ["abnormalResults"] = labs.Count(IsAbnormal),
                ["ocrProcessedResults"] = labs.Count(l => !string.IsNullOrWhiteSpace(l.OcrExtractedData)),
                ["topTests"] = labs.GroupBy(l => l.TestName)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => (object)g.Count())
            };

            return ServiceResult<Dictionary<string, object>>.Success(stats);
        }
        
        public async Task<ServiceResult> VerifyLabResultAsync(int labResultId)
        {
            var lab = await _uow.LabResults.GetByIdAsync(labResultId);
            if (lab is null)
                return ServiceResult.NotFound("Lab result was not found.");

            lab.Results = AppendAuditNote(lab.Results, "Verified");
            _uow.LabResults.Update(lab);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RejectLabResultAsync(int labResultId, string reason)
        {
            var lab = await _uow.LabResults.GetByIdAsync(labResultId);
            if (lab is null)
                return ServiceResult.NotFound("Lab result was not found.");

            if (string.IsNullOrWhiteSpace(reason))
                return ServiceResult.Failure("Rejection reason is required.");

            lab.Results = AppendAuditNote(lab.Results, $"Rejected: {reason.Trim()}");
            _uow.LabResults.Update(lab);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        private async Task<List<string>> ValidateUploadAsync(LabUploadDto dto)
        {
            var errors = new List<string>();
            if (dto is null)
            {
                errors.Add("Lab result payload is required.");
                return errors;
            }

            if (dto.PatientId <= 0)
                errors.Add("Patient id is required.");
            else if (!await _uow.PatientProfiles.AnyAsync(p => p.Id == dto.PatientId))
                errors.Add("Patient profile was not found.");

            if (string.IsNullOrWhiteSpace(dto.TestName))
                errors.Add("Test name is required.");
            if (dto.TestDate > DateTime.UtcNow.AddDays(1))
                errors.Add("Test date cannot be in the future.");

            return errors;
        }

        private async Task<ServiceResult> EnsurePatientExistsAsync(int patientId)
        {
            if (patientId <= 0)
                return ServiceResult.Failure("Patient id is required.");

            return await _uow.PatientProfiles.AnyAsync(p => p.Id == patientId)
                ? ServiceResult.Success()
                : ServiceResult.NotFound("Patient profile was not found.");
        }

        private static LabResultDto Map(LabResult lab) => new()
        {
            Id = lab.Id,
            TestName = lab.TestName,
            TestDate = lab.TestDate,
            FilePath = lab.FilePath,
            FileUrl = lab.FileUrl,
            OcrExtractedData = lab.OcrExtractedData,
            Results = lab.Results,
            CreatedAt = lab.CreatedAt
        };

        private static bool IsAbnormal(LabResult lab)
        {
            var text = $"{lab.Results} {lab.OcrExtractedData}".ToLowerInvariant();
            return new[] { "abnormal", "positive", "critical", "high", "low", "elevated", "detected" }
                .Any(token => text.Contains(token));
        }

        private static string? BuildFileUrl(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var normalized = filePath.Replace('\\', '/').Trim();
            return normalized.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : "/" + normalized.TrimStart('/');
        }

        private static DateTime NormalizeDate(DateTime date)
            => date == default ? DateTime.UtcNow : DateTime.SpecifyKind(date, DateTimeKind.Utc);

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string AppendAuditNote(string? current, string note)
        {
            var auditNote = $"[{DateTime.UtcNow:O}] {note}";
            return string.IsNullOrWhiteSpace(current)
                ? auditNote
                : $"{current.Trim()}{Environment.NewLine}{auditNote}";
        }
    }
}
