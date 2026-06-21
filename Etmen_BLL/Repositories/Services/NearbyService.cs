

namespace Etmen_BLL.Repositories.Services
{
    public sealed class NearbyService : INearbyService
    {
        private readonly IUnitOfWork _uow;

        public NearbyService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<List<ProviderDto>>> SearchNearbyProvidersAsync(NearbySearchDto dto)
        {
            try
            {
                if (dto.RadiusInKm <= 0)
                    return ServiceResult<List<ProviderDto>>.Failure("Radius must be greater than zero.");

                // Get nearby providers based on type and radius
                var providers = await _uow.HealthcareProviders.GetNearbyProvidersAsync(
                    dto.Latitude,
                    dto.Longitude,
                    dto.RadiusInKm
                );

                // Filter by type if specified
                if (!string.IsNullOrWhiteSpace(dto.Type))
                {
                    providers = providers.Where(p => p.Type.Equals(dto.Type, StringComparison.OrdinalIgnoreCase));
                }

                // Convert to DTO and calculate distances
                var providerDtos = providers
                    .Select(p => new ProviderDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Type = p.Type,
                        Address = p.Address,
                        Phone = p.Phone,
                        AvailableBeds = p.AvailableBeds,
                        IsEmergencyCenter = p.IsEmergencyCenter,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        DistanceKm = (decimal)GeoHelper.CalculateDistanceKm(
                            (double)dto.Latitude,
                            (double)dto.Longitude,
                            (double)p.Latitude,
                            (double)p.Longitude
                        )
                    })
                    .OrderBy(p => p.DistanceKm)
                    .ToList();

                return ServiceResult<List<ProviderDto>>.Success(providerDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ProviderDto>>.Failure($"Failed to search nearby providers: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<AvailableSlotDto>>> GetAvailableSlotsByProviderAsync(int providerId)
        {
            try
            {
                if (providerId <= 0)
                    return ServiceResult<List<AvailableSlotDto>>.Failure("Valid provider ID is required.");

                var affiliations = await _uow.DoctorProviders.GetByProviderIdAsync(providerId);
                var doctorIds = affiliations.Select(a => a.DoctorProfileId).ToList();

                if (!doctorIds.Any())
                {
                    return ServiceResult<List<AvailableSlotDto>>.Success(new List<AvailableSlotDto>());
                }

                // Get available slots for the affiliated doctors
                var slots = await _uow.AvailableSlots.FindAsync(s => doctorIds.Contains(s.DoctorProfileId) && !s.IsBooked);
                var slotDtos = slots
                    .Select(s => s.Adapt<AvailableSlotDto>())
                    .ToList();

                return ServiceResult<List<AvailableSlotDto>>.Success(slotDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<AvailableSlotDto>>.Failure($"Failed to retrieve available slots: {ex.Message}");
            }
        }

        public async Task<ServiceResult> BookAppointmentAsync(BookingRequestDto dto)
        {
            try
            {
                if (dto.PatientProfileId <= 0)
                    return ServiceResult.Failure("Valid patient profile ID is required.");

                if (dto.DoctorId <= 0)
                    return ServiceResult.Failure("Valid doctor ID is required.");

                if (dto.SlotId <= 0)
                    return ServiceResult.Failure("Valid slot ID is required.");

                // Check if slot exists and is available
                var slot = await _uow.AvailableSlots.GetByIdAsync(dto.SlotId);
                if (slot == null || slot.IsBooked)
                    return ServiceResult.Failure("Selected slot is not available.");

                // Verify patient exists
                var patient = await _uow.PatientProfiles.GetByIdAsync(dto.PatientProfileId);
                if (patient == null)
                    return ServiceResult.Failure("Patient profile not found.");

                // Verify doctor exists
                var doctor = await _uow.DoctorProfiles.GetByIdAsync(dto.DoctorId);
                if (doctor == null)
                    return ServiceResult.Failure("Doctor profile not found.");

                // Create appointment
                var appointment = new Appointment
                {
                    PatientProfileId = dto.PatientProfileId,
                    DoctorProfileId = dto.DoctorId,
                    AppointmentDate = dto.Date,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                // Mark slot as booked
                slot.IsBooked = true;

                await _uow.Appointments.AddAsync(appointment);
                _uow.AvailableSlots.Update(slot);
                try
                {
                    await _uow.CompleteAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return ServiceResult.Failure("عذراً، هذا الموعد تم حجزه للتو من قبل مريض آخر.");
                }

                return ServiceResult.Success(201);
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to book appointment: {ex.Message}");
            }
        }

    }
}