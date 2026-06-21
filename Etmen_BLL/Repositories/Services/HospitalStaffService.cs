namespace Etmen_BLL.Repositories.Services
{
    public sealed class HospitalStaffService : IHospitalStaffService
    {
        private static readonly EmergencyRequestStatus[] QueueStatuses =
        [
            EmergencyRequestStatus.Pending,
            EmergencyRequestStatus.Accepted,
            EmergencyRequestStatus.Escalated
        ];

        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public HospitalStaffService(
            IUnitOfWork uow,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _uow = uow;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<ServiceResult<HospitalStaffQueueDto>> GetQueueAsync(int? providerId = null)
        {
            HealthcareProvider? provider = null;
            if (providerId.HasValue)
            {
                provider = await _uow.HealthcareProviders.GetByIdAsync(providerId.Value);
                if (provider is null)
                    return ServiceResult<HospitalStaffQueueDto>.NotFound("Healthcare provider was not found.");
                if (!provider.IsEmergencyCenter)
                    return ServiceResult<HospitalStaffQueueDto>.Failure("Provider is not configured as an emergency center.");
            }

            var query = _uow.EmergencyRequests.Table
                .Include(e => e.PatientProfile)
                    .ThenInclude(p => p.ApplicationUser)
                .Include(e => e.HealthcareProvider)
                .Where(e => QueueStatuses.Contains(e.Status));

            var requestsRaw = await query.ToListAsync();
            var requests = new List<EmergencyRequest>();

            if (providerId.HasValue)
            {
                // Fetch all active emergency providers to calculate distance
                var emergencyProviders = await _uow.HealthcareProviders.Table
                    .Where(p => p.IsActive && p.IsEmergencyCenter)
                    .ToListAsync();

                foreach (var req in requestsRaw)
                {
                    // 1. Direct requests to this hospital are always visible
                    if (req.HealthcareProviderId == providerId.Value)
                    {
                        requests.Add(req);
                        continue;
                    }

                    // 2. Direct requests assigned to other hospitals are not visible
                    if (req.HealthcareProviderId.HasValue)
                    {
                        continue;
                    }

                    // 3. For general requests (HealthcareProviderId == null), apply Tiered Routing
                    if (req.Status == EmergencyRequestStatus.Accepted)
                    {
                        continue;
                    }

                    if (req.Latitude.HasValue && req.Longitude.HasValue)
                    {
                        double myDistance = GeoHelper.CalculateDistanceKm(
                            (double)req.Latitude.Value, (double)req.Longitude.Value,
                            (double)provider!.Latitude, (double)provider!.Longitude);

                        // Find distances to all hospitals that currently have beds
                        var providersWithBeds = emergencyProviders
                            .Where(p => p.AvailableBeds > 0)
                            .Select(p => new
                            {
                                Provider = p,
                                Distance = GeoHelper.CalculateDistanceKm(
                                    (double)req.Latitude.Value, (double)req.Longitude.Value,
                                    (double)p.Latitude, (double)p.Longitude)
                            })
                            .ToList();

                        bool hasTier1Hospitals = providersWithBeds.Any(p => p.Distance <= 30.0);
                        bool hasTier2Hospitals = providersWithBeds.Any(p => p.Distance <= 100.0);

                        if (hasTier1Hospitals)
                        {
                            // Tier 1: Only show to hospitals <= 30km
                            if (myDistance <= 30.0)
                            {
                                requests.Add(req);
                            }
                        }
                        else if (hasTier2Hospitals)
                        {
                            // Tier 2: Show to hospitals <= 100km
                            if (myDistance <= 100.0)
                            {
                                requests.Add(req);
                            }
                        }
                        else
                        {
                            // Tier 3: Show to all (no nearby hospitals have beds)
                            requests.Add(req);
                        }
                    }
                    else
                    {
                        // Fallback if request has no GPS coordinates
                        requests.Add(req);
                    }
                }
            }
            else
            {
                requests = requestsRaw;
            }

            requests = requests
                .OrderByDescending(e => e.IsAutoGenerated)
                .ThenByDescending(e => e.PriorityScore)
                .ThenByDescending(e => e.Status == EmergencyRequestStatus.Escalated)
                .ThenBy(e => e.RequestedAt)
                .ToList();

            var dto = new HospitalStaffQueueDto
            {
                ProviderId = provider?.Id,
                ProviderName = provider?.Name,
                AvailableBeds = provider?.AvailableBeds,
                BedCapacity = provider?.BedCapacity ?? 150,
                AmbulanceCapacity = provider?.AmbulanceCapacity ?? 4,
                AvailableAmbulances = provider?.AvailableAmbulances ?? 4,
                PendingCount = requests.Count(e => e.Status == EmergencyRequestStatus.Pending),
                AcceptedCount = requests.Count(e => e.Status == EmergencyRequestStatus.Accepted),
                EscalatedCount = requests.Count(e => e.Status == EmergencyRequestStatus.Escalated),
                GeneratedAt = DateTime.UtcNow,
                Items = requests.Select(MapQueueItem).ToList()
            };

            return ServiceResult<HospitalStaffQueueDto>.Success(dto);
        }

        public async Task<ServiceResult<HospitalStaffEmergencyDetailDto>> GetRequestDetailAsync(int requestId, int? providerId = null)
        {
            if (requestId <= 0)
                return ServiceResult<HospitalStaffEmergencyDetailDto>.Failure("Request id is required.");

            var request = await _uow.EmergencyRequests.Table
                .Include(e => e.PatientProfile)
                    .ThenInclude(p => p.ApplicationUser)
                .Include(e => e.HealthcareProvider)
                .FirstOrDefaultAsync(e => e.Id == requestId);

            if (request is null)
                return ServiceResult<HospitalStaffEmergencyDetailDto>.NotFound("Emergency request was not found.");

            if (providerId.HasValue && request.HealthcareProviderId.HasValue && request.HealthcareProviderId != providerId.Value)
                return ServiceResult<HospitalStaffEmergencyDetailDto>.Forbidden("This emergency request is assigned to another provider.");

            return ServiceResult<HospitalStaffEmergencyDetailDto>.Success(MapDetail(request));
        }

        public async Task<ServiceResult> RespondToRequestAsync(HospitalStaffEmergencyRespondDto dto)
        {
            return await RespondToRequestAsync(dto, null);
        }

        public async Task<ServiceResult> RespondToRequestAsync(HospitalStaffEmergencyRespondDto dto, string? respondedByUserId)
        {
            var errors = ValidateResponse(dto).ToList();
            if (errors.Count > 0)
                return ServiceResult.Failure(errors);

            if (!Enum.TryParse<EmergencyRequestStatus>(dto.Status, true, out var requestedStatus))
                return ServiceResult.Failure("Invalid emergency request status.");

            if (!IsStaffActionStatus(requestedStatus))
                return ServiceResult.Failure("Hospital staff can only accept, reject, escalate, complete, or cancel emergency requests.");

            var request = await _uow.EmergencyRequests.GetByIdAsync(dto.RequestId);

            if (request is null)
                return ServiceResult.NotFound("Emergency request was not found.");

            var provider = await _uow.HealthcareProviders.GetByIdAsync(dto.ProviderId);
            if (provider is null)
                return ServiceResult.NotFound("Healthcare provider was not found.");

            var transitionError = ValidateTransition(request.Status, requestedStatus, dto.ResponseNotes);
            if (transitionError is not null)
                return ServiceResult.Failure(transitionError);

            ApplyResponse(request, provider, requestedStatus, dto.ResponseNotes);

            // Stage the request and provider updates first
            _uow.EmergencyRequests.Update(request);
            _uow.HealthcareProviders.Update(provider);

            if (!string.IsNullOrEmpty(respondedByUserId))
            {
                // Use the userId as a raw FK to avoid tracking conflicts with IdentityUser
                request.RespondedByUserId = respondedByUserId;

                // Stage the activity log without an intermediate SaveChanges
                var staffProfile = await _uow.StaffProfiles.Table
                    .FirstOrDefaultAsync(sp => sp.ApplicationUserId == respondedByUserId);
                if (staffProfile != null)
                {
                    await StageActivityLogAsync(staffProfile.Id, "RespondToRequest",
                        $"قام بالرد على حالة الطوارئ رقم {request.Id} بالحالة {requestedStatus}");
                }
            }

            // Handle optional doctor assignment atomically (FK assignment only — no navigation load)
            if (!string.IsNullOrEmpty(dto.AssignedDoctorUserId))
            {
                request.AssignedDoctorUserId = dto.AssignedDoctorUserId;
                request.DoctorAssignedAt = DateTime.UtcNow;
                request.DoctorsNotified = true;
                request.DoctorsNotifiedAt = DateTime.UtcNow;
            }

            try
            {
                // Single save for all staged changes
                await _uow.CompleteAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult.Failure("لقد تغير عدد الأسرة المتاحة في المستشفى من قبل مستخدم آخر في نفس الوقت. يرجى محاولة قبول الطلب مرة أخرى.");
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateBedsAsync(HospitalStaffBedsUpdateDto dto)
        {
            if (dto is null)
                return ServiceResult.Failure("Payload is required.");
            if (dto.ProviderId <= 0)
                return ServiceResult.Failure("Provider id is required.");
            if (dto.AvailableBeds < 0)
                return ServiceResult.Failure("Available beds count cannot be negative.");

            var provider = await _uow.HealthcareProviders.GetByIdAsync(dto.ProviderId);
            if (provider is null)
                return ServiceResult.NotFound("Healthcare provider was not found.");

            provider.AvailableBeds = dto.AvailableBeds;
            _uow.HealthcareProviders.Update(provider);
            await _uow.CompleteAsync();

            return ServiceResult.Success();
        }

        // ── Staff Management Implementations ───────────────────────────────────

        public async Task<ServiceResult<List<StaffProfileDto>>> GetStaffMembersAsync(int providerId)
        {
            var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
            if (provider == null)
                return ServiceResult<List<StaffProfileDto>>.NotFound("Provider not found.");

            var profiles = await _uow.StaffProfiles.Table
                .Include(sp => sp.ApplicationUser)
                .Include(sp => sp.HealthcareProvider)
                .Where(sp => sp.HealthcareProviderId == providerId)
                .ToListAsync();

            var dtos = profiles.Select(p => new StaffProfileDto
            {
                Id = p.Id,
                UserId = p.ApplicationUserId,
                Email = p.ApplicationUser.Email ?? string.Empty,
                FullName = $"{p.ApplicationUser.FirstName} {p.ApplicationUser.LastName}".Trim(),
                ProviderId = p.HealthcareProviderId,
                ProviderName = p.HealthcareProvider.Name,
                RoleType = p.RoleType,
                ActiveShift = p.ActiveShift,
                IsInvitationAccepted = p.IsInvitationAccepted,
                InvitationToken = p.InvitationToken,
                InvitationTokenExpiry = p.InvitationTokenExpiry,
                JoinedAt = p.JoinedAt
            }).ToList();

            return ServiceResult<List<StaffProfileDto>>.Success(dtos);
        }

        public async Task<ServiceResult> InviteStaffAsync(int providerId, string email, StaffRoleType role, StaffShiftType shift, string senderUserId, string inviteUrlPrefix)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult.Failure("Email is required.");

            var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
            if (provider == null)
                return ServiceResult.NotFound("Provider not found.");

            var user = await _userManager.FindByEmailAsync(email);
            bool isNewUser = false;
            string tempPassword = "";

            if (user == null)
            {
                isNewUser = true;
                tempPassword = "Staff@" + Guid.NewGuid().ToString("N").Substring(0, 8);
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = "موظف",
                    LastName = "جديد",
                    EmailConfirmed = true,
                    IsActive = true,
                    MustChangePassword = true
                };

                var createResult = await _userManager.CreateAsync(user, tempPassword);
                if (!createResult.Succeeded)
                {
                    return ServiceResult.Failure(string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }

                await _userManager.AddToRoleAsync(user, "HospitalStaff");
            }
            else
            {
                var existingProfile = await _uow.StaffProfiles.Table.FirstOrDefaultAsync(sp => sp.ApplicationUserId == user.Id);
                if (existingProfile != null)
                {
                    if (existingProfile.HealthcareProviderId == providerId)
                    {
                        if (existingProfile.IsInvitationAccepted)
                        {
                            return ServiceResult.Failure("هذا المستخدم هو موظف نشط بالفعل في هذه المنشأة.");
                        }
                        else
                        {
                            // Update the existing pending invitation with new token, role, and shift
                            var updatedToken = Guid.NewGuid().ToString("N");
                            existingProfile.InvitationToken = updatedToken;
                            existingProfile.InvitationTokenExpiry = DateTime.UtcNow.AddDays(7);
                            existingProfile.RoleType = role;
                            existingProfile.ActiveShift = shift;
                            existingProfile.InvitationSenderUserId = senderUserId;

                            _uow.StaffProfiles.Update(existingProfile);
                            await _uow.CompleteAsync();

                            // Use the updated token for email link
                            var updateInviteLink = $"{inviteUrlPrefix}?token={updatedToken}";
                            string updateSubject = "تحديث دعوة الانضمام إلى طاقم عمل منصة اطمئن";
                            string updateRoleArabic = role == StaffRoleType.Receptionist ? "موظف استقبال" : "موظف طوارئ وفرز";
                            string updateShiftArabic = shift switch
                            {
                                StaffShiftType.Morning => "الوردية الصباحية",
                                StaffShiftType.Evening => "الوردية المسائية",
                                StaffShiftType.Night => "الوردية الليلية",
                                _ => "غير محدد"
                            };

                            string updateBody = $@"
                                <div dir='rtl' style='font-family:tahoma,arial,sans-serif;padding:20px;border:1px solid #eee;border-radius:12px;max-width:600px;margin:auto;'>
                                    <h3 style='color:#1a6fbf;'>تحديث دعوة الانضمام - منصة اطمئن</h3>
                                    <p>تم تحديث دعوة انضمامك لطاقم العمل في المنشأة <strong>{provider.Name}</strong> كـ <strong>{updateRoleArabic}</strong> ({updateShiftArabic}).</p>
                                    <p>يرجى النقر على الرابط أدناه لتفعيل حسابك وتأكيد قبول الدعوة:</p>
                                    <p style='text-align:center;'><a href='{updateInviteLink}' style='background-color:#1a6fbf;color:white;padding:12px 24px;text-decoration:none;border-radius:24px;display:inline-block;font-weight:bold;'>قبول الدعوة وتفعيل الحساب</a></p>
                                    <p style='font-size:0.8rem;color:#888;'>هذا الرابط صالح لمدة 7 أيام فقط.</p>
                                </div>
                            ";

                            await _emailService.SendEmailAsync(email, email, updateSubject, updateBody);
                            await LogActivityInternalAsync(existingProfile.Id, "Re-Invited", $"تم إعادة إرسال وتحديث دعوة الانضمام للموظف بواسطة المستخدم {senderUserId}");
                            
                            return ServiceResult.Success();
                        }
                    }
                    else
                    {
                        return ServiceResult.Failure("هذا المستخدم مضاف بالفعل كموظف في منشأة أخرى.");
                    }
                }

                if (!await _userManager.IsInRoleAsync(user, "HospitalStaff"))
                {
                    await _userManager.AddToRoleAsync(user, "HospitalStaff");
                }
            }

            var token = Guid.NewGuid().ToString("N");
            var profile = new StaffProfile
            {
                ApplicationUserId = user.Id,
                HealthcareProviderId = providerId,
                RoleType = role,
                ActiveShift = shift,
                IsInvitationAccepted = false,
                InvitationSenderUserId = senderUserId,
                InvitationToken = token,
                InvitationTokenExpiry = DateTime.UtcNow.AddDays(7),
                JoinedAt = null
            };

            await _uow.StaffProfiles.AddAsync(profile);
            await _uow.CompleteAsync();

            var inviteLink = $"{inviteUrlPrefix}?token={token}";
            string subject = "دعوة للانضمام إلى طاقم عمل منصة اطمئن";
            string roleArabic = role == StaffRoleType.Receptionist ? "موظف استقبال" : "موظف طوارئ وفرز";
            string shiftArabic = shift switch
            {
                StaffShiftType.Morning => "الوردية الصباحية",
                StaffShiftType.Evening => "الوردية المسائية",
                StaffShiftType.Night => "الوردية الليلية",
                _ => "غير محدد"
            };

            string body = $@"
                <div dir='rtl' style='font-family:tahoma,arial,sans-serif;padding:20px;border:1px solid #eee;border-radius:12px;max-width:600px;margin:auto;'>
                    <h3 style='color:#1a6fbf;'>مرحباً بك في منصة اطمئن!</h3>
                    <p>لقد تمت دعوتك للانضمام إلى طاقم العمل في <strong>{provider.Name}</strong> كـ <strong>{roleArabic}</strong> ({shiftArabic}).</p>
                    {(isNewUser ? $"<p style='background-color:#f9f9f9;padding:15px;border-radius:8px;'>بيانات الدخول المؤقتة الخاصة بك هي:<br/><strong>البريد الإلكتروني:</strong> {email}<br/><strong>كلمة المرور المؤقتة:</strong> {tempPassword}</p><p style='color:red;'><em>ملاحظة هامة: سيُطلب منك تغيير كلمة المرور فور تسجيل الدخول الأول لضمان أمان حسابك.</em></p>" : "")}
                    <p>يرجى النقر على الرابط أدناه لتسجيل الدخول وتفعيل حسابك وقبول الدعوة:</p>
                    <p style='text-align:center;'><a href='{inviteLink}' style='background-color:#1a6fbf;color:white;padding:12px 24px;text-decoration:none;border-radius:24px;display:inline-block;font-weight:bold;'>قبول الدعوة وتفعيل الحساب</a></p>
                    <p style='font-size:0.8rem;color:#888;'>هذا الرابط صالح لمدة 7 أيام فقط.</p>
                </div>
            ";

            await _emailService.SendEmailAsync(email, email, subject, body);

            await LogActivityInternalAsync(profile.Id, "Invited", $"تم إرسال دعوة انضمام للموظف بواسطة المستخدم {senderUserId}");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ResendInvitationAsync(int profileId, string inviteUrlPrefix)
        {
            var profile = await _uow.StaffProfiles.Table
                .Include(p => p.ApplicationUser)
                .Include(p => p.HealthcareProvider)
                .FirstOrDefaultAsync(p => p.Id == profileId);

            if (profile == null)
                return ServiceResult.NotFound("Staff profile not found.");

            if (profile.IsInvitationAccepted)
                return ServiceResult.Failure("Invitation is already accepted.");

            var token = Guid.NewGuid().ToString("N");
            profile.InvitationToken = token;
            profile.InvitationTokenExpiry = DateTime.UtcNow.AddDays(7);

            _uow.StaffProfiles.Update(profile);
            await _uow.CompleteAsync();

            var inviteLink = $"{inviteUrlPrefix}?token={token}";
            string subject = "تذكير: دعوة للانضمام إلى طاقم عمل منصة اطمئن";
            string roleArabic = profile.RoleType == StaffRoleType.Receptionist ? "موظف استقبال" : "موظف طوارئ وفرز";

            string body = $@"
                <div dir='rtl' style='font-family:tahoma,arial,sans-serif;padding:20px;border:1px solid #eee;border-radius:12px;max-width:600px;margin:auto;'>
                    <h3 style='color:#1a6fbf;'>تذكير بدعوة الانضمام لمنصة اطمئن</h3>
                    <p>نذكرك بالدعوة المرسلة إليك للانضمام إلى طاقم عمل <strong>{profile.HealthcareProvider.Name}</strong> كـ <strong>{roleArabic}</strong>.</p>
                    <p>يرجى الضغط على الرابط أدناه لإكمال تفعيل حسابك:</p>
                    <p style='text-align:center;'><a href='{inviteLink}' style='background-color:#1a6fbf;color:white;padding:12px 24px;text-decoration:none;border-radius:24px;display:inline-block;font-weight:bold;'>قبول الدعوة وتفعيل الحساب</a></p>
                </div>
            ";

            await _emailService.SendEmailAsync(profile.ApplicationUser.Email!, profile.ApplicationUser.Email!, subject, body);

            await LogActivityInternalAsync(profile.Id, "ResentInvitation", "تم إعادة إرسال بريد الدعوة والتذكير");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> CancelInvitationAsync(int profileId)
        {
            var profile = await _uow.StaffProfiles.Table
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == profileId);

            if (profile == null)
                return ServiceResult.NotFound("Staff profile not found.");

            if (profile.IsInvitationAccepted)
                return ServiceResult.Failure("Cannot cancel an already accepted invitation.");

            var user = profile.ApplicationUser;

            _uow.StaffProfiles.Remove(profile);
            await _uow.CompleteAsync();

            if (user.MustChangePassword)
            {
                await _userManager.DeleteAsync(user);
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateStaffAsync(int profileId, StaffRoleType role, StaffShiftType shift)
        {
            var profile = await _uow.StaffProfiles.GetByIdAsync(profileId);
            if (profile == null)
                return ServiceResult.NotFound("Staff profile not found.");

            profile.RoleType = role;
            profile.ActiveShift = shift;

            _uow.StaffProfiles.Update(profile);
            await _uow.CompleteAsync();

            await LogActivityInternalAsync(profile.Id, "UpdateStaff", $"تم تعديل الدور إلى {role} والوردية إلى {shift}");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RemoveStaffAsync(int profileId)
        {
            var profile = await _uow.StaffProfiles.Table
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == profileId);

            if (profile == null)
                return ServiceResult.NotFound("Staff profile not found.");

            var user = profile.ApplicationUser;

            _uow.StaffProfiles.Remove(profile);
            await _uow.CompleteAsync();

            await _userManager.RemoveFromRoleAsync(user, "HospitalStaff");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<string>> GenerateInviteLinkAsync(int providerId, StaffRoleType role, StaffShiftType shift, string senderUserId, string inviteUrlPrefix)
        {
            var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
            if (provider == null)
                return ServiceResult<string>.NotFound("Provider not found.");

            var token = Guid.NewGuid().ToString("N");
            var profile = new StaffProfile
            {
                ApplicationUserId = "",
                HealthcareProviderId = providerId,
                RoleType = role,
                ActiveShift = shift,
                IsInvitationAccepted = false,
                InvitationSenderUserId = senderUserId,
                InvitationToken = token,
                InvitationTokenExpiry = DateTime.UtcNow.AddDays(1),
                JoinedAt = null
            };

            await _uow.StaffProfiles.AddAsync(profile);
            await _uow.CompleteAsync();

            var inviteLink = $"{inviteUrlPrefix}?token={token}";
            return ServiceResult<string>.Success(inviteLink);
        }

        public async Task<ServiceResult> AcceptInvitationAsync(string token, string userId)
        {
            var profile = await _uow.StaffProfiles.Table
                .Include(p => p.HealthcareProvider)
                .FirstOrDefaultAsync(p => p.InvitationToken == token);

            if (profile == null)
                return ServiceResult.Failure("رابط الدعوة هذا غير صحيح.");

            if (profile.InvitationTokenExpiry.HasValue && profile.InvitationTokenExpiry.Value < DateTime.UtcNow)
                return ServiceResult.Failure("رابط الدعوة هذا قد انتهت صلاحيته.");

            profile.ApplicationUserId = userId;
            profile.IsInvitationAccepted = true;
            profile.InvitationToken = null;
            profile.InvitationTokenExpiry = null;
            profile.JoinedAt = DateTime.UtcNow;

            _uow.StaffProfiles.Update(profile);
            await _uow.CompleteAsync();

            if (!string.IsNullOrEmpty(profile.InvitationSenderUserId))
            {
                var sender = await _uow.Users.FirstOrDefaultAsync(u => u.Id == profile.InvitationSenderUserId);
                if (sender != null)
                {
                    var user = await _uow.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    var userEmail = user?.Email ?? "موظف جديد";

                    var alert = new Alert
                    {
                        UserId = profile.InvitationSenderUserId,
                        Title = "قبول دعوة انضمام موظف",
                        Message = $"الموظف ({userEmail}) قبل دعوتك وانضم رسمياً لطاقم العمل في ({profile.HealthcareProvider.Name}).",
                        AlertType = "Notification",
                        Status = AlertStatus.Unread,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _uow.Alerts.AddAsync(alert);
                    await _uow.CompleteAsync();
                }
            }

            await LogActivityInternalAsync(profile.Id, "AcceptedInvitation", "قبل الموظف الدعوة وانضم لطاقم العمل");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> LogActivityAsync(int staffProfileId, string action, string? details)
        {
            await LogActivityInternalAsync(staffProfileId, action, details);
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<List<StaffActivityLogDto>>> GetLogsAsync(int providerId)
        {
            var logs = await _uow.StaffActivityLogs.Table
                .Include(l => l.StaffProfile)
                    .ThenInclude(p => p.ApplicationUser)
                .Where(l => l.StaffProfile.HealthcareProviderId == providerId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();

            var dtos = logs.Select(l => new StaffActivityLogDto
            {
                Id = l.Id,
                StaffProfileId = l.StaffProfileId,
                StaffName = $"{l.StaffProfile.ApplicationUser.FirstName} {l.StaffProfile.ApplicationUser.LastName}".Trim(),
                Action = l.Action,
                Details = l.Details,
                CreatedAt = l.CreatedAt
            }).ToList();

            return ServiceResult<List<StaffActivityLogDto>>.Success(dtos);
        }

        public async Task<ServiceResult<StaffStatsDto>> GetStatsAsync(int providerId)
        {
            var provider = await _uow.HealthcareProviders.GetByIdAsync(providerId);
            if (provider == null)
                return ServiceResult<StaffStatsDto>.NotFound("Provider not found.");

            var staffList = await _uow.StaffProfiles.Table
                .Include(sp => sp.ApplicationUser)
                .Where(sp => sp.HealthcareProviderId == providerId)
                .ToListAsync();

            var requests = await _uow.EmergencyRequests.Table
                .Where(e => e.HealthcareProviderId == providerId && e.AcceptedAt.HasValue)
                .ToListAsync();

            var totalResponseTime = requests
                .Where(e => e.AcceptedAt.HasValue)
                .Select(e => (e.AcceptedAt!.Value - e.RequestedAt).TotalMinutes)
                .ToList();

            var avgResponseTime = totalResponseTime.Count > 0 ? totalResponseTime.Average() : 0.0;

            var stats = new StaffStatsDto
            {
                ProviderId = providerId,
                ProviderName = provider.Name,
                TotalStaffCount = staffList.Count,
                ActiveReceptionistsCount = staffList.Count(s => s.RoleType == StaffRoleType.Receptionist && s.IsInvitationAccepted),
                ActiveTriageStaffCount = staffList.Count(s => s.RoleType == StaffRoleType.TriageStaff && s.IsInvitationAccepted),
                AcceptedRequestsCount = requests.Count,
                AverageResponseTimeInMinutes = Math.Round(avgResponseTime, 2),
                StaffPerformance = staffList
                    .Where(s => s.IsInvitationAccepted)
                    .Select(s => {
                        var handled = requests.Where(r => r.RespondedByUserId == s.ApplicationUserId).ToList();
                        var staffAvgTime = handled.Count > 0
                            ? handled.Select(r => (r.AcceptedAt!.Value - r.RequestedAt).TotalMinutes).Average()
                            : 0.0;
                        return new StaffPerformanceDto
                        {
                            StaffName = $"{s.ApplicationUser.FirstName} {s.ApplicationUser.LastName}".Trim(),
                            Email = s.ApplicationUser.Email ?? string.Empty,
                            RoleType = s.RoleType,
                            HandledRequestsCount = handled.Count,
                            AverageResponseTimeInMinutes = Math.Round(staffAvgTime, 2)
                        };
                    })
                    .OrderByDescending(p => p.HandledRequestsCount)
                    .ToList()
            };

            return ServiceResult<StaffStatsDto>.Success(stats);
        }

        // ── Helper Methods ────────────────────────────────────────────────────

        /// <summary>
        /// Stages an activity log entry without saving — caller must call CompleteAsync.
        /// </summary>
        private async Task StageActivityLogAsync(int staffProfileId, string action, string? details)
        {
            var log = new StaffActivityLog
            {
                StaffProfileId = staffProfileId,
                Action = action,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.StaffActivityLogs.AddAsync(log);
            // Intentionally no CompleteAsync here — caller is responsible for saving.
        }

        /// <summary>
        /// Saves an activity log entry immediately (standalone operations).
        /// </summary>
        private async Task LogActivityInternalAsync(int staffProfileId, string action, string? details)
        {
            await StageActivityLogAsync(staffProfileId, action, details);
            await _uow.CompleteAsync();
        }

        private static HospitalStaffQueueItemDto MapQueueItem(EmergencyRequest request)
        {
            var user = request.PatientProfile?.ApplicationUser;
            return new HospitalStaffQueueItemDto
            {
                RequestId = request.Id,
                PatientProfileId = request.PatientProfileId,
                PatientName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Patient",
                PatientPhone = user?.PhoneNumber ?? string.Empty,
                EmergencyType = request.EmergencyType ?? "General",
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                WaitingMinutes = (int)(DateTime.UtcNow - request.RequestedAt).TotalMinutes,
                IsAutoGenerated = request.IsAutoGenerated,
                PriorityScore = request.PriorityScore,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                AssignedProviderId = request.HealthcareProviderId,
                AssignedProviderName = request.HealthcareProvider?.Name,
                ResponseNotes = request.ResponseNotes
            };
        }

        private static HospitalStaffEmergencyDetailDto MapDetail(EmergencyRequest request) => new()
        {
            RequestId = request.Id,
            Status = request.Status,
            EmergencyType = request.EmergencyType ?? "General",
            Description = request.Description,
            RequestedAt = request.RequestedAt,
            AcceptedAt = request.AcceptedAt,
            ResponseNotes = request.ResponseNotes,
            PatientName = request.PatientProfile?.ApplicationUser != null ? $"{request.PatientProfile.ApplicationUser.FirstName} {request.PatientProfile.ApplicationUser.LastName}".Trim() : "Patient",
            PatientPhone = request.PatientProfile?.ApplicationUser?.PhoneNumber ?? string.Empty,
            BloodType = request.PatientProfile?.BloodType,
            HasChronicDiseases = request.PatientProfile?.HasChronicDiseases ?? false,
            ChronicDiseasesNotes = request.PatientProfile?.ChronicDiseasesNotes,
            Allergies = request.PatientProfile?.Allergies,
            CurrentMedications = request.PatientProfile?.CurrentMedications,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AssignedProviderAvailableBeds = request.HealthcareProvider?.AvailableBeds
        };

        private static IEnumerable<string> ValidateResponse(HospitalStaffEmergencyRespondDto dto)
        {
            if (dto is null)
            {
                yield return "Emergency response payload is required.";
                yield break;
            }

            if (dto.RequestId <= 0)
                yield return "Request id is required.";
            if (dto.ProviderId <= 0)
                yield return "Provider id is required.";
            if (string.IsNullOrWhiteSpace(dto.Status))
                yield return "Response status is required.";
        }

        private static bool IsStaffActionStatus(EmergencyRequestStatus status)
            => status is EmergencyRequestStatus.Accepted
                or EmergencyRequestStatus.Rejected
                or EmergencyRequestStatus.Escalated
                or EmergencyRequestStatus.Completed
                or EmergencyRequestStatus.Cancelled;

        private static string? ValidateTransition(EmergencyRequestStatus current, EmergencyRequestStatus next, string? notes)
        {
            if (current is EmergencyRequestStatus.Completed or EmergencyRequestStatus.Cancelled)
                return "Completed or cancelled requests cannot be changed.";

            if (current == EmergencyRequestStatus.Rejected && next != EmergencyRequestStatus.Escalated)
                return "Rejected requests can only be escalated.";

            if (next is EmergencyRequestStatus.Rejected or EmergencyRequestStatus.Cancelled or EmergencyRequestStatus.Escalated &&
                string.IsNullOrWhiteSpace(notes))
                return "Response notes are required for rejection, cancellation, or escalation.";

            if (next == EmergencyRequestStatus.Completed && current != EmergencyRequestStatus.Accepted)
                return "Only accepted emergency requests can be completed.";

            return null;
        }

        private static void ApplyResponse(
            EmergencyRequest request,
            HealthcareProvider provider,
            EmergencyRequestStatus status,
            string? notes)
        {
            var wasAccepted = request.Status == EmergencyRequestStatus.Accepted;
            request.Status = status;
            request.ResponseNotes = string.IsNullOrWhiteSpace(notes) ? request.ResponseNotes : notes.Trim();

            switch (status)
            {
                case EmergencyRequestStatus.Accepted:
                    request.HealthcareProviderId = provider.Id;
                    request.AcceptedAt ??= DateTime.UtcNow;
                    provider.AvailableBeds = Math.Max(0, (provider.AvailableBeds ?? 0) - 1);
                    provider.AvailableAmbulances = Math.Max(0, (provider.AvailableAmbulances ?? 0) - 1);
                    break;
                case EmergencyRequestStatus.Completed:
                    request.CompletedAt ??= DateTime.UtcNow;
                    if (wasAccepted)
                    {
                        provider.AvailableAmbulances = Math.Min(provider.AmbulanceCapacity ?? 4, (provider.AvailableAmbulances ?? 0) + 1);
                    }
                    break;
                case EmergencyRequestStatus.Rejected:
                case EmergencyRequestStatus.Escalated:
                case EmergencyRequestStatus.Cancelled:
                    request.HealthcareProviderId ??= provider.Id;
                    if (wasAccepted)
                    {
                        provider.AvailableAmbulances = Math.Min(provider.AmbulanceCapacity ?? 4, (provider.AvailableAmbulances ?? 0) + 1);
                    }
                    break;
            }
        }
    }
}
