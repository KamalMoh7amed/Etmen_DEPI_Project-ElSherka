

namespace Etmen_BLL.Repositories.Services
{
    public sealed class CrisisRiskEngineService : ICrisisRiskEngineService
    {
        private readonly IUnitOfWork _uow;

        public CrisisRiskEngineService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        /// <summary>
        /// Calculates the crisis risk for a specific patient against a given crisis configuration.
        /// Uses the crisis-specific thresholds (EmergencyThreshold, HighRiskThreshold, MediumRiskThreshold)
        /// to determine the patient's risk level within the context of that crisis.
        /// </summary>
        public async Task<ServiceResult<CrisisRiskResultDto>> CalculateCrisisRiskAsync(int patientProfileId, int crisisConfigurationId)
        {
            try
            {
                if (patientProfileId <= 0)
                    return ServiceResult<CrisisRiskResultDto>.Failure("Valid patient profile ID is required.");

                if (crisisConfigurationId <= 0)
                    return ServiceResult<CrisisRiskResultDto>.Failure("Valid crisis configuration ID is required.");

                // Get patient profile
                var patient = await _uow.PatientProfiles.GetByIdAsync(patientProfileId);
                if (patient == null)
                    return ServiceResult<CrisisRiskResultDto>.Failure("Patient profile not found.");

                // Get latest medical record for patient
                var latestRecord = await _uow.MedicalRecords.GetLatestByPatientIdAsync(patientProfileId);
                if (latestRecord == null)
                    return ServiceResult<CrisisRiskResultDto>.Failure("No medical records found for patient.");

                // Get crisis configuration with outbreak zones
                var crisis = await _uow.CrisisConfigurations.GetWithOutbreakZonesAsync(crisisConfigurationId);
                if (crisis == null)
                    return ServiceResult<CrisisRiskResultDto>.Failure("Crisis configuration not found.");

                // Calculate base risk score using RiskCalculatorHelper
                var (riskScore, _, triggeredFactors) = RiskCalculatorHelper.Calculate(
                    latestRecord.SystolicBP,
                    latestRecord.DiastolicBP,
                    latestRecord.HeartRate,
                    latestRecord.Temperature,
                    latestRecord.OxygenSaturation,
                    latestRecord.BloodSugar,
                    latestRecord.Symptoms
                );

                // Determine risk level using crisis-specific thresholds
                // This allows each crisis configuration to define its own sensitivity
                var riskLevel = riskScore >= crisis.EmergencyThreshold
                    ? Etmen_Domain.Enums.RiskLevel.Emergency
                    : riskScore >= crisis.HighRiskThreshold
                        ? Etmen_Domain.Enums.RiskLevel.High
                        : riskScore >= crisis.MediumRiskThreshold
                            ? Etmen_Domain.Enums.RiskLevel.Medium
                            : Etmen_Domain.Enums.RiskLevel.Low;

                // Check if patient is in an outbreak zone using their stored location.
                bool isInOutbreakZone = false;
                string? zoneName = null;

                if (patient.Latitude.HasValue && patient.Longitude.HasValue)
                {
                    var outbreakZones = crisis.OutbreakZones;
                    foreach (var zone in outbreakZones)
                    {
                        double distance = GeoHelper.CalculateDistanceKm(
                            (double)patient.Latitude.Value,
                            (double)patient.Longitude.Value,
                            (double)zone.CenterLatitude,
                            (double)zone.CenterLongitude
                        );

                        if (distance <= (double)zone.RadiusInKm)
                        {
                            isInOutbreakZone = true;
                            zoneName = zone.ZoneName;
                            break;
                        }
                    }
                }
                // If patient has no location data → assume not in zone (safe default)

                // Generate recommendations based on risk level and triggered vitals/symptoms
                var recommendations = RiskCalculatorHelper.GenerateRecommendations(riskLevel, triggeredFactors, isCrisisMode: true);

                var result = new CrisisRiskResultDto
                {
                    RiskScore = riskScore,
                    RiskLevel = riskLevel,
                    IsInOutbreakZone = isInOutbreakZone,
                    ZoneName = zoneName,
                    Recommendations = recommendations,
                    SystemMode = crisis.SystemMode
                };

                return ServiceResult<CrisisRiskResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<CrisisRiskResultDto>.Failure($"Failed to calculate crisis risk: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the probability (0.0 – 1.0) that a geographic point is within an active outbreak.
        /// Higher probability when the point is closer to the center of a zone and the zone has a higher risk level.
        /// </summary>
        public async Task<ServiceResult<decimal>> CalculateOutbreakProbabilityAsync(decimal latitude, decimal longitude, int crisisConfigurationId)
        {
            try
            {
                if (crisisConfigurationId <= 0)
                    return ServiceResult<decimal>.Failure("Valid crisis configuration ID is required.");

                if (latitude < -90m || latitude > 90m)
                    return ServiceResult<decimal>.Failure("Invalid latitude. Must be between -90 and 90.");

                if (longitude < -180m || longitude > 180m)
                    return ServiceResult<decimal>.Failure("Invalid longitude. Must be between -180 and 180.");

                // Get all outbreak zones for this crisis
                var zones = await _uow.OutbreakZones.GetByCrisisIdAsync(crisisConfigurationId);

                var zoneList = zones as IList<Etmen_Domain.Entities.OutbreakZone> ?? zones.ToList();

                if (zoneList.Count == 0)
                    return ServiceResult<decimal>.Success(0m); // No outbreak zones = no probability

                decimal maxOutbreakProbability = 0m;

                foreach (var zone in zoneList)
                {
                    // Calculate distance from point to zone center using Haversine formula
                    double distanceKm = GeoHelper.CalculateDistanceKm(
                        (double)latitude,
                        (double)longitude,
                        (double)zone.CenterLatitude,
                        (double)zone.CenterLongitude
                    );

                    // Only consider zones where the point falls within the radius
                    if (distanceKm <= (double)zone.RadiusInKm)
                    {
                        // Proximity factor: 1.0 at center, 0.0 at the edge
                        decimal distanceRatio = 1m - (decimal)(distanceKm / (double)zone.RadiusInKm);

                        // Normalise RiskLevel to [0, 1]. Clamp to valid range to avoid bad data issues.
                        int clampedRiskLevel = Math.Clamp(zone.RiskLevel, 0, 10);
                        decimal riskMultiplier = clampedRiskLevel / 10m;

                        decimal zoneProbability = distanceRatio * riskMultiplier;
                        maxOutbreakProbability = Math.Max(maxOutbreakProbability, zoneProbability);
                    }
                }

                // Cap probability at 1.0
                decimal finalProbability = Math.Min(maxOutbreakProbability, 1.0m);
                return ServiceResult<decimal>.Success(finalProbability);
            }
            catch (Exception ex)
            {
                return ServiceResult<decimal>.Failure($"Failed to calculate outbreak probability: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the list of outbreak zones registered for a given crisis configuration.
        /// Each zone can then be used by the caller to determine which patients fall inside it.
        /// </summary>
        public async Task<ServiceResult<List<OutbreakZoneDto>>> GetPatientsInZoneAsync(int crisisConfigurationId)
        {
            try
            {
                if (crisisConfigurationId <= 0)
                    return ServiceResult<List<OutbreakZoneDto>>.Failure("Valid crisis configuration ID is required.");

                // Retrieve all outbreak zones for this crisis
                var zones = await _uow.OutbreakZones.GetByCrisisIdAsync(crisisConfigurationId);

                var result = zones
                    .Select(zone => zone.Adapt<OutbreakZoneDto>())
                    .ToList();

                return ServiceResult<List<OutbreakZoneDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<OutbreakZoneDto>>.Failure($"Failed to retrieve outbreak zones: {ex.Message}");
            }
        }
    }
}