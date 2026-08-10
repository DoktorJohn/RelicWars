using Application.DTOs;
using Application.Interfaces.IServices;
using Game.Contracts;
using Game.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Xunit;

namespace Game.Tests;

public class AllianceControllerTests
{
    [Fact]
    public async Task CreateAllianceReturnsConflictForDuplicateWorldName()
    {
        var controller = new AllianceController(
            new DuplicateNameAllianceService(),
            NullLogger<AllianceController>.Instance);

        var result = await controller.CreateAlliance(
            new CreateAllianceDTO(Guid.NewGuid(), "Existing", "EXT"));

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var error = Assert.IsType<ApiError>(conflict.Value);
        Assert.Equal("alliance.conflict", error.Code);
        Assert.Equal("Alliance name is already in use in this world.", error.Message);
    }

    [Fact]
    public async Task CancelInvitationForwardsDtoAndReturnsServiceResult()
    {
        var service = new DuplicateNameAllianceService { CancelResult = true };
        var controller = new AllianceController(service, NullLogger<AllianceController>.Instance);
        var dto = new CancelAllianceInvitationDTO(Guid.NewGuid(), Guid.NewGuid());

        var result = await controller.CancelInvitation(dto);

        Assert.Same(dto, service.CancelDto);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(ok.Value));
    }

    [Fact]
    public void CancelInvitationContractHasAuthorizedPostRouteAndExpectedShape()
    {
        var method = typeof(AllianceController).GetMethod(nameof(AllianceController.CancelInvitation))!;
        var route = Assert.Single(method.GetCustomAttributes<HttpPostAttribute>());

        Assert.Equal("invitations/cancel", route.Template);
        Assert.NotNull(typeof(AllianceController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal(
            new[] { nameof(CancelAllianceInvitationDTO.InvitationId), nameof(CancelAllianceInvitationDTO.WorldPlayerId) },
            typeof(CancelAllianceInvitationDTO).GetProperties().Select(property => property.Name).OrderBy(name => name));
    }

    private sealed class DuplicateNameAllianceService : IAllianceService
    {
        public CancelAllianceInvitationDTO? CancelDto { get; private set; }
        public bool CancelResult { get; init; }
        public Task<AllianceDTO> CreateAlliance(CreateAllianceDTO dto) =>
            Task.FromException<AllianceDTO>(new InvalidOperationException("Alliance name is already in use in this world."));

        public Task<AllianceDTO> GetAllianceInfo(Guid allianceId) => throw new NotImplementedException();
        public Task<bool> DisbandAlliance(DisbandAllianceDTO dto) => throw new NotImplementedException();
        public Task<bool> InviteToAlliance(InviteToAllianceDTO dto) => throw new NotImplementedException();
        public Task<List<AllianceInvitationDTO>> GetInvitations(Guid worldPlayerId) => throw new NotImplementedException();
        public Task<List<AllianceInvitedPlayerDTO>> GetInvitedPlayers(Guid worldPlayerId) => throw new NotImplementedException();
        public Task<bool> CancelInvitation(CancelAllianceInvitationDTO dto)
        {
            CancelDto = dto;
            return Task.FromResult(CancelResult);
        }
        public Task<AllianceDTO> AcceptInvitation(RespondToAllianceInvitationDTO dto) => throw new NotImplementedException();
        public Task<bool> DeclineInvitation(RespondToAllianceInvitationDTO dto) => throw new NotImplementedException();
        public Task<bool> LeaveAlliance(LeaveAllianceDTO dto) => throw new NotImplementedException();
        public Task<bool> KickPlayer(KickPlayerFromAllianceDTO dto) => throw new NotImplementedException();
        public Task<AllianceDTO> SetMemberRole(SetAllianceMemberRoleDTO dto) => throw new NotImplementedException();
        public Task<AllianceDTO> UpdateDescription(UpdateAllianceDescriptionDTO dto) => throw new NotImplementedException();
        public Task<List<AllianceSearchResultDTO>> SearchAlliances(Guid worldId, string query) => throw new NotImplementedException();
        public Task<AllianceGeopoliticsDTO> GetGeopolitics(Guid allianceId) => throw new NotImplementedException();
        public Task<AllianceRelationDTO> SendPactInvite(SendPactInviteDTO dto) => throw new NotImplementedException();
        public Task<AllianceRelationDTO> RespondToPactInvite(RespondToPactInviteDTO dto) => throw new NotImplementedException();
        public Task<AllianceRelationDTO> DeclareWar(DeclareWarDTO dto) => throw new NotImplementedException();
    }
}
