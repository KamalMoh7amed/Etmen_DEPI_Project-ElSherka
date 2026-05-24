using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Emergency
{
    public class EmergencyTrackingDto
    {
        public int RequestId { get; set; }
        public EmergencyRequestStatus Status { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderPhone { get; set; }
        public decimal? EstimatedArrivalTime { get; set; }
        public decimal DistanceInKm { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
    }
}