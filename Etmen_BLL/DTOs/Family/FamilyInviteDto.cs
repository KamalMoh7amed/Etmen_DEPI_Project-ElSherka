namespace Etmen_BLL.DTOs.Family
{
    public class FamilyInviteDto
    {
        public string Relationship { get; set; } = string.Empty;
        public bool CanViewRecords { get; set; }
        public bool CanViewRisk { get; set; }
        public bool CanBookAppointments { get; set; }
    }
}