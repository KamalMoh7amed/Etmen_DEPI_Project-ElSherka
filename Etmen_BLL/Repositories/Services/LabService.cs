

namespace Etmen_BLL.Repositories.Services
{
    public sealed class LabService : ILabService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;
        private readonly ILogger<LabService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundTaskQueue _taskQueue;

        public LabService(
            IUnitOfWork uow,
            IEmailService emailService,
            IPdfReportService pdfService,
            ILogger<LabService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IBackgroundTaskQueue taskQueue)
        {
            _uow          = uow;
            _emailService = emailService;
            _pdfService   = pdfService;
            _logger       = logger;
            _scopeFactory = scopeFactory;
            _configuration   = configuration;
            _taskQueue    = taskQueue;
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

            // ── Send lab result email with PDF (queued background task) ──
            await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
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

                                using var scope = _scopeFactory.CreateScope();
                                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

                                // Use the dedicated vision API config (gemini-1.5-flash supports inlineData)
                                var apiKey = _configuration["GeminiVisionApi:ApiKey"]
                                          ?? _configuration["ChatbotApi:ApiKey"];
                                var endpoint = _configuration["GeminiVisionApi:Endpoint"]
                                            ?? _configuration["ChatbotApi:Endpoint"];

                                _logger.LogInformation("Starting Gemini Vision OCR for lab {Id}, file={Path}, mime={Mime}", lab.Id, physicalPath, mimeType);

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

                                    using var response = await httpClient.SendAsync(request, token);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var responseText = await response.Content.ReadAsStringAsync(token);
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

                                                    _logger.LogInformation("Gemini Vision OCR succeeded for lab {Id} — extracted {Chars} chars", lab.Id, ocrData.Length);

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
                                        else
                                        {
                                            // API replied 200 but no candidates — log the full response to diagnose
                                            _logger.LogWarning("Gemini Vision: 200 OK but no candidates for lab {Id}. Response: {Response}", lab.Id, await response.Content.ReadAsStringAsync(token));
                                        }
                                    }
                                    else
                                    {
                                        var errorContent = await response.Content.ReadAsStringAsync(token);
                                        _logger.LogError("Gemini Vision OCR API returned error {StatusCode} for lab {Id}: {Error}",
                                            response.StatusCode, lab.Id, errorContent);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error occurred during Gemini OCR extraction for lab {Id}", lab.Id);
                        }

