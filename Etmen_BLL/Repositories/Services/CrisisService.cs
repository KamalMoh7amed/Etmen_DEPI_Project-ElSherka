

namespace Etmen_BLL.Repositories.Services
{
    public sealed class CrisisService : ICrisisService
    {
        private readonly IUnitOfWork _uow;

        public CrisisService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<CrisisConfigurationDto>> GetActiveCrisisAsync()
        {
            var crisis = await _uow.CrisisConfigurations.GetActiveCrisisAsync();
            return crisis is null
                ? ServiceResult<CrisisConfigurationDto>.NotFound("No active crisis was found.")
                : ServiceResult<CrisisConfigurationDto>.Success(Map(crisis));
        }

        public async Task<ServiceResult<List<CrisisConfigurationDto>>> GetAllCrisesAsync()
        {
            var crises = await _uow.CrisisConfigurations.Table
                .Include(c => c.SymptomWeights)
                .Include(c => c.OutbreakZones)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<CrisisConfigurationDto>>.Success(crises.Select(Map).ToList());
        }

        public async Task<ServiceResult<CrisisConfigurationDto>> GetCrisisByIdAsync(int crisisId)
        {
            var crisis = await GetCrisisSnapshotAsync(crisisId);
            return crisis is null
                ? ServiceResult<CrisisConfigurationDto>.NotFound("Crisis was not found.")
                : ServiceResult<CrisisConfigurationDto>.Success(Map(crisis));
        }

        public async Task<ServiceResult<CrisisStatsDto>> GetCrisisStatsAsync(int crisisId)
        {
            var crisis = await GetCrisisSnapshotAsync(crisisId);
            if (crisis is null)
                return ServiceResult<CrisisStatsDto>.NotFound("Crisis was not found.");

            var endDate = crisis.EndDate ?? DateTime.UtcNow;
            var assessments = (await _uow.RiskAssessments.GetByDateRangeAsync(crisis.StartDate, endDate)).ToList();
            var stats = new CrisisStatsDto
            {
                TotalAssessments = assessments.Count,
                HighRiskCount = assessments.Count(a => a.RiskLevel >= RiskLevel.High),
                CriticalCount = assessments.Count(a => a.RiskLevel >= RiskLevel.Critical || a.IsEmergency),
                OutbreakZonesCount = crisis.OutbreakZones.Count,
                AverageRiskScore = assessments.Count == 0 ? 0 : Math.Round(assessments.Average(a => a.RiskScore), 2),
                LastUpdated = DateTime.UtcNow
            };

            return ServiceResult<CrisisStatsDto>.Success(stats);
        }

