using Etmen_BLL.DTOs.Emergency;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Mapster;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class EmergencyService : IEmergencyService
    {
        private readonly IUnitOfWork _uow;

        public EmergencyService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<EmergencyRequestDto>> CreateEmergencyRequestAsync(EmergencyRequestDto dto)
        {
            try
            {
                if (dto.PatientProfileId <= 0)
                    return ServiceResult<EmergencyRequestDto>.Failure("Valid patient profile ID is required.");

                // Verify patient exists
                var patient = await _uow.PatientProfiles.GetByIdAsync(dto.PatientProfileId);
                if (patient == null)
                    return ServiceResult<EmergencyRequestDto>.Failure("Patient profile not found.");

                if (string.IsNullOrWhiteSpace(dto.EmergencyType))
                    return ServiceResult<EmergencyRequestDto>.Failure("Emergency type is required.");

                // Validate location coordinates
                if (dto.Latitude < -90 || dto.Latitude > 90)
                    return ServiceResult<EmergencyRequestDto>.Failure("Invalid latitude coordinate.");
                if (dto.Longitude < -180 || dto.Longitude > 180)
                    return ServiceResult<EmergencyRequestDto>.Failure("Invalid longitude coordinate.");

                var emergencyRequest = new EmergencyRequest
                {
                    PatientProfileId = dto.PatientProfileId,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    EmergencyType = dto.EmergencyType,
                    Description = dto.Description,
                    Status = EmergencyRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                };

                await _uow.EmergencyRequests.AddAsync(emergencyRequest);
                await _uow.CompleteAsync();

                var result = emergencyRequest.Adapt<EmergencyRequestDto>();
                return ServiceResult<EmergencyRequestDto>.Success(result, 201);
            }
            catch (Exception ex)
            {
                return ServiceResult<EmergencyRequestDto>.Failure($"Failed to create emergency request: {ex.Message}");
            }
        }

        public async Task<ServiceResult<EmergencyRequestDto>> GetEmergencyRequestAsync(int requestId)
        {
            try
            {
                if (requestId <= 0)
                    return ServiceResult<EmergencyRequestDto>.Failure("Valid request ID is required.");

                var emergencyRequest = await _uow.EmergencyRequests.GetByIdAsync(requestId);
                if (emergencyRequest == null)
                    return ServiceResult<EmergencyRequestDto>.Failure("Emergency request not found.");

                var result = emergencyRequest.Adapt<EmergencyRequestDto>();
                return ServiceResult<EmergencyRequestDto>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<EmergencyRequestDto>.Failure($"Failed to retrieve emergency request: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<EmergencyTrackingDto>>> GetPendingEmergenciesAsync()
        {
            try
            {
                var pendingRequests = await _uow.EmergencyRequests.GetPendingRequestsAsync();
                var trackingDtos = new List<EmergencyTrackingDto>();

                foreach (var request in pendingRequests)
                {
                    decimal distanceInKm = 0;

                    // Calculate distance if both request and provider have coordinates
                    if (request.Latitude.HasValue && request.Longitude.HasValue && 
                        request.HealthcareProvider != null)
                    {
                        distanceInKm = (decimal)GeoHelper.CalculateDistanceKm(
                            (double)request.Latitude.Value,
                            (double)request.Longitude.Value,
                            (double)request.HealthcareProvider.Latitude,
                            (double)request.HealthcareProvider.Longitude
                        );
                    }

                    var tracking = new EmergencyTrackingDto
                    {
                        RequestId = request.Id,
                        Status = request.Status,
                        ProviderName = request.HealthcareProvider?.Name,
                        ProviderPhone = request.HealthcareProvider?.Phone,
                        EstimatedArrivalTime = null, // Can be calculated if additional data available
                        DistanceInKm = distanceInKm,
                        RequestedAt = request.RequestedAt,
                        AcceptedAt = request.AcceptedAt
                    };
                    trackingDtos.Add(tracking);
                }

                return ServiceResult<List<EmergencyTrackingDto>>.Success(trackingDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<EmergencyTrackingDto>>.Failure($"Failed to retrieve pending emergencies: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateEmergencyStatusAsync(int requestId, EmergencyUpdateDto dto)
        {
            try
            {
                if (requestId <= 0)
                    return ServiceResult.Failure("Valid request ID is required.");

                if (string.IsNullOrWhiteSpace(dto.Status))
                    return ServiceResult.Failure("Status is required.");

                var emergencyRequest = await _uow.EmergencyRequests.GetByIdAsync(requestId);
                if (emergencyRequest == null)
                    return ServiceResult.Failure("Emergency request not found.");

                // Parse status string to enum
                if (!Enum.TryParse<EmergencyRequestStatus>(dto.Status, true, out var newStatus))
                    return ServiceResult.Failure("Invalid emergency status.");

                // Validate provider exists if being assigned
                if (dto.AssignedProviderId.HasValue && dto.AssignedProviderId > 0)
                {
                    var provider = await _uow.HealthcareProviders.GetByIdAsync(dto.AssignedProviderId.Value);
                    if (provider == null)
                        return ServiceResult.Failure("Healthcare provider not found.");

                    emergencyRequest.HealthcareProviderId = dto.AssignedProviderId;
                }

                emergencyRequest.Status = newStatus;

                if (!string.IsNullOrWhiteSpace(dto.ResponseNotes))
                    emergencyRequest.ResponseNotes = dto.ResponseNotes;

                if (newStatus == EmergencyRequestStatus.Accepted && emergencyRequest.AcceptedAt == null)
                    emergencyRequest.AcceptedAt = DateTime.UtcNow;

                if (newStatus == EmergencyRequestStatus.Completed && emergencyRequest.CompletedAt == null)
                    emergencyRequest.CompletedAt = DateTime.UtcNow;

                _uow.EmergencyRequests.Update(emergencyRequest);
                await _uow.CompleteAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to update emergency status: {ex.Message}");
            }
        }

        public async Task<ServiceResult<HospitalQueueDto>> GetHospitalQueueAsync()
        {
            try
            {
                var pendingRequests = await _uow.EmergencyRequests.GetPendingRequestsAsync();

                if (!pendingRequests.Any())
                    return ServiceResult<HospitalQueueDto>.Failure("No pending emergency requests in queue.");

                // Return the first pending request as the current queue entry
                var firstRequest = pendingRequests.First();
                var queueDto = new HospitalQueueDto
                {
                    RequestId = firstRequest.Id,
                    PatientName = firstRequest.PatientProfile?.FullName ?? "Unknown",
                    EmergencyType = firstRequest.EmergencyType ?? "Unspecified",
                    Status = firstRequest.Status,
                    RequestedAt = firstRequest.RequestedAt,
                    AvailableBeds = firstRequest.HealthcareProvider?.AvailableBeds ?? 0
                };

                return ServiceResult<HospitalQueueDto>.Success(queueDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<HospitalQueueDto>.Failure($"Failed to retrieve hospital queue: {ex.Message}");
            }
        }

    }
}