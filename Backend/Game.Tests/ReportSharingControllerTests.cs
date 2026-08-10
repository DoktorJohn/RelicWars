using Application.DTOs;
using Application.Interfaces.IServices;
using Game.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests;

public class ReportSharingControllerTests
{
    [Fact]
    public async Task PublicStatusEndpointIsAuthorizedAndForwardsRequest()
    {
        var service = new RecordingBattleReportService();
        var controller = new BattleReportController(service, NullLogger<BattleReportController>.Instance);
        var worldPlayerId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        var result = await controller.SetPublicStatus(
            worldPlayerId, reportId, new SetBattleReportPublicStatusRequest { IsPublic = true });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal((worldPlayerId, reportId, true), service.LastPublicStatusRequest);
        Assert.NotNull(typeof(BattleReportController).GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
        var method = typeof(BattleReportController).GetMethod(nameof(BattleReportController.SetPublicStatus));
        Assert.Equal("{worldPlayerId}/reports/{battleReportId}/public-status", method?.GetCustomAttributes(typeof(HttpPutAttribute), true).Cast<HttpPutAttribute>().Single().Template);
    }

    [Fact]
    public void MessagingContractsExposeOptionalReportWithoutOwnerReadStatus()
    {
        Assert.NotNull(typeof(StartConversationRequestDTO).GetProperty(nameof(StartConversationRequestDTO.BattleReportId)));
        Assert.NotNull(typeof(ReplyMessageRequestDTO).GetProperty(nameof(ReplyMessageRequestDTO.BattleReportId)));
        Assert.NotNull(typeof(MessageDTO).GetProperty(nameof(MessageDTO.ReportAttachment)));
        Assert.Null(typeof(SharedBattleReportDTO).GetProperty("IsRead"));
    }

    private sealed class RecordingBattleReportService : IBattleReportService
    {
        public (Guid WorldPlayerId, Guid ReportId, bool IsPublic)? LastPublicStatusRequest { get; private set; }
        public Task<List<BattleReportDTO>> GetBattleReportsAsync(Guid worldPlayerId) => Task.FromResult(new List<BattleReportDTO>());
        public Task<BattleReportUnreadStatusDTO> GetUnreadStatusAsync(Guid worldPlayerId) => Task.FromResult(new BattleReportUnreadStatusDTO());
        public Task MarkBattleReportAsReadAsync(Guid worldPlayerId, Guid battleReportId) => Task.CompletedTask;
        public Task DeleteBattleReportAsync(Guid worldPlayerId, Guid battleReportId) => Task.CompletedTask;
        public Task SetBattleReportPublicStatusAsync(Guid worldPlayerId, Guid battleReportId, bool isPublic)
        {
            LastPublicStatusRequest = (worldPlayerId, battleReportId, isPublic);
            return Task.CompletedTask;
        }
    }
}
