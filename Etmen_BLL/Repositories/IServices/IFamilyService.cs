

namespace Etmen_BLL.Repositories.IServices
{
    public interface IFamilyService
    {
        Task<ServiceResult<FamilyDto>> InviteFamilyMemberAsync(FamilyInviteDto dto);
        Task<ServiceResult> AcceptFamilyInviteAsync(string inviteToken);
        Task<ServiceResult<List<FamilyDto>>> GetFamilyMembersAsync(int patientProfileId);
        Task<ServiceResult> RemoveFamilyMemberAsync(int familyLinkId);
        Task<ServiceResult> UpdateFamilyPermissionsAsync(int familyLinkId, FamilyDto dto);
    }
}
