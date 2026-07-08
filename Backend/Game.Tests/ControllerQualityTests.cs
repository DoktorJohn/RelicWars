using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Game.Contracts;
using Game.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Domain.User;
using System.Security.Claims;
using Xunit;

namespace Game.Tests;

public class ControllerQualityTests
{
    private static readonly Guid MessagingProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MessagingWorldPlayerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void WorldPlayerController_DoesNotExposeCheatRoute()
    {
        var routes = typeof(WorldPlayerController)
            .GetMethods()
            .SelectMany(method => method.GetCustomAttributes(inherit: true))
            .OfType<HttpMethodAttribute>()
            .Select(attribute => attribute.Template ?? string.Empty);

        Assert.DoesNotContain(routes, route => route.Contains("cheat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task WorldCityInspection_ForbiddenReturnsForbid()
    {
        var controller = new WorldController(new ThrowingWorldService(new UnauthorizedAccessException()), NullLogger<WorldController>.Instance);

        var result = await controller.GetCityInspection(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task WorldMapChunk_NotFoundReturnsApiError()
    {
        var controller = new WorldController(new NullWorldService(), NullLogger<WorldController>.Instance);

        var result = await controller.GetWorldMapChunkData(new GetWorldMapChunkDTO());

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ApiError>(notFound.Value);
        Assert.Equal("resource.not_found", error.Code);
    }

    [Fact]
    public async Task WorldPlayerJoinFailureReturnsApiError()
    {
        var controller = new WorldPlayerController(
            NullLogger<WorldPlayerController>.Instance,
            new FailingJoinWorldPlayerService("world is full"));

        var result = await controller.ProcessPlayerWorldJoinRequest(new WorldPlayerDTO(Guid.NewGuid(), Guid.NewGuid()));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ApiError>(badRequest.Value);
        Assert.Equal("world_player.join_failed", error.Code);
        Assert.Equal("world is full", error.Message);
    }

    [Fact]
    public async Task RankingUnexpectedErrorReturnsServerApiError()
    {
        var controller = new RankingController(
            NullLogger<RankingController>.Instance,
            new ThrowingRankingService(new InvalidDataException("ranking internals")));

        var result = await controller.GetRankings();

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var error = Assert.IsType<ApiError>(serverError.Value);
        Assert.Equal("server.error", error.Code);
        Assert.DoesNotContain("ranking internals", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AttackCityDeployment_InvalidStateReturnsStableApiError()
    {
        var controller = CreateController(new InvalidOperationException("internal unit inventory details"));

        var result = await controller.AttackCityDeployment(null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ApiError>(badRequest.Value);
        Assert.Equal("deployment.invalid_state", error.Code);
        Assert.DoesNotContain("inventory", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AttackCityDeployment_NotFoundReturnsNotFound()
    {
        var result = await CreateController(new KeyNotFoundException()).AttackCityDeployment(null!);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AttackCityDeployment_ForbiddenReturnsForbid()
    {
        var result = await CreateController(new UnauthorizedAccessException()).AttackCityDeployment(null!);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AttackCityDeployment_UnexpectedErrorReturnsGenericServerError()
    {
        var result = await CreateController(new InvalidDataException("database password leaked"))
            .AttackCityDeployment(null!);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var error = Assert.IsType<ApiError>(serverError.Value);
        Assert.Equal("server.error", error.Code);
        Assert.Equal("En intern serverfejl opstod.", error.Message);
    }

    [Fact]
    public async Task MessagingStartConversation_ArgumentExceptionReturnsStableApiError()
    {
        var controller = CreateMessagingController(new ArgumentException("missing subject value"));

        var result = await controller.StartConversation(MessagingWorldPlayerId, new StartConversationRequestDTO
        {
            ReceiverWorldPlayerId = Guid.NewGuid(),
            ParticipantWorldPlayerIds = [Guid.NewGuid()],
            Subject = "subject",
            Content = "content"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ApiError>(badRequest.Value);
        Assert.Equal("request.invalid", error.Code);
        Assert.Equal("Anmodningen er ugyldig.", error.Message);
        Assert.DoesNotContain("missing subject", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MessagingStartConversation_InvalidOperationReturnsConflictApiError()
    {
        var controller = CreateMessagingController(new InvalidOperationException("internal conversation details"));

        var result = await controller.StartConversation(MessagingWorldPlayerId, new StartConversationRequestDTO
        {
            ReceiverWorldPlayerId = Guid.NewGuid(),
            ParticipantWorldPlayerIds = [Guid.NewGuid()],
            Subject = "subject",
            Content = "content"
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var error = Assert.IsType<ApiError>(conflict.Value);
        Assert.Equal("resource.conflict", error.Code);
        Assert.Equal("Handlingen er i konflikt med den aktuelle tilstand.", error.Message);
        Assert.DoesNotContain("details", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BattleReportMarkAsRead_NotFoundReturnsNotFound()
    {
        var controller = CreateBattleReportController(new KeyNotFoundException());

        var result = await controller.MarkAsRead(MessagingWorldPlayerId, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task BattleReportGetUnreadStatus_InvalidOperationReturnsConflictApiError()
    {
        var controller = CreateBattleReportController(new InvalidOperationException("internal report details"));

        var result = await controller.GetUnreadStatus(MessagingWorldPlayerId);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var error = Assert.IsType<ApiError>(conflict.Value);
        Assert.Equal("resource.conflict", error.Code);
        Assert.Equal("Handlingen er i konflikt med den aktuelle tilstand.", error.Message);
    }

    [Fact]
    public async Task BattleReportDelete_NotFoundReturnsNotFound()
    {
        var controller = CreateBattleReportController(new KeyNotFoundException());

        var result = await controller.DeleteReport(MessagingWorldPlayerId, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    private static UnitDeploymentController CreateController(Exception exception) =>
        new(new ThrowingUnitDeploymentService(exception), NullLogger<UnitDeploymentController>.Instance);

    private static MessagingController CreateMessagingController(Exception exception)
    {
        var controller = new MessagingController(
            new ThrowingMessagingService(exception),
            new OwnedWorldPlayerRepository(MessagingProfileId, MessagingWorldPlayerId),
            NullLogger<MessagingController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, MessagingProfileId.ToString())],
                        authenticationType: "Test"))
                }
            }
        };

        return controller;
    }

    private static BattleReportController CreateBattleReportController(Exception exception)
    {
        var controller = new BattleReportController(
            new ThrowingBattleReportService(exception),
            NullLogger<BattleReportController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, MessagingProfileId.ToString())],
                        authenticationType: "Test"))
                }
            }
        };

        return controller;
    }

    private sealed class ThrowingUnitDeploymentService : IUnitDeploymentService
    {
        private readonly Exception _exception;

        public ThrowingUnitDeploymentService(Exception exception) => _exception = exception;

        public Task<OwnedUnitDeploymentDTO> AttackCityDeploymentAsync(AttackCityDeploymentRequestDTO dto) =>
            Task.FromException<OwnedUnitDeploymentDTO>(_exception);

        public Task<OwnedUnitDeploymentDTO> SupportCityDeploymentAsync(SupportCityDeploymentRequestDTO dto) =>
            Task.FromException<OwnedUnitDeploymentDTO>(_exception);

        public Task<OwnedUnitDeploymentDTO> RecallAsync(Guid deploymentId) =>
            Task.FromException<OwnedUnitDeploymentDTO>(_exception);

        public Task<List<OwnedUnitDeploymentDTO>> GetDeploymentsAsync(Guid worldPlayerId) =>
            Task.FromException<List<OwnedUnitDeploymentDTO>>(_exception);
    }

    private sealed class ThrowingMessagingService(Exception exception) : IMessagingService
    {
        private readonly Exception _exception = exception;

        public Task<ConversationDTO> StartConversationAsync(Guid senderId, IEnumerable<Guid> participantIds, string subject, string content) =>
            Task.FromException<ConversationDTO>(_exception);

        public Task<MessageDTO> ReplyToConversationAsync(Guid requestorId, Guid conversationId, string content) =>
            Task.FromException<MessageDTO>(_exception);

        public Task<List<ConversationDTO>> GetConversationsAsync(Guid worldPlayerId) =>
            Task.FromException<List<ConversationDTO>>(_exception);

        public Task<List<MessageDTO>> GetMessagesAsync(Guid conversationId, Guid requestorId, DateTime? before, int take) =>
            Task.FromException<List<MessageDTO>>(_exception);

        public Task MarkConversationAsReadAsync(Guid conversationId, Guid requestorId) =>
            Task.FromException(_exception);

        public Task MarkMessageAsReadAsync(Guid messageId, Guid requestorId) =>
            Task.FromException(_exception);

        public Task DeleteConversationAsync(Guid conversationId, Guid requestorId) =>
            Task.FromException(_exception);

        public Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query) =>
            Task.FromException<List<PlayerSearchResultDTO>>(_exception);

        public Task<bool> HasUnreadMessagesAsync(Guid worldPlayerId) =>
            Task.FromException<bool>(_exception);

        public Task<int> CountUnreadMessagesAsync(Guid worldPlayerId) =>
            Task.FromException<int>(_exception);
    }

    private sealed class ThrowingBattleReportService(Exception exception) : IBattleReportService
    {
        private readonly Exception _exception = exception;

        public Task<List<BattleReportDTO>> GetBattleReportsAsync(Guid worldPlayerId) =>
            Task.FromException<List<BattleReportDTO>>(_exception);

        public Task<BattleReportUnreadStatusDTO> GetUnreadStatusAsync(Guid worldPlayerId) =>
            Task.FromException<BattleReportUnreadStatusDTO>(_exception);

        public Task MarkBattleReportAsReadAsync(Guid worldPlayerId, Guid battleReportId) =>
            Task.FromException(_exception);

        public Task DeleteBattleReportAsync(Guid worldPlayerId, Guid battleReportId) =>
            Task.FromException(_exception);
    }

    private sealed class ThrowingWorldService(Exception exception) : IWorldService
    {
        private readonly Exception _exception = exception;

        public Task<List<WorldAvailableResponseDTO>> ObtainAllActiveGameWorldsAsync() =>
            Task.FromException<List<WorldAvailableResponseDTO>>(_exception);

        public Task<WorldMapChunkResponseDTO?> GetWorldMapChunk(GetWorldMapChunkDTO dto) =>
            Task.FromException<WorldMapChunkResponseDTO?>(_exception);

        public Task<CityInspectionDTO?> GetCityInspectionAsync(Guid cityId) =>
            Task.FromException<CityInspectionDTO?>(_exception);

        public Task<WorldIslandDetailsDTO?> GetIslandDetailsAsync(Guid islandId) =>
            Task.FromException<WorldIslandDetailsDTO?>(_exception);
    }

    private sealed class NullWorldService : IWorldService
    {
        public Task<List<WorldAvailableResponseDTO>> ObtainAllActiveGameWorldsAsync() =>
            Task.FromResult(new List<WorldAvailableResponseDTO>());

        public Task<WorldMapChunkResponseDTO?> GetWorldMapChunk(GetWorldMapChunkDTO dto) =>
            Task.FromResult<WorldMapChunkResponseDTO?>(null);

        public Task<CityInspectionDTO?> GetCityInspectionAsync(Guid cityId) =>
            Task.FromResult<CityInspectionDTO?>(null);

        public Task<WorldIslandDetailsDTO?> GetIslandDetailsAsync(Guid islandId) =>
            Task.FromResult<WorldIslandDetailsDTO?>(null);
    }

    private sealed class FailingJoinWorldPlayerService(string message) : IWorldPlayerService
    {
        public Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid worldId) =>
            Task.FromResult(new WorldPlayerJoinResponse(false, message, null, null, Domain.Enums.IdeologyTypeEnum.None));

        public Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId) =>
            Task.FromException<WorldPlayerProfileDTO>(new NotImplementedException());

        public Task<WorldPlayerProfileDTO> UpdateWorldPlayerDescriptionAsync(Guid worldPlayerId, string description) =>
            Task.FromException<WorldPlayerProfileDTO>(new NotImplementedException());

        public Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId) =>
            Task.FromException<WorldPlayerEconomyDTO>(new NotImplementedException());

        public Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query) =>
            Task.FromException<List<PlayerSearchResultDTO>>(new NotImplementedException());

        public void SyncGlobalResources(WorldPlayer player, DateTime currentDateTime)
        {
        }

        public Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request) =>
            Task.FromException<WorldPlayerSelectIdeologyResponse>(new NotImplementedException());
    }

    private sealed class ThrowingRankingService(Exception exception) : IRankingService
    {
        private readonly Exception _exception = exception;

        public Task<List<Domain.StaticData.Data.RankingEntryData>> GetRankings() =>
            Task.FromException<List<Domain.StaticData.Data.RankingEntryData>>(_exception);

        public Task<Domain.StaticData.Data.RankingEntryData?> GetRankingById(Guid worldPlayerId) =>
            Task.FromException<Domain.StaticData.Data.RankingEntryData?>(_exception);
    }

    private sealed class OwnedWorldPlayerRepository(Guid profileId, Guid worldPlayerId) : IWorldPlayerRepository
    {
        private readonly WorldPlayer _ownedWorldPlayer = new()
        {
            Id = worldPlayerId,
            PlayerProfileId = profileId
        };

        public Task<WorldPlayer?> GetByIdAsync(Guid id) =>
            Task.FromResult(id == _ownedWorldPlayer.Id ? _ownedWorldPlayer : null);

        public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) =>
            Task.FromResult<WorldPlayer?>(null);

        public Task AddAsync(WorldPlayer user) => Task.CompletedTask;
        public Task UpdateAsync(WorldPlayer user) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<List<WorldPlayer>>? GetAllAsync() => Task.FromResult<List<WorldPlayer>>(new());
        public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) =>
            Task.FromResult(profileId == _ownedWorldPlayer.PlayerProfileId ? _ownedWorldPlayer : null);
        public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) => Task.FromResult(new List<WorldPlayer>());
        public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery) => Task.FromResult(new List<WorldPlayer>());
    }
}
