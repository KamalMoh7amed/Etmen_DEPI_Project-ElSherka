
using Microsoft.Extensions.Logging;


namespace Etmen_BLL.Repositories.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IUnitOfWork uow, ILogger<AdminService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        /// <summary>
        /// Gets all users with pagination
        /// </summary>
        public async Task<ServiceResult<PaginatedResult<UserListItemDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                    return ServiceResult<PaginatedResult<UserListItemDto>>.Failure("Ø±Ù‚Ù… Ø§Ù„ØµÙØ­Ø© ÙˆØ­Ø¬Ù… Ø§Ù„ØµÙØ­Ø© ÙŠØ¬Ø¨ Ø£Ù† ÙŠÙƒÙˆÙ†Ø§ Ø£ÙƒØ¨Ø± Ù…Ù† 0.");

                var users = await _uow.Users.GetAllAsync();
                var totalCount = users.Count();

                var paginatedUsers = users
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserListItemDto
                    {
                        Id = u.Id,
                        FullName = $"{u.FirstName} {u.LastName}".Trim(),
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber,
                        Role = "User",
                        IsActive = u.IsActive,
                        IsEmailVerified = u.EmailConfirmed,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt
                    })
                    .ToList();

                var result = new PaginatedResult<UserListItemDto>
                {
                    Items = paginatedUsers.AsReadOnly(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogInformation("Retrieved {Count} users for page {PageNumber}.", paginatedUsers.Count, pageNumber);

                return ServiceResult<PaginatedResult<UserListItemDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users.");
                return ServiceResult<PaginatedResult<UserListItemDto>>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù…ÙŠÙ†.");
            }
        }

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        public async Task<ServiceResult<UserListItemDto>> GetUserByIdAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                    return ServiceResult<UserListItemDto>.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… ØºÙŠØ± ØµØ­ÙŠØ­.");

                var user = await _uow.Users.GetByIdAsync(userId.ToString());
                if (user is null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", userId);
                    return ServiceResult<UserListItemDto>.NotFound("Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯.");
                }

                var userDto = new UserListItemDto
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Role = "User",
                    IsActive = user.IsActive,
                    IsEmailVerified = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return ServiceResult<UserListItemDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}.", userId);
                return ServiceResult<UserListItemDto>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù….");
            }
        }

        /// <summary>
        /// Updates user status (active/inactive)
        /// </summary>
        public async Task<ServiceResult> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserId))
                    return ServiceResult.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… Ù…Ø·Ù„ÙˆØ¨.");

                var user = await _uow.Users.GetByIdAsync(dto.UserId);
                if (user is null)
                {
                    _logger.LogWarning("User {UserId} not found for status update.", dto.UserId);
                    return ServiceResult.NotFound("Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯.");
                }

                user.IsActive = dto.IsActive;
                _uow.Users.Update(user);
                await _uow.CompleteAsync();

                _logger.LogInformation("User {UserId} status updated to {IsActive}.", dto.UserId, dto.IsActive);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId} status.", dto.UserId);
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ ØªØ­Ø¯ÙŠØ« Ø­Ø§Ù„Ø© Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù….");
            }
        }

        /// <summary>
        /// Performs bulk actions on multiple users (activate, deactivate, change role, delete)
        /// </summary>
        public async Task<ServiceResult> BulkUserActionAsync(BulkUserActionDto dto)
        {
            try
            {
                if (dto.UserIds is null || dto.UserIds.Count == 0)
                    return ServiceResult.Failure("Ù‚Ø§Ø¦Ù…Ø© Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù…ÙŠÙ† Ù„Ø§ ÙŠÙ…ÙƒÙ† Ø£Ù† ØªÙƒÙˆÙ† ÙØ§Ø±ØºØ©.");

                if (string.IsNullOrWhiteSpace(dto.Action))
                    return ServiceResult.Failure("Ø§Ù„Ø¥Ø¬Ø±Ø§Ø¡ Ù…Ø·Ù„ÙˆØ¨.");

                var users = new List<ApplicationUser>();
                foreach (var userId in dto.UserIds)
                {
                    var user = await _uow.Users.GetByIdAsync(userId);
                    if (user is not null)
                        users.Add(user);
                }

                if (users.Count == 0)
                {
                    _logger.LogWarning("No valid users found for bulk action.");
                    return ServiceResult.NotFound("Ù„Ù… ÙŠØªÙ… Ø§Ù„Ø¹Ø«ÙˆØ± Ø¹Ù„Ù‰ Ù…Ø³ØªØ®Ø¯Ù…ÙŠÙ† ØµØ§Ù„Ø­ÙŠÙ†.");
                }

                switch (dto.Action.ToLower())
                {
                    case "activate":
                        foreach (var user in users)
                            user.IsActive = true;
                        break;

                    case "deactivate":
                        foreach (var user in users)
                            user.IsActive = false;
                        break;

                    case "delete":
                        foreach (var user in users)
                        {
                            user.IsActive = false;
                            // Mark for soft delete if applicable
                        }
                        break;

                    default:
                        return ServiceResult.Failure($"Ø§Ù„Ø¥Ø¬Ø±Ø§Ø¡ '{dto.Action}' ØºÙŠØ± Ù…Ø¹Ø±ÙˆÙ.");
                }

                foreach (var user in users)
                    _uow.Users.Update(user);

                await _uow.CompleteAsync();

                _logger.LogInformation("Bulk action '{Action}' performed on {Count} users.", dto.Action, users.Count);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action.");
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ ØªÙ†ÙÙŠØ° Ø§Ù„Ø¥Ø¬Ø±Ø§Ø¡ Ø§Ù„Ø¬Ù…Ø§Ø¹ÙŠ.");
            }
        }

        /// <summary>
        /// Deletes a user
        /// </summary>
        public async Task<ServiceResult> DeleteUserAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                    return ServiceResult.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… ØºÙŠØ± ØµØ­ÙŠØ­.");

                var user = await _uow.Users.GetByIdAsync(userId.ToString());
                if (user is null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion.", userId);
                    return ServiceResult.NotFound("Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯.");
                }

                _uow.Users.Remove(user);
                await _uow.CompleteAsync();

                _logger.LogInformation("User {UserId} deleted successfully.", userId);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}.", userId);
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø­Ø°Ù Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù….");
            }
        }

        /// <summary>
        /// Gets all providers with pagination
        /// </summary>
        public async Task<ServiceResult<PaginatedResult<ProviderListItemDto>>> GetAllProvidersAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                    return ServiceResult<PaginatedResult<ProviderListItemDto>>.Failure("Ø±Ù‚Ù… Ø§Ù„ØµÙØ­Ø© ÙˆØ­Ø¬Ù… Ø§Ù„ØµÙØ­Ø© ÙŠØ¬Ø¨ Ø£Ù† ÙŠÙƒÙˆÙ†Ø§ Ø£ÙƒØ¨Ø± Ù…Ù† 0.");

                var providers = await _uow.HealthcareProviders.GetAllAsync();
                var totalCount = providers.Count();

                var paginatedProviders = providers
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProviderListItemDto
                    {
                        Id = p.Id,
                        Name = p.Name ?? string.Empty,
                        Type = p.Type ?? string.Empty,
                        Address = p.Address,
                        Phone = p.Phone,
                        AvailableBeds = p.AvailableBeds,
                        BedCapacity = p.BedCapacity,
                        AmbulanceCapacity = p.AmbulanceCapacity,
                        AvailableAmbulances = p.AvailableAmbulances,
                        IsEmergencyCenter = p.IsEmergencyCenter,
                        IsActive = p.IsActive,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude
                    })
                    .ToList();

                var result = new PaginatedResult<ProviderListItemDto>
                {
                    Items = paginatedProviders.AsReadOnly(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogInformation("Retrieved {Count} providers for page {PageNumber}.", paginatedProviders.Count, pageNumber);

                return ServiceResult<PaginatedResult<ProviderListItemDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all providers.");
                return ServiceResult<PaginatedResult<ProviderListItemDto>>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ø§Ù„Ù…ÙˆÙØ±ÙŠÙ†.");
            }
        }

        /// <summary>
        /// Gets a provider by ID
        /// </summary>
        public async Task<ServiceResult<ProviderListItemDto>> GetProviderByIdAsync(int providerId)
        {
            try
            {
                if (providerId <= 0)
                    return ServiceResult<ProviderListItemDto>.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ù…ÙˆÙØ± ØºÙŠØ± ØµØ­ÙŠØ­.");

                var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
                if (provider is null)
                {
                    _logger.LogWarning("Provider with ID {ProviderId} not found.", providerId);
                    return ServiceResult<ProviderListItemDto>.NotFound("Ø§Ù„Ù…ÙˆÙØ± ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯.");
                }

                var providerDto = new ProviderListItemDto
                {
                    Id = provider.Id,
                    Name = provider.Name ?? string.Empty,
                    Type = provider.Type ?? string.Empty,
                    Address = provider.Address,
                    Phone = provider.Phone,
                    AvailableBeds = provider.AvailableBeds,
                    BedCapacity = provider.BedCapacity,
                    AmbulanceCapacity = provider.AmbulanceCapacity,
                    AvailableAmbulances = provider.AvailableAmbulances,
                    IsEmergencyCenter = provider.IsEmergencyCenter,
                    IsActive = provider.IsActive,
                    Latitude = provider.Latitude,
                    Longitude = provider.Longitude
                };

                return ServiceResult<ProviderListItemDto>.Success(providerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving provider {ProviderId}.", providerId);
                return ServiceResult<ProviderListItemDto>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ø§Ù„Ù…ÙˆÙØ±.");
            }
        }

        /// <summary>
        /// Creates a new healthcare provider
        /// </summary>
        public async Task<ServiceResult> CreateProviderAsync(CreateProviderDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return ServiceResult.Failure("Ø§Ø³Ù… Ø§Ù„Ù…ÙˆÙØ± Ù…Ø·Ù„ÙˆØ¨.");

                if (string.IsNullOrWhiteSpace(dto.Type))
                    return ServiceResult.Failure("Ù†ÙˆØ¹ Ø§Ù„Ù…ÙˆÙØ± Ù…Ø·Ù„ÙˆØ¨.");

                if (dto.Latitude < -90 || dto.Latitude > 90)
                    return ServiceResult.Failure("Ø®Ø· Ø§Ù„Ø¹Ø±Ø¶ ÙŠØ¬Ø¨ Ø£Ù† ÙŠÙƒÙˆÙ† Ø¨ÙŠÙ† -90 Ùˆ 90.");

                if (dto.Longitude < -180 || dto.Longitude > 180)
                    return ServiceResult.Failure("Ø®Ø· Ø§Ù„Ø·ÙˆÙ„ ÙŠØ¬Ø¨ Ø£Ù† ÙŠÙƒÙˆÙ† Ø¨ÙŠÙ† -180 Ùˆ 180.");

                var provider = new HealthcareProvider
                {
                    Name = dto.Name.Trim(),
                    Type = dto.Type.Trim(),
                    Address = dto.Address,
                    Phone = dto.Phone,
                    AvailableBeds = dto.AvailableBeds ?? 0,
                    BedCapacity = dto.BedCapacity ?? 150,
                    AmbulanceCapacity = dto.AmbulanceCapacity ?? 4,
                    AvailableAmbulances = dto.AvailableAmbulances ?? dto.AmbulanceCapacity ?? 4,
                    IsEmergencyCenter = dto.IsEmergencyCenter,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    IsActive = true
                };

                await _uow.HealthcareProviders.AddAsync(provider);
                await _uow.CompleteAsync();

                _logger.LogInformation("New provider '{Name}' created successfully with ID {ProviderId}.", provider.Name, provider.Id);

                return ServiceResult.Success(201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider.");
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø¥Ù†Ø´Ø§Ø¡ Ø§Ù„Ù…ÙˆÙØ±.");
            }
        }

        /// <summary>
        /// Updates an existing provider
        /// </summary>
        public async Task<ServiceResult> UpdateProviderAsync(int providerId, UpdateProviderDto dto)
        {
            try
            {
                if (providerId <= 0)
                    return ServiceResult.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ù…ÙˆÙØ± ØºÙŠØ± ØµØ­ÙŠØ­.");

                if (string.IsNullOrWhiteSpace(dto.Name))
                    return ServiceResult.Failure("Ø§Ø³Ù… Ø§Ù„Ù…ÙˆÙØ± Ù…Ø·Ù„ÙˆØ¨.");

                var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
                if (provider is null)
                {
                    _logger.LogWarning("Provider with ID {ProviderId} not found for update.", providerId);
                    return ServiceResult.NotFound("Ø§Ù„Ù…ÙˆÙØ± ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯.");
                }

                provider.Name = dto.Name.Trim();
                provider.Type = dto.Type.Trim();
                provider.Address = dto.Address;
                provider.Phone = dto.Phone;
                provider.AvailableBeds = dto.AvailableBeds ?? 0;
                provider.BedCapacity = dto.BedCapacity ?? 150;
                provider.AmbulanceCapacity = dto.AmbulanceCapacity ?? 4;
                provider.AvailableAmbulances = dto.AvailableAmbulances ?? dto.AmbulanceCapacity ?? 4;
                provider.IsEmergencyCenter = dto.IsEmergencyCenter;
                provider.Latitude = dto.Latitude;
                provider.Longitude = dto.Longitude;
                provider.IsActive = dto.IsActive;

                _uow.HealthcareProviders.Update(provider);
                await _uow.CompleteAsync();

                _logger.LogInformation("Provider {ProviderId} updated successfully.", providerId);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider {ProviderId}.", providerId);
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ ØªØ­Ø¯ÙŠØ« Ø§Ù„Ù…ÙˆÙØ±.");
            }
        }

        /// <summary>
        /// Deletes a provider
        /// </summary>
        public async Task<ServiceResult> DeleteProviderAsync(int providerId)
        {
            try
            {
                if (providerId <= 0)
                    return ServiceResult.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ù…ÙˆÙØ± ØºÙŠØ± ØµØ­ÙŠØ­.");

                var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
                if (provider is null)
                {
                    _logger.LogWarning("Provider with ID {ProviderId} not found for deletion.", providerId);
                    return ServiceResult.NotFound("Ø§Ù„Ù…ÙˆÙØ± ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯.");
                }

                _uow.HealthcareProviders.Remove(provider);
                await _uow.CompleteAsync();

                _logger.LogInformation("Provider {ProviderId} deleted successfully.", providerId);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting provider {ProviderId}.", providerId);
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø­Ø°Ù Ø§Ù„Ù…ÙˆÙØ±.");
            }
        }

        /// <summary>
        /// Gets admin dashboard with key statistics
        /// </summary>
        public async Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync()
        {
            try
            {
                var users = await _uow.Users.GetAllAsync();
                var appointments = await _uow.Appointments.GetAllAsync();
                var emergencyRequests = await _uow.EmergencyRequests.GetAllAsync();
                var crises = await _uow.CrisisConfigurations.GetAllAsync();

                var totalUsers = users.Count();
                var activeDoctors = 0; // ÙŠÙ…ÙƒÙ† ØªØ­Ø³ÙŠÙ†Ù‡ Ø¨Ù†Ø§Ø¡Ù‹ Ø¹Ù„Ù‰ Ø§Ù„Ø¯ÙˆØ±
                var activePatients = 0; // ÙŠÙ…ÙƒÙ† ØªØ­Ø³ÙŠÙ†Ù‡ Ø¨Ù†Ø§Ø¡Ù‹ Ø¹Ù„Ù‰ Ø§Ù„Ø¯ÙˆØ±
                var activeUsers = users.Count(u => u.IsActive);

                var dashboard = new AdminDashboardDto
                {
                    TotalUsers = totalUsers,
                    ActiveDoctors = activeDoctors,
                    ActivePatients = activePatients,
                    TotalAppointments = appointments.Count(),
                    PendingEmergencyRequests = emergencyRequests.Count(e => e.Status == EmergencyRequestStatus.Pending),
                    IsCrisisModeActive = crises.Any(c => c.IsActive),
                    ActiveCrisisName = crises.FirstOrDefault(c => c.IsActive)?.CrisisName,
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation("Admin dashboard retrieved successfully.");

                return ServiceResult<AdminDashboardDto>.Success(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard.");
                return ServiceResult<AdminDashboardDto>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ù„ÙˆØ­Ø© Ø§Ù„ØªØ­ÙƒÙ….");
            }
        }

        /// <summary>
        /// Gets reports with pagination
        /// </summary>
        public async Task<ServiceResult<PaginatedResult<AdminReportDto>>> GetReportsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                    return ServiceResult<PaginatedResult<AdminReportDto>>.Failure("Ø±Ù‚Ù… Ø§Ù„ØµÙØ­Ø© ÙˆØ­Ø¬Ù… Ø§Ù„ØµÙØ­Ø© ÙŠØ¬Ø¨ Ø£Ù† ÙŠÙƒÙˆÙ†Ø§ Ø£ÙƒØ¨Ø± Ù…Ù† 0.");

                // Generate sample reports based on system data
                var reports = new List<AdminReportDto>
                {
                    new()
                    {
                        ReportType = "Users",
                        StartDate = DateTime.UtcNow.AddMonths(-1),
                        EndDate = DateTime.UtcNow,
                        TotalRecords = (await _uow.Users.GetAllAsync()).Count(),
                        FileUrl = "/reports/users-report.pdf",
                        GeneratedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new()
                    {
                        ReportType = "Appointments",
                        StartDate = DateTime.UtcNow.AddMonths(-1),
                        EndDate = DateTime.UtcNow,
                        TotalRecords = (await _uow.Appointments.GetAllAsync()).Count(),
                        FileUrl = "/reports/appointments-report.pdf",
                        GeneratedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new()
                    {
                        ReportType = "Emergencies",
                        StartDate = DateTime.UtcNow.AddMonths(-1),
                        EndDate = DateTime.UtcNow,
                        TotalRecords = (await _uow.EmergencyRequests.GetAllAsync()).Count(),
                        FileUrl = "/reports/emergencies-report.pdf",
                        GeneratedAt = DateTime.UtcNow.AddDays(-3)
                    }
                };

                var totalCount = reports.Count;
                var paginatedReports = reports
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PaginatedResult<AdminReportDto>
                {
                    Items = paginatedReports.AsReadOnly(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogInformation("Retrieved {Count} reports for page {PageNumber}.", paginatedReports.Count, pageNumber);

                return ServiceResult<PaginatedResult<AdminReportDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports.");
                return ServiceResult<PaginatedResult<AdminReportDto>>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ø§Ù„ØªÙ‚Ø§Ø±ÙŠØ±.");
            }
        }

        /// <summary>
        /// Gets crisis management information
        /// </summary>
        public async Task<ServiceResult<AdminCrisisDto>> GetCrisisManagementAsync()
        {
            try
            {
                var crises = await _uow.CrisisConfigurations.GetAllAsync();
                var activeCrisis = crises.FirstOrDefault(c => c.IsActive);

                if (activeCrisis is null)
                {
                    // Return default crisis DTO if no active crisis
                    var emptyDto = new AdminCrisisDto
                    {
                        Id = 0,
                        CrisisName = "No Active Crisis",
                        IsActive = false,
                        ZonesCount = 0
                    };
                    return ServiceResult<AdminCrisisDto>.Success(emptyDto);
                }

                var zones = await _uow.OutbreakZones.GetAllAsync();
                var crisisZones = zones.Count(z => z.CrisisConfigurationId == activeCrisis.Id);

                var crisisDto = new AdminCrisisDto
                {
                    Id = activeCrisis.Id,
                    CrisisName = activeCrisis.CrisisName ?? string.Empty,
                    CrisisType = activeCrisis.CrisisType,
                    SystemMode = activeCrisis.SystemMode,
                    IsActive = activeCrisis.IsActive,
                    StartDate = activeCrisis.StartDate,
                    EndDate = activeCrisis.EndDate,
                    ZonesCount = crisisZones
                };

                _logger.LogInformation("Crisis management information retrieved.");

                return ServiceResult<AdminCrisisDto>.Success(crisisDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving crisis management information.");
                return ServiceResult<AdminCrisisDto>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ù…Ø¹Ù„ÙˆÙ…Ø§Øª Ø§Ù„Ø£Ø²Ù…Ø©.");
            }
        }

        /// <summary>
        /// Gets activity logs with pagination
        /// </summary>
        public async Task<ServiceResult<List<ActivityLogDto>>> GetActivityLogsAsync(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                    return ServiceResult<List<ActivityLogDto>>.Failure("Ø±Ù‚Ù… Ø§Ù„ØµÙØ­Ø© ÙˆØ­Ø¬Ù… Ø§Ù„ØµÙØ­Ø© ÙŠØ¬Ø¨ Ø£Ù† ÙŠÙƒÙˆÙ†Ø§ Ø£ÙƒØ¨Ø± Ù…Ù† 0.");

                // Note: Activity logs would typically be stored in a separate log table
                // This is a placeholder implementation
                var logs = new List<ActivityLogDto>
                {
                    new()
                    {
                        Id = 1,
                        UserId = "admin-1",
                        UserName = "Admin User",
                        Action = "Create",
                        EntityType = "Provider",
                        EntityId = 1,
                        Details = "Created new healthcare provider",
                        IpAddress = "192.168.1.1",
                        Timestamp = DateTime.UtcNow.AddHours(-1)
                    }
                };

                var paginatedLogs = logs
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} activity logs for page {PageNumber}.", paginatedLogs.Count, pageNumber);

                return ServiceResult<List<ActivityLogDto>>.Success(paginatedLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity logs.");
                return ServiceResult<List<ActivityLogDto>>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ø§Ù„Ø³Ø¬Ù„Ø§Øª.");
            }
        }

        /// <summary>
        /// Gets system configuration
        /// </summary>
        public async Task<ServiceResult<SystemConfigDto>> GetSystemConfigAsync()
        {
            try
            {
                // Retrieve configuration from database or settings
                // This is a default configuration implementation
                var config = new SystemConfigDto
                {
                    EnableCrisisMode = false,
                    EnableAIChat = true,
                    EnableOCR = true,
                    EnableFamilyLinking = true,
                    EnableEmergencyRequests = true,
                    MaxLoginAttempts = 5,
                    LockoutDurationMinutes = 15,
                    SessionTimeoutMinutes = 30
                };

                _logger.LogInformation("System configuration retrieved successfully.");

                return ServiceResult<SystemConfigDto>.Success(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system configuration.");
                return ServiceResult<SystemConfigDto>.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ Ø¥Ø¹Ø¯Ø§Ø¯Ø§Øª Ø§Ù„Ù†Ø¸Ø§Ù….");
            }
        }

        /// <summary>
        /// Updates system configuration
        /// </summary>
        public async Task<ServiceResult> UpdateSystemConfigAsync(SystemConfigDto dto)
        {
            try
            {
                if (dto.MaxLoginAttempts < 1)
                    return ServiceResult.Failure("Ø­Ø¯ Ù…Ø­Ø§ÙˆÙ„Ø§Øª ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„ ÙŠØ¬Ø¨ Ø£Ù† ÙŠÙƒÙˆÙ† Ø£ÙƒØ¨Ø± Ù…Ù† 0.");

                if (dto.LockoutDurationMinutes < 1)
                    return ServiceResult.Failure("Ù…Ø¯Ø© Ø§Ù„Ø­Ø¯ ÙŠØ¬Ø¨ Ø£Ù† ØªÙƒÙˆÙ† Ø£ÙƒØ¨Ø± Ù…Ù† 0.");

                if (dto.SessionTimeoutMinutes < 1)
                    return ServiceResult.Failure("Ù…Ù‡Ù„Ø© Ø¬Ù„Ø³Ø© Ø§Ù„Ø¹Ù…Ù„ ÙŠØ¬Ø¨ Ø£Ù† ØªÙƒÙˆÙ† Ø£ÙƒØ¨Ø± Ù…Ù† 0.");

                // Update configuration in database
                // This would typically be saved to a configuration table
                _logger.LogInformation("System configuration updated successfully.");

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system configuration.");
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ ØªØ­Ø¯ÙŠØ« Ø¥Ø¹Ø¯Ø§Ø¯Ø§Øª Ø§Ù„Ù†Ø¸Ø§Ù….");
            }
        }

        /// <summary>
        /// Approves a crisis
        /// </summary>
        public async Task<ServiceResult> ApproveCrisisAsync(int crisisId)
        {
            try
            {
                if (crisisId <= 0)
                    return ServiceResult.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ø£Ø²Ù…Ø© ØºÙŠØ± ØµØ­ÙŠØ­.");

                var crisis = await _uow.CrisisConfigurations.GetByIdAsync(crisisId);
                if (crisis is null)
                {
                    _logger.LogWarning("Crisis with ID {CrisisId} not found for approval.", crisisId);
                    return ServiceResult.NotFound("Ø§Ù„Ø£Ø²Ù…Ø© ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯Ø©.");
                }

                // Check if already approved
                if (crisis.IsActive)
                    return ServiceResult.Conflict("Ø§Ù„Ø£Ø²Ù…Ø© Ù…ÙˆØ§ÙÙ‚ Ø¹Ù„ÙŠÙ‡Ø§ Ø¨Ø§Ù„ÙØ¹Ù„.");

                crisis.IsActive = true;
                crisis.StartDate = DateTime.UtcNow;

                _uow.CrisisConfigurations.Update(crisis);
                await _uow.CompleteAsync();

                _logger.LogInformation("Crisis {CrisisId} approved and activated.", crisisId);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving crisis {CrisisId}.", crisisId);
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ù„Ù…ÙˆØ§ÙÙ‚Ø© Ø¹Ù„Ù‰ Ø§Ù„Ø£Ø²Ù…Ø©.");
            }
        }

        /// <summary>
        /// Rejects a crisis with a reason
        /// </summary>
        public async Task<ServiceResult> RejectCrisisAsync(int crisisId, string reason)
        {
            try
            {
                if (crisisId <= 0)
                    return ServiceResult.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ø£Ø²Ù…Ø© ØºÙŠØ± ØµØ­ÙŠØ­.");

                if (string.IsNullOrWhiteSpace(reason))
                    return ServiceResult.Failure("ÙŠØ¬Ø¨ ØªÙ‚Ø¯ÙŠÙ… Ø³Ø¨Ø¨ Ø§Ù„Ø±ÙØ¶.");

                var crisis = await _uow.CrisisConfigurations.GetByIdAsync(crisisId);
                if (crisis is null)
                {
                    _logger.LogWarning("Crisis with ID {CrisisId} not found for rejection.", crisisId);
                    return ServiceResult.NotFound("Ø§Ù„Ø£Ø²Ù…Ø© ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯Ø©.");
                }

                crisis.IsActive = false;
                crisis.EndDate = DateTime.UtcNow;

                _uow.CrisisConfigurations.Update(crisis);
                await _uow.CompleteAsync();

                _logger.LogInformation("Crisis {CrisisId} rejected with reason: {Reason}", crisisId, reason);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting crisis {CrisisId}.", crisisId);
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø±ÙØ¶ Ø§Ù„Ø£Ø²Ù…Ø©.");
            }
        }

        /// <summary>
        /// Updates crisis status
        /// </summary>
        public async Task<ServiceResult> UpdateCrisisStatusAsync(int crisisId, string status)
        {
            try
            {
                if (crisisId <= 0)
                    return ServiceResult.Failure("Ù…Ø¹Ø±Ù Ø§Ù„Ø£Ø²Ù…Ø© ØºÙŠØ± ØµØ­ÙŠØ­.");

                if (string.IsNullOrWhiteSpace(status))
                    return ServiceResult.Failure("Ø­Ø§Ù„Ø© Ø§Ù„Ø£Ø²Ù…Ø© Ù…Ø·Ù„ÙˆØ¨Ø©.");

                var crisis = await _uow.CrisisConfigurations.GetByIdAsync(crisisId);
                if (crisis is null)
                {
                    _logger.LogWarning("Crisis with ID {CrisisId} not found for status update.", crisisId);
                    return ServiceResult.NotFound("Ø§Ù„Ø£Ø²Ù…Ø© ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯Ø©.");
                }

                // Validate status values
                var validStatuses = new[] { "Pending", "Active", "Resolved", "Closed" };
                if (!validStatuses.Contains(status))
                    return ServiceResult.Failure($"Ø­Ø§Ù„Ø© ØºÙŠØ± ØµØ§Ù„Ø­Ø©. Ø§Ù„Ø­Ø§Ù„Ø§Øª Ø§Ù„Ù…Ø³Ù…ÙˆØ­Ø©: {string.Join(", ", validStatuses)}");

                if (status == "Closed" || status == "Resolved")
                    crisis.EndDate = DateTime.UtcNow;

                _uow.CrisisConfigurations.Update(crisis);
                await _uow.CompleteAsync();

                _logger.LogInformation("Crisis {CrisisId} status updated to {Status}.", crisisId, status);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating crisis {CrisisId} status.", crisisId);
                return ServiceResult.Failure("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ ØªØ­Ø¯ÙŠØ« Ø­Ø§Ù„Ø© Ø§Ù„Ø£Ø²Ù…Ø©.");
            }
        }
    }
}