        public async Task<ServiceResult<CrisisConfigurationDto>> CreateCrisisAsync(CreateCrisisDto dto)
        {
            var errors = ValidateCreate(dto).ToList();
            if (errors.Count > 0)
                return ServiceResult<CrisisConfigurationDto>.Failure(errors);

            var crisis = new CrisisConfiguration
            {
                CrisisName = dto.CrisisName.Trim(),
                CrisisType = dto.CrisisType,
                SystemMode = dto.SystemMode,
                Description = Normalize(dto.Description),
                StartDate = dto.StartDate == default ? DateTime.UtcNow : dto.StartDate,
                EndDate = dto.EndDate,
                EmergencyThreshold = dto.EmergencyThreshold,
                HighRiskThreshold = dto.HighRiskThreshold,
                MediumRiskThreshold = dto.MediumRiskThreshold,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.CrisisConfigurations.AddAsync(crisis);
            await _uow.CompleteAsync();

            return ServiceResult<CrisisConfigurationDto>.Created(Map(crisis));
        }

        public async Task<ServiceResult<CrisisConfigurationDto>> UpdateCrisisAsync(int crisisId, EditCrisisDto dto)
        {
            var crisis = await _uow.CrisisConfigurations.GetByIdAsync(crisisId);
            if (crisis is null)
                return ServiceResult<CrisisConfigurationDto>.NotFound("Crisis was not found.");

            var errors = ValidateEdit(dto).ToList();
            if (errors.Count > 0)
                return ServiceResult<CrisisConfigurationDto>.Failure(errors);

            crisis.CrisisName = dto.CrisisName.Trim();
            crisis.CrisisType = dto.CrisisType;
            crisis.SystemMode = dto.SystemMode;
            crisis.Description = Normalize(dto.Description);
            crisis.EndDate = dto.EndDate;
            crisis.EmergencyThreshold = dto.EmergencyThreshold;
            crisis.HighRiskThreshold = dto.HighRiskThreshold;
            crisis.MediumRiskThreshold = dto.MediumRiskThreshold;
            crisis.UpdatedAt = DateTime.UtcNow;

            _uow.CrisisConfigurations.Update(crisis);
            await _uow.CompleteAsync();

            var snapshot = await GetCrisisSnapshotAsync(crisisId);
            return ServiceResult<CrisisConfigurationDto>.Success(Map(snapshot ?? crisis));
        }

        public async Task<ServiceResult> ActivateCrisisAsync(int crisisId)
        {
            var crisis = await _uow.CrisisConfigurations.GetByIdAsync(crisisId);
            if (crisis is null)
                return ServiceResult.NotFound("Crisis was not found.");
            
            if (crisis.SystemMode == SystemMode.Normal)
            {
                crisis.SystemMode = SystemMode.Crisis;
                _uow.CrisisConfigurations.Update(crisis);
            }

            await _uow.CrisisConfigurations.ActivateCrisisAsync(crisisId);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeactivateCrisisAsync(int crisisId)
        {
            if (!await _uow.CrisisConfigurations.AnyAsync(c => c.Id == crisisId))
                return ServiceResult.NotFound("Crisis was not found.");

            await _uow.CrisisConfigurations.DeactivateCrisisAsync(crisisId);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteCrisisAsync(int crisisId)
        {
            var crisis = await _uow.CrisisConfigurations.GetByIdAsync(crisisId);
            if (crisis is null)
                return ServiceResult.NotFound("Crisis was not found.");
            if (crisis.IsActive)
                return ServiceResult.Conflict("Active crises must be deactivated before deletion.");

            _uow.CrisisConfigurations.Remove(crisis);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> AddSymptomAsync(int crisisId, SymptomWeightDto symptomDto)
        {
            var crisis = await _uow.CrisisConfigurations.GetWithSymptomWeightsAsync(crisisId);
            if (crisis is null)
                return ServiceResult.NotFound("Crisis was not found.");

            var errors = ValidateSymptom(symptomDto).ToList();
            if (errors.Count > 0)
                return ServiceResult.Failure(errors);

            if (crisis.SymptomWeights.Any(s => SameSymptom(s.SymptomName, symptomDto.SymptomName)))
                return ServiceResult.Conflict("Symptom already exists for this crisis.");

            crisis.SymptomWeights.Add(MapSymptom(symptomDto));
            crisis.UpdatedAt = DateTime.UtcNow;
            _uow.CrisisConfigurations.Update(crisis);
            await _uow.CompleteAsync();
            return ServiceResult.Success(201);
        }

        public async Task<ServiceResult> AddMultipleSymptomsAsync(int crisisId, List<SymptomWeightDto> symptomsDto)
        {
            if (symptomsDto is null || symptomsDto.Count == 0)
                return ServiceResult.Failure("At least one symptom is required.");

            var errors = symptomsDto.SelectMany(ValidateSymptom).ToList();
            var duplicateNames = symptomsDto
                .GroupBy(s => s.SymptomName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => $"Duplicate symptom in payload: {g.Key}");
            errors.AddRange(duplicateNames);
            if (errors.Count > 0)
                return ServiceResult.Failure(errors);

            var crisis = await _uow.CrisisConfigurations.GetWithSymptomWeightsAsync(crisisId);
            if (crisis is null)
                return ServiceResult.NotFound("Crisis was not found.");

            var existing = crisis.SymptomWeights.Select(s => s.SymptomName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var conflicts = symptomsDto.Where(s => existing.Contains(s.SymptomName.Trim())).Select(s => s.SymptomName).ToList();
            if (conflicts.Count > 0)
                return ServiceResult.Conflict($"Symptoms already exist: {string.Join(", ", conflicts)}");

            foreach (var symptom in symptomsDto)
                crisis.SymptomWeights.Add(MapSymptom(symptom));

            crisis.UpdatedAt = DateTime.UtcNow;
            _uow.CrisisConfigurations.Update(crisis);
            await _uow.CompleteAsync();
            return ServiceResult.Success(201);
        }

        public async Task<ServiceResult> UpdateSymptomAsync(int crisisId, string symptomName, SymptomWeightDto updatedSymptomDto)
        {
            if (string.IsNullOrWhiteSpace(symptomName))
                return ServiceResult.Failure("Symptom name is required.");

            var errors = ValidateSymptom(updatedSymptomDto).ToList();
            if (errors.Count > 0)
                return ServiceResult.Failure(errors);

            var crisis = await _uow.CrisisConfigurations.GetWithSymptomWeightsAsync(crisisId);
            if (crisis is null)
                return ServiceResult.NotFound("Crisis was not found.");

            var symptom = crisis.SymptomWeights.FirstOrDefault(s => SameSymptom(s.SymptomName, symptomName));
            if (symptom is null)
                return ServiceResult.NotFound("Symptom was not found.");

            if (!SameSymptom(symptomName, updatedSymptomDto.SymptomName) &&
                crisis.SymptomWeights.Any(s => SameSymptom(s.SymptomName, updatedSymptomDto.SymptomName)))
                return ServiceResult.Conflict("Another symptom with the new name already exists.");

            symptom.SymptomName = updatedSymptomDto.SymptomName.Trim();
            symptom.Weight = updatedSymptomDto.Weight;
            symptom.IsEmergencySymptom = updatedSymptomDto.IsEmergencySymptom;
            crisis.UpdatedAt = DateTime.UtcNow;
            _uow.CrisisConfigurations.Update(crisis);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RemoveSymptomAsync(int crisisId, string symptomName)
        {
            if (string.IsNullOrWhiteSpace(symptomName))
                return ServiceResult.Failure("Symptom name is required.");

            var crisis = await _uow.CrisisConfigurations.GetWithSymptomWeightsAsync(crisisId);
            if (crisis is null)
                return ServiceResult.NotFound("Crisis was not found.");

            var symptom = crisis.SymptomWeights.FirstOrDefault(s => SameSymptom(s.SymptomName, symptomName));
            if (symptom is null)
                return ServiceResult.NotFound("Symptom was not found.");

            crisis.SymptomWeights.Remove(symptom);
            crisis.UpdatedAt = DateTime.UtcNow;
            _uow.CrisisConfigurations.Update(crisis);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<List<SymptomWeightDto>>> GetSymptomsByCrisisAsync(int crisisId)
        {
            var crisis = await _uow.CrisisConfigurations.GetWithSymptomWeightsAsync(crisisId);
            return crisis is null
                ? ServiceResult<List<SymptomWeightDto>>.NotFound("Crisis was not found.")
                : ServiceResult<List<SymptomWeightDto>>.Success(crisis.SymptomWeights.OrderByDescending(s => s.Weight).Select(MapSymptom).ToList());
        }

        public async Task<ServiceResult> UpdateRiskThresholdsAsync(int crisisId, decimal? emergencyThreshold, decimal? highRiskThreshold, decimal? mediumRiskThreshold)
        {
            var crisis = await _uow.CrisisConfigurations.GetByIdAsync(crisisId);
            if (crisis is null)
                return ServiceResult.NotFound("Crisis was not found.");

            var emergency = emergencyThreshold ?? crisis.EmergencyThreshold;
            var high = highRiskThreshold ?? crisis.HighRiskThreshold;
            var medium = mediumRiskThreshold ?? crisis.MediumRiskThreshold;

            var errors = ValidateThresholds(emergency, high, medium).ToList();
            if (errors.Count > 0)
                return ServiceResult.Failure(errors);

            crisis.EmergencyThreshold = emergency;
            crisis.HighRiskThreshold = high;
            crisis.MediumRiskThreshold = medium;
            crisis.UpdatedAt = DateTime.UtcNow;

            _uow.CrisisConfigurations.Update(crisis);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        private async Task<CrisisConfiguration?> GetCrisisSnapshotAsync(int crisisId)
            => await _uow.CrisisConfigurations.Table
                .Include(c => c.SymptomWeights)
                .Include(c => c.OutbreakZones)
                .FirstOrDefaultAsync(c => c.Id == crisisId);

        private static CrisisConfigurationDto Map(CrisisConfiguration crisis) => new()
        {
            Id = crisis.Id,
            CrisisName = crisis.CrisisName,
            CrisisType = crisis.CrisisType,
            SystemMode = crisis.SystemMode,
            IsActive = crisis.IsActive,
            StartDate = crisis.StartDate,
            EndDate = crisis.EndDate,
            Description = crisis.Description,
            EmergencyThreshold = crisis.EmergencyThreshold,
            HighRiskThreshold = crisis.HighRiskThreshold,
            MediumRiskThreshold = crisis.MediumRiskThreshold,
            SymptomWeights = crisis.SymptomWeights.OrderByDescending(s => s.Weight).Select(MapSymptom).ToList(),
            ZonesCount = crisis.OutbreakZones.Count
        };

        private static SymptomWeightDto MapSymptom(SymptomWeight symptom) => new()
        {
            SymptomName = symptom.SymptomName,
            Weight = symptom.Weight,
            IsEmergencySymptom = symptom.IsEmergencySymptom
        };

        private static SymptomWeight MapSymptom(SymptomWeightDto dto) => new()
        {
            SymptomName = dto.SymptomName.Trim(),
            Weight = dto.Weight,
            IsEmergencySymptom = dto.IsEmergencySymptom
        };

        private static IEnumerable<string> ValidateCreate(CreateCrisisDto dto)
        {
            if (dto is null)
            {
                yield return "Crisis payload is required.";
                yield break;
            }

            if (string.IsNullOrWhiteSpace(dto.CrisisName))
                yield return "Crisis name is required.";
            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
                yield return "End date cannot be before start date.";

            foreach (var error in ValidateThresholds(dto.EmergencyThreshold, dto.HighRiskThreshold, dto.MediumRiskThreshold))
                yield return error;
        }

        private static IEnumerable<string> ValidateEdit(EditCrisisDto dto)
        {
            if (dto is null)
            {
                yield return "Crisis payload is required.";
                yield break;
            }

            if (string.IsNullOrWhiteSpace(dto.CrisisName))
                yield return "Crisis name is required.";

            foreach (var error in ValidateThresholds(dto.EmergencyThreshold, dto.HighRiskThreshold, dto.MediumRiskThreshold))
                yield return error;
        }

        private static IEnumerable<string> ValidateThresholds(decimal emergency, decimal high, decimal medium)
        {
            if (emergency is < 0 or > 1 || high is < 0 or > 1 || medium is < 0 or > 1)
                yield return "Risk thresholds must be between 0 and 1.";
            if (!(medium < high && high < emergency))
                yield return "Threshold order must be medium < high < emergency.";
        }

        private static IEnumerable<string> ValidateSymptom(SymptomWeightDto dto)
        {
            if (dto is null)
            {
                yield return "Symptom payload is required.";
                yield break;
            }

            if (string.IsNullOrWhiteSpace(dto.SymptomName))
                yield return "Symptom name is required.";
            if (dto.Weight is < 0 or > 1)
                yield return "Symptom weight must be between 0 and 1.";
        }

        private static bool SameSymptom(string left, string right)
            => string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
