namespace Etmen_BLL.DTOs.Family
{
    public class FamilyDto
    {
        public int Id { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public bool IsAccepted { get; set; }
        public bool CanViewRecords { get; set; }
        public bool CanViewRisk { get; set; }
        public bool CanBookAppointments { get; set; }
        public string LinkedPatientName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}