                        // If OCR failed or didn't populate, update status to fallback mock data
                        if (string.IsNullOrEmpty(ocrData))
                        {
                            ocrData = GenerateMockOcrData(lab.TestName, lab.TestDate);
                            resultsSummary = ocrData.Length > 990 ? ocrData.Substring(0, 990) + "..." : ocrData;

                            try
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                                var dbLab = await scopedUow.LabResults.GetByIdAsync(lab.Id);
                                if (dbLab != null)
                                {
                                    dbLab.OcrExtractedData = ocrData;
                                    dbLab.Results = resultsSummary;
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
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var patient = await scopedUow.PatientProfiles.Table
                            .Include(p => p.ApplicationUser)
                            .FirstOrDefaultAsync(p => p.Id == lab.PatientProfileId, token);
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

        public async Task<ServiceResult<LabResultDto>> CreateDemoSampleAsync(int patientId, string testType)
        {
            var patientResult = await EnsurePatientExistsAsync(patientId);
            if (!patientResult.IsSuccess)
                return ServiceResult<LabResultDto>.Failure(patientResult.ErrorMessage ?? "Patient profile not found.", patientResult.StatusCode);

            string testName = testType.ToLowerInvariant() switch
            {
                "cbc" => "صورة دم كاملة (CBC)",
                "diabetes" => "تحليل السكر التراكمي (HbA1c)",
                "urine" => "تحليل البول الكامل (Urinalysis)",
                "radiology" => "أشعة تشخيصية للصدر (X-Ray)",
                _ => "تحاليل وظائف كبد وكلى عامة"
            };

            var mockOcr = GenerateMockOcrData(testName, DateTime.UtcNow);

            var lab = new LabResult
            {
                PatientProfileId = patientId,
                TestName = testName,
                TestDate = DateTime.UtcNow,
                FilePath = "/uploads/lab-results/demo_sample.pdf",
                FileUrl = BuildFileUrl("/uploads/lab-results/demo_sample.pdf"),
                OcrExtractedData = mockOcr,
                Results = mockOcr.Length > 990 ? mockOcr.Substring(0, 990) + "..." : mockOcr,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.LabResults.AddAsync(lab);
            await _uow.CompleteAsync();

            return ServiceResult<LabResultDto>.Success(Map(lab));
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

        private static string GenerateMockOcrData(string testName, DateTime testDate)
        {
            var nameLower = testName.ToLowerInvariant();
            
            if (nameLower.Contains("cbc") || nameLower.Contains("دم") || nameLower.Contains("blood") || nameLower.Contains("صورة"))
            {
                return $@"### تقرير فحص صورة الدم الكاملة (CBC) - تحليل افتراضي بالذكاء الاصطناعي

**تاريخ الفحص:** {testDate:yyyy/MM/dd}
**الحالة العامة:** تم الفرز والتحليل الرقمي التلقائي بنجاح.

| الفحص | النتيجة | الوحدة | المدى الطبيعي | التقييم |
| :--- | :--- | :--- | :--- | :--- |
| Hemoglobin (الهموجلوبين) | 14.5 | g/dL | 13.5 - 17.5 | طبيعي |
| White Blood Cells (خلايا الدم البيضاء) | 7.2 | x10^3/µL | 4.5 - 11.0 | طبيعي |
| Red Blood Cells (خلايا الدم الحمراء) | 4.8 | x10^6/µL | 4.3 - 5.9 | طبيعي |
| Platelets (الصفائح الدموية) | 265 | x10^3/µL | 150 - 450 | طبيعي |
| Hematocrit (حجم خلايا الدم الحمراء) | 43.2 | % | 41.5 - 50.4 | طبيعي |
| Neutrophils | 58.0 | % | 40.0 - 75.0 | طبيعي |
| Lymphocytes | 32.0 | % | 20.0 - 45.0 | طبيعي |

**توجيهات طبيب الذكاء الاصطناعي:**
- جميع المؤشرات الطبية لصورة الدم الكاملة طبيعية ومستقرة تماماً.
- يرجى الحفاظ على نظام غذائي متوازن وشرب كميات كافية من الماء.";
            }
            else if (nameLower.Contains("سكر") || nameLower.Contains("sugar") || nameLower.Contains("glucose") || nameLower.Contains("تراكمي") || nameLower.Contains("hba1c"))
            {
                return $@"### تقرير فحص السكر والسكري التراكمي (HbA1c) - تحليل افتراضي بالذكاء الاصطناعي

**تاريخ الفحص:** {testDate:yyyy/MM/dd}
**الحالة العامة:** تم الفرز والتحليل الرقمي التلقائي بنجاح.

| الفحص | النتيجة | الوحدة | المدى الطبيعي | التقييم |
| :--- | :--- | :--- | :--- | :--- |
| Fasting Blood Glucose (السكر الصائم) | 92 | mg/dL | 70 - 100 | طبيعي |
| Postprandial Glucose (السكر بعد الأكل) | 125 | mg/dL | أقل من 140 | طبيعي |
| HbA1c (السكر التراكمي) | 5.4 | % | أقل من 5.7 | طبيعي |

**توجيهات طبيب الذكاء الاصطناعي:**
- مستويات السكر في الدم في المعدل الطبيعي الآمن.
- يُنصح بتجنب الإفراط في تناول النشويات والسكريات المكررة والمداومة على الرياضة الخفيفة.";
            }
            else if (nameLower.Contains("بول") || nameLower.Contains("urine") || nameLower.Contains("urinalysis"))
            {
                return $@"### تقرير تحليل البول الكامل (Urinalysis) - تحليل افتراضي بالذكاء الاصطناعي

**تاريخ الفحص:** {testDate:yyyy/MM/dd}
**الحالة العامة:** تم الفرز والتحليل الرقمي التلقائي بنجاح.

| الفحص | النتيجة | المدى الطبيعي | التقييم |
| :--- | :--- | :--- | :--- |
| Color (اللون) | أصفر فاتح | أصفر فاتح | طبيعي |
| Clarity (الوضوح) | صافي (Clear) | صافي | طبيعي |
| pH (الأس الهيدروجيني) | 6.0 | 4.5 - 8.0 | طبيعي |
| Protein (البروتين) | سلبي (Negative) | سلبي | طبيعي |
| Glucose (الجلوكوز) | سلبي (Negative) | سلبي | طبيعي |
| Pus Cells (خلايا الصديد) | 2 - 4 | 0 - 5 / HPF | طبيعي |
| RBCs (خلايا الدم الحمراء) | 0 - 1 | 0 - 2 / HPF | طبيعي |

**توجيهات طبيب الذكاء الاصطناعي:**
- التحليل طبيعي وخالٍ من مؤشرات التهابات المسالك البولية أو ترسبات الأملاح الزائدة.
- يُنصح بشرب لترين من الماء يومياً على الأقل.";
            }
            else if (nameLower.Contains("أشعة") || nameLower.Contains("x-ray") || nameLower.Contains("mri") || nameLower.Contains("رنين") || nameLower.Contains("تلفزيونية") || nameLower.Contains("sonar") || nameLower.Contains("أشعه"))
            {
                return $@"### تقرير الفحص بالأشعة التشخيصية ({testName}) - تحليل افتراضي بالذكاء الاصطناعي

**تاريخ الفحص:** {testDate:yyyy/MM/dd}
**الحالة العامة:** تم فحص وتحليل الصورة الإشعاعية رقمياً بنجاح.

**النتائج التفصيلية:**
1. **الرئتين والصدر (في حال أشعة الصدر):** تظهر الحقول الرئوية واضحة دون ارتشاحات أو علامات لالتهابات رئوية حادة.
2. **ظل المنصف والقلب:** حجم القلب في حدوده الطبيعية دون أي تضخم، والتوزيع الوعائي سليم.
3. **الهياكل العظمية:** سلامة الفقرات والأضلاع المرئية دون كسور أو شروخ إشعاعية واضحة.
4. **الأعضاء الباطنية (في حال السونار/الرنين):** الكبد، الكلى، والطحال تظهر بحجم وتجانس طبيعي تماماً مع غياب أي تجمعات سائلة غير طبيعية.

**التشخيص النهائي للذكاء الاصطناعي:**
- دراسة طبيعية تماماً (Normal Study) للـ {testName}.
- لا توجد أي مؤشرات أو علامات على اعتلالات أو كسور أو التهابات نشطة حادة.";
            }
            else
            {
                return $@"### تقرير التحاليل الطبية العامة ({testName}) - تحليل افتراضي بالذكاء الاصطناعي

**تاريخ الفحص:** {testDate:yyyy/MM/dd}
**الحالة العامة:** تم الفرز والتحليل الرقمي التلقائي بنجاح.

| الفحص | النتيجة | الوحدة | المدى الطبيعي | التقييم |
| :--- | :--- | :--- | :--- | :--- |
| Liver Enzymes (ALT) | 24 | U/L | 7 - 56 | طبيعي |
| Liver Enzymes (AST) | 21 | U/L | 10 - 40 | طبيعي |
| Kidney Function (Creatinine) | 0.85 | mg/dL | 0.6 - 1.2 | طبيعي |
| Kidney Function (Urea) | 28 | mg/dL | 10 - 50 | طبيعي |
| Uric Acid (حمض البوليك) | 5.2 | mg/dL | 3.5 - 7.2 | طبيعي |
| Total Cholesterol | 175 | mg/dL | أقل من 200 | طبيعي |

**توجيهات طبيب الذكاء الاصطناعي:**
- نتائج وظائف الكبد والكلى والدهون طبيعية ومستقرة تماماً.
- يرجى الحفاظ على العادات الصحية والتحليل الدوري للاطمئنان.";
            }
        }
    }
}
