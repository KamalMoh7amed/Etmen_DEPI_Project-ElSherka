using System;
using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.HospitalStaff
{
    public class StaffProfileDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public StaffRoleType RoleType { get; set; }
        public StaffShiftType ActiveShift { get; set; }
        public bool IsInvitationAccepted { get; set; }
        public string? InvitationToken { get; set; }
        public DateTime? InvitationTokenExpiry { get; set; }
        public DateTime? JoinedAt { get; set; }
    }

    public class StaffActivityLogDto
    {
        public int Id { get; set; }
        public int StaffProfileId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StaffStatsDto
    {
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public int TotalStaffCount { get; set; }
        public int ActiveReceptionistsCount { get; set; }
        public int ActiveTriageStaffCount { get; set; }
        public int AcceptedRequestsCount { get; set; }
        public double AverageResponseTimeInMinutes { get; set; }
        public List<StaffPerformanceDto> StaffPerformance { get; set; } = new();
    }

    public class StaffPerformanceDto
    {
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public StaffRoleType RoleType { get; set; }
        public int HandledRequestsCount { get; set; }
        public double AverageResponseTimeInMinutes { get; set; }
    }
}
