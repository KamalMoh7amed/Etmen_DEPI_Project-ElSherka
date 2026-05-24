using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Emergency
{
    public class HospitalQueueDto
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string EmergencyType { get; set; } = string.Empty;
        public EmergencyRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public int? AvailableBeds { get; set; }
    }
}