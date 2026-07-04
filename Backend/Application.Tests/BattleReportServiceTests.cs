using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using System.Text.Json;

namespace Application.Tests;

public class BattleReportServiceTests
{
    [Fact]
    public async Task GetBattleReportsAsync_ReturnsOwnedReportsSortedAndMapped()
    {
        var owner = Player("Owner");
        var other = Player("Other");
        var reportRepository = new MemoryBattleReportRepository(
            Report(owner.Id, DateTime.UtcNow.AddMinutes(-5), false),
            Report(owner.Id, DateTime.UtcNow, true),
            Report(other.Id, DateTime.UtcNow.AddMinutes(1), false));
        var service = new BattleReportService(reportRepository, new TestPlayerAccessService([owner, other]));

        var reports = await service.GetBattleReportsAsync(owner.Id);

        Assert.Equal(2, reports.Count);
        Assert.True(reports[0].OccurredAt > reports[1].OccurredAt);
        Assert.True(reports[0].IsRead);
        Assert.False(reports[1].IsRead);
        Assert.Equal(UnitTypeEnum.Militia, reports[0].AttackerLosses[0].Type);
        Assert.Equal(3, reports[0].AttackerLosses[0].Quantity);
        Assert.Contains("Banner", reports[0].AppliedModifiers);
        Assert.Equal(ReportTypeEnum.Battle, reports[0].ReportType);
    }

    [Fact]
    public async Task GetUnreadStatusAsync_CountsOnlyUnreadOwnedReports()
    {
        var owner = Player("Owner");
        var repository = new MemoryBattleReportRepository(
            Report(owner.Id, DateTime.UtcNow.AddMinutes(-5), false),
            Report(owner.Id, DateTime.UtcNow, true),
            Report(Guid.NewGuid(), DateTime.UtcNow, false));
        var service = new BattleReportService(repository, new TestPlayerAccessService([owner]));

        var status = await service.GetUnreadStatusAsync(owner.Id);

        Assert.Equal(1, status.UnreadCount);
    }

    [Fact]
    public async Task MarkBattleReportAsReadAsync_UpdatesUnreadState()
    {
        var owner = Player("Owner");
        var report = Report(owner.Id, DateTime.UtcNow, false);
        var repository = new MemoryBattleReportRepository(report);
        var service = new BattleReportService(repository, new TestPlayerAccessService([owner]));

        await service.MarkBattleReportAsReadAsync(owner.Id, report.Id);

        Assert.True(report.IsRead);
        Assert.Equal(1, repository.MarkAsReadCalls);
    }

    [Fact]
    public async Task MarkBattleReportAsReadAsync_RejectsMissingAndForeignReports()
    {
        var owner = Player("Owner");
        var other = Player("Other");
        var foreignReport = Report(other.Id, DateTime.UtcNow, false);
        var repository = new MemoryBattleReportRepository(foreignReport);
        var service = new BattleReportService(repository, new TestPlayerAccessService([owner, other]));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.MarkBattleReportAsReadAsync(owner.Id, Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.MarkBattleReportAsReadAsync(owner.Id, foreignReport.Id));
    }

    [Fact]
    public async Task DeleteBattleReportAsync_RemovesOwnedReport()
    {
        var owner = Player("Owner");
        var report = Report(owner.Id, DateTime.UtcNow, false);
        var repository = new MemoryBattleReportRepository(report);
        var service = new BattleReportService(repository, new TestPlayerAccessService([owner]));

        await service.DeleteBattleReportAsync(owner.Id, report.Id);

        Assert.Empty(repository.GetReportsFor(owner.Id));
        Assert.Equal(1, repository.DeleteCalls);
    }

    [Fact]
    public async Task DeleteBattleReportAsync_RejectsMissingAndForeignReports()
    {
        var owner = Player("Owner");
        var other = Player("Other");
        var foreignReport = Report(other.Id, DateTime.UtcNow, false);
        var repository = new MemoryBattleReportRepository(foreignReport);
        var service = new BattleReportService(repository, new TestPlayerAccessService([owner, other]));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DeleteBattleReportAsync(owner.Id, Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteBattleReportAsync(owner.Id, foreignReport.Id));
    }

    private static WorldPlayer Player(string name) => new()
    {
        Id = Guid.NewGuid(),
        PlayerProfileId = Guid.NewGuid(),
        PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = name }
    };

    private static BattleReport Report(Guid worldPlayerId, DateTime occurredAt, bool isRead) => new()
    {
        Id = Guid.NewGuid(),
        WorldPlayerId = worldPlayerId,
        Title = "Battle at the gate",
        Body = "A battle report",
        OccurredAt = occurredAt,
        IsRead = isRead,
        ReportType = ReportTypeEnum.Battle,
        AttackerLossesJson = JsonSerializer.Serialize(new[] { new UnitStackDTO(UnitTypeEnum.Militia, 3) }),
        DefenderLossesJson = JsonSerializer.Serialize(new[] { new UnitStackDTO(UnitTypeEnum.Militia, 2) }),
        RevivedUnitsJson = JsonSerializer.Serialize(new[] { new UnitStackDTO(UnitTypeEnum.Militia, 1) }),
        AppliedModifiersJson = JsonSerializer.Serialize(new[] { "Banner" })
    };

    private sealed class MemoryBattleReportRepository : IBattleReportRepository
    {
        private readonly List<BattleReport> _reports = [];

        public MemoryBattleReportRepository(params BattleReport[] reports)
        {
            _reports.AddRange(reports);
        }

        public int MarkAsReadCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public Task AddAsync(BattleReport report)
        {
            _reports.Add(report);
            return Task.CompletedTask;
        }

        public Task<BattleReport?> GetByIdAsync(Guid reportId) =>
            Task.FromResult(_reports.SingleOrDefault(report => report.Id == reportId));

        public Task<List<BattleReport>> GetByUserIdAsync(Guid userId) =>
            Task.FromResult(_reports.Where(report => report.WorldPlayerId == userId).OrderByDescending(report => report.OccurredAt).ToList());

        public Task<int> GetUnreadCountAsync(Guid userId) =>
            Task.FromResult(_reports.Count(report => report.WorldPlayerId == userId && !report.IsRead));

        public Task MarkAsReadAsync(Guid reportId)
        {
            var report = _reports.SingleOrDefault(entry => entry.Id == reportId);
            if (report != null)
            {
                report.IsRead = true;
                MarkAsReadCalls++;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid reportId)
        {
            var report = _reports.SingleOrDefault(entry => entry.Id == reportId);
            if (report != null)
            {
                _reports.Remove(report);
                DeleteCalls++;
            }

            return Task.CompletedTask;
        }

        public List<BattleReport> GetReportsFor(Guid userId) =>
            _reports.Where(report => report.WorldPlayerId == userId).ToList();
    }
}
