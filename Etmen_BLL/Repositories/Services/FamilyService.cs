using Etmen_BLL.DTOs.Family;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class FamilyService : IFamilyService
    {
        private readonly IUnitOfWork _uow;

        public FamilyService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<FamilyDto>> InviteFamilyMemberAsync(FamilyInviteDto dto)
        {
            throw new NotImplementedException("InviteFamilyMemberAsync is not implemented yet.");
        }

        public Task<ServiceResult> AcceptFamilyInviteAsync(string inviteToken)
        {
            throw new NotImplementedException("AcceptFamilyInviteAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<FamilyDto>>> GetFamilyMembersAsync(int patientProfileId)
        {
            throw new NotImplementedException("GetFamilyMembersAsync is not implemented yet.");
        }

        public Task<ServiceResult> RemoveFamilyMemberAsync(int familyLinkId)
        {
            throw new NotImplementedException("RemoveFamilyMemberAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateFamilyPermissionsAsync(int familyLinkId, FamilyDto dto)
        {
            throw new NotImplementedException("UpdateFamilyPermissionsAsync is not implemented yet.");
        }

    }
}