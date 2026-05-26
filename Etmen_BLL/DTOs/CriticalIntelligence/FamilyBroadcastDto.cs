namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class FamilyBroadcastDto
    {
        public int EmergencyRequestId { get; set; }
        public int FamilyMembersNotified { get; set; }
        public List<string> NotifiedUserIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
