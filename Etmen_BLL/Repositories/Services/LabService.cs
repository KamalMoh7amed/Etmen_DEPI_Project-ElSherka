using Etmen_BLL.DTOs.Lab;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Net.Http;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class LabService : ILabService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;
        private readonly ILogger<LabService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public LabService(
            IUnitOfWork uow,
            IEmailService emailService,
            IPdfReportService pdfService,
            ILogger<LabService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _uow          = uow;
            _emailService = emailService;
            _pdfService   = pdfService;
            _logger       = logger;
            _serviceProvider = serviceProvider;
            _configuration   = configuration;
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

            // ── Send lab result email with PDF (fire-and-forget) ──────────
            _ = Task.Run(async () =>
            {
                try
                {
                    string? ocrData = null;
                    string? resultsSummary = null;

                    if (dto.UseOcr && !string.IsNullOrEmpty(lab.FilePath))
                    {
                        try
                        {
                            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", lab.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(physicalPath))
                            {
                                var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
                                var mimeType = ext switch
                                {
                                    ".pdf" => "application/pdf",
                                    ".jpg" or ".jpeg" => "image/jpeg",
                                    ".png" => "image/png",
                                    ".gif" => "image/gif",
                                    ".bmp" => "image/bmp",
                                    _ => "application/octet-stream"
                                };

                                var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
                                var base64Data = Convert.ToBase64String(fileBytes);

                                using var scope = _serviceProvider.CreateScope();
                                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

                                var apiKey = _configuration["ChatbotApi:ApiKey"];
                                var endpoint = _configuration["ChatbotApi:Endpoint"];

                                if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(endpoint))
                                {
                                    var requestBody = new
                                    {
                                        contents = new[]
                                        {
                                            new
                                            {
                                                parts = new object[]
                                                {
                                                    new
                                                    {
                                                        inlineData = new
                                                        {
                                                            mimeType = mimeType,
                                                            data = base64Data
                                                        }
                                                    },
                                                    new
                                                    {
                                                        text = "قم بتحليل تقرير المختبر الطبي المرفق بدقة. استخرج كافة أسماء الفحوصات الطبية، قيمها المقاسة، وحداتها، ومداها الطبيعي. حدد أي نتائج غير طبيعية (خارج المدى الطبيعي) بشكل واضح بكتابة (مرتفع) أو (منخفض) أو (غير طبيعي). صِغ التقرير النهائي باللغة العربية بشكل منسق ومنظم جداً للمريض."
                                                    }
                                                }
                                            }
                                        },
                                        generationConfig = new
                                        {
                                            temperature = 0.2,
                                            maxOutputTokens = 2000
                                        }
                                    };

                                    var url = $"{endpoint}?key={apiKey}";
                                    var requestJson = JsonSerializer.Serialize(requestBody);

                                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                                    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                                    using var response = await httpClient.SendAsync(request);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var responseText = await response.Content.ReadAsStringAsync();
                                        using var doc = JsonDocument.Parse(responseText);
                                        var root = doc.RootElement;
                                        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                                        {
                                            var firstCandidate = candidates[0];
                                            if (firstCandidate.TryGetProperty("content", out var content) &&
                                                content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                                            {
                                                var extractedText = parts[0].GetProperty("text").GetString();
                                                if (!string.IsNullOrEmpty(extractedText))
                                                {
                                                    ocrData = extractedText;
                                                    resultsSummary = ocrData.Length > 990 ? ocrData.Substring(0, 990) + "..." : ocrData;

                                                    // Update database record inside scope
                                                    var dbLab = await scopedUow.LabResults.GetByIdAsync(lab.Id);
                                                    if (dbLab != null)
                                                    {
                                                        dbLab.OcrExtractedData = ocrData;
                                                        dbLab.Results = resultsSummary;
                                                        scopedUow.LabResults.Update(dbLab);
                                                        await scopedUow.CompleteAsync();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var errorContent = await response.Content.ReadAsStringAsync();
                                        _logger.LogError("Gemini OCR API returned error {StatusCode}: {Error}", response.StatusCode, errorContent);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error occurred during Gemini OCR extraction for lab {Id}", lab.Id);
                        }

                        // If OCR failed or didn't populate, update status to fail
                        if (string.IsNullOrEmpty(ocrData))
                        {
                            try
                            {
                                using var scope = _serviceProvider.CreateScope();
                                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                                var dbLab = await scopedUow.LabResults.GetByIdAsync(lab.Id);
                                if (dbLab != null)
                                {
                                    dbLab.OcrExtractedData = "فشل استخراج البيانات الطبية بالذكاء الاصطناعي. يرجى التأكد من وضوح الصورة المرفوعة.";
                                    scopedUow.LabResults.Update(dbLab);
                                    await scopedUow.CompleteAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to update fallback OCR status for lab {Id}", lab.Id);
                            }
                        }
                    }

                    // Send lab result email with updated data
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var patient = await scopedUow.PatientProfiles.GetByIdAsync(lab.PatientProfileId);
                        var patientEmail = patient?.ApplicationUser?.Email;
                        var patientName = patient?.FullName ?? "المريض";

                        if (!string.IsNullOrWhiteSpace(patientEmail))
                        {
                            // Fetch the latest updated lab values from database to ensure we send the final PDF
                            var dbLab = await scopedUow.LabResults.GetByIdAsync(lab.Id);
                            if (dbLab != null)
                            {
                                var pdfBytes = await _pdfService.GenerateLabReportPdfAsync(
                                    patientName, dbLab.TestName, dbLab.TestDate,
                                    dbLab.Results, dbLab.OcrExtractedData);

                                await _emailService.SendLabResultEmailAsync(
                                    patientEmail, patientName,
                                    dbLab.TestName, dbLab.TestDate, pdfBytes);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to complete background lab processing and email for lab {Id}", lab.Id);
                }
            });

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
            PatientId = lab.PatientProfileId,
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
            return new[] { 
                "abnormal", "positive", "critical", "high", "low", "elevated", "detected",
                "مرتفع", "منخفض", "حرج", "غير طبيعي", "إيجابي", "غير طبيعية"
            }
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
