using Etmen_BLL.DTOs.Crisis;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Mapster;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class CrisisRiskEngineService : ICrisisRiskEngineService
    {
        private readonly IUnitOfWork _uow;

        public CrisisRiskEngineService(IUnitOfWork uow)
        {
            _uow = uow;
        }

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

                // Get crisis configuration with thresholds
                var crisis = await _uow.CrisisConfigurations.GetWithOutbreakZonesAsync(crisisConfigurationId);
                if (crisis == null)
                    return ServiceResult<CrisisRiskResultDto>.Failure("Crisis configuration not found.");

                // Calculate base risk score using RiskCalculatorHelper
                var (riskScore, isEmergency, triggeredFactors) = RiskCalculatorHelper.Calculate(
                    latestRecord.SystolicBP,
                    latestRecord.DiastolicBP,
                    latestRecord.HeartRate,
                    latestRecord.Temperature,
                    latestRecord.OxygenSaturation,
                    latestRecord.BloodSugar,
                    latestRecord.Symptoms
                );

                // Determine risk level based on crisis thresholds
                var riskLevel = RiskCalculatorHelper.GetRiskLevel(riskScore);

                // Check if patient is in outbreak zone
                bool isInOutbreakZone = false;
                string? zoneName = null;

                // Get all outbreak zones for this crisis
                var outbreakZones = await _uow.OutbreakZones.GetByCrisisIdAsync(crisisConfigurationId);

                if (outbreakZones.Any())
                {
                    // Note: Patient location data (latitude/longitude) should ideally come from:
                    // 1. PatientProfile (if location tracking is enabled)
                    // 2. Separate LocationHistory table
                    // 3. Last known location from medical records (if enhanced to include coordinates)
                    // 
                    // For now, if patient has no location data, we assume they're not in zone.
                    // This check would be enhanced once patient location tracking is available.

                    // Example: If PatientProfile had coordinates, we'd check like this:
                    // foreach (var zone in outbreakZones)
                    // {
                    //     double distance = GeoHelper.CalculateDistanceKm(
                    //         (double)patient.Latitude,
                    //         (double)patient.Longitude,
                    //         (double)zone.CenterLatitude,
                    //         (double)zone.CenterLongitude
                    //     );
                    //     if (distance <= (double)zone.RadiusInKm)
                    //     {
                    //         isInOutbreakZone = true;
                    //         zoneName = zone.ZoneName;
                    //         break;
                    //     }
                    // }
                }

                // Generate recommendations based on risk level and crisis mode
                var recommendations = RiskCalculatorHelper.GenerateRecommendations(riskLevel, triggeredFactors);

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

        public async Task<ServiceResult<decimal>> CalculateOutbreakProbabilityAsync(decimal latitude, decimal longitude, int crisisConfigurationId)
        {
            try
            {
                if (crisisConfigurationId <= 0)
                    return ServiceResult<decimal>.Failure("Valid crisis configuration ID is required.");

                // Validate coordinates
                if (latitude < -90 || latitude > 90)
                    return ServiceResult<decimal>.Failure("Invalid latitude coordinate.");
                if (longitude < -180 || longitude > 180)
                    return ServiceResult<decimal>.Failure("Invalid longitude coordinate.");

                // Get all outbreak zones for this crisis
                var zones = await _uow.OutbreakZones.GetByCrisisIdAsync(crisisConfigurationId);

                if (!zones.Any())
                    return ServiceResult<decimal>.Success(0m); // No outbreak zones = no probability

                decimal maxOutbreakProbability = 0m;

                foreach (var zone in zones)
                {
                    // Calculate distance from point to zone center
                    double distanceKm = GeoHelper.CalculateDistanceKm(
                        (double)latitude,
                        (double)longitude,
                        (double)zone.CenterLatitude,
                        (double)zone.CenterLongitude
                    );

                    // Check if point is within zone radius
                    if (distanceKm <= (double)zone.RadiusInKm)
                    {
                        // Calculate probability based on distance and zone risk level
                        // Closer to center = higher probability
                        // Risk level also influences probability
                        decimal distanceRatio = (decimal)(1 - (distanceKm / (double)zone.RadiusInKm));
                        decimal riskMultiplier = zone.RiskLevel / 10m; // Assuming risk level 1-10
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

        public async Task<ServiceResult<List<OutbreakZoneDto>>> GetPatientsInZoneAsync(int crisisConfigurationId)
        {
            try
            {
                if (crisisConfigurationId <= 0)
                    return ServiceResult<List<OutbreakZoneDto>>.Failure("Valid crisis configuration ID is required.");

                // Get all outbreak zones for this crisis
                var zones = await _uow.OutbreakZones.GetByCrisisIdAsync(crisisConfigurationId);

                var result = zones
                    .Select(zone => zone.Adapt<OutbreakZoneDto>())
                    .ToList();

                return ServiceResult<List<OutbreakZoneDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<OutbreakZoneDto>>.Failure($"Failed to retrieve patients in zone: {ex.Message}");
            }
        }

    }
}