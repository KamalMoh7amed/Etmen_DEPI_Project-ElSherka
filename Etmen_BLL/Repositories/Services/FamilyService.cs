using Etmen_BLL.DTOs.Family;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Mapster;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class FamilyService : IFamilyService
    {
        private readonly IUnitOfWork _uow;

        public FamilyService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<FamilyDto>> InviteFamilyMemberAsync(FamilyInviteDto dto)
        {
            try
            {
                if (dto.LinkedPatientId <= 0)
                    return ServiceResult<FamilyDto>.Failure("Valid linked patient ID is required.");

                if (string.IsNullOrWhiteSpace(dto.Relationship))
                    return ServiceResult<FamilyDto>.Failure("Relationship is required.");

                // Verify that the linked patient exists
                var linkedPatient = await _uow.PatientProfiles.GetByIdAsync(dto.LinkedPatientId);
                if (linkedPatient == null)
                    return ServiceResult<FamilyDto>.Failure("Linked patient not found.");

                // Create a new family link with invite token
                var inviteToken = InviteTokenGenerator.Generate();
                var familyLink = new FamilyLink
                {
                    LinkedPatientId = dto.LinkedPatientId,
                    Relationship = dto.Relationship,
                    InviteToken = inviteToken,
                    CanViewRecords = dto.CanViewRecords,
                    CanViewRisk = dto.CanViewRisk,
                    CanBookAppointments = dto.CanBookAppointments,
                    IsAccepted = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.FamilyLinks.AddAsync(familyLink);
                await _uow.CompleteAsync();

                var result = familyLink.Adapt<FamilyDto>();
                return ServiceResult<FamilyDto>.Success(result, 201);
            }
            catch (Exception ex)
            {
                return ServiceResult<FamilyDto>.Failure($"Failed to invite family member: {ex.Message}");
            }
        }

        public async Task<ServiceResult> AcceptFamilyInviteAsync(string inviteToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(inviteToken))
                    return ServiceResult.Failure("Invite token is required.");

                var familyLink = await _uow.FamilyLinks.GetByInviteTokenAsync(inviteToken);
                if (familyLink == null)
                    return ServiceResult.Failure("Invalid or expired invite token.");

                if (familyLink.IsAccepted)
                    return ServiceResult.Failure("This invitation has already been accepted.");

                familyLink.IsAccepted = true;
                familyLink.AcceptedAt = DateTime.UtcNow;

                _uow.FamilyLinks.Update(familyLink);
                await _uow.CompleteAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to accept family invite: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<FamilyDto>>> GetFamilyMembersAsync(int patientProfileId)
        {
            try
            {
                var familyLinks = await _uow.FamilyLinks.GetByPrimaryPatientIdAsync(patientProfileId);
                var familyDtos = familyLinks
                    .Where(fl => fl.IsAccepted)
                    .Select(fl => fl.Adapt<FamilyDto>())
                    .ToList();

                return ServiceResult<List<FamilyDto>>.Success(familyDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<FamilyDto>>.Failure($"Failed to retrieve family members: {ex.Message}");
            }
        }

        public async Task<ServiceResult> RemoveFamilyMemberAsync(int familyLinkId)
        {
            try
            {
                var familyLink = await _uow.FamilyLinks.GetByIdAsync(familyLinkId);
                if (familyLink == null)
                    return ServiceResult.Failure("Family link not found.");

                _uow.FamilyLinks.Remove(familyLink);
                await _uow.CompleteAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to remove family member: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateFamilyPermissionsAsync(int familyLinkId, FamilyDto dto)
        {
            try
            {
                var familyLink = await _uow.FamilyLinks.GetByIdAsync(familyLinkId);
                if (familyLink == null)
                    return ServiceResult.Failure("Family link not found.");

                familyLink.CanViewRecords = dto.CanViewRecords;
                familyLink.CanViewRisk = dto.CanViewRisk;
                familyLink.CanBookAppointments = dto.CanBookAppointments;

                _uow.FamilyLinks.Update(familyLink);
                await _uow.CompleteAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to update family permissions: {ex.Message}");
            }
        }

    }
}