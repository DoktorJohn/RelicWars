using Application.DTOs;
using Application.Interfaces.IServices;
using Domain.Enums;
using Game.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Game.Tests;

public class DailyObjectivesControllerTests
{
    [Fact]
    public async Task Get_returns_authoritative_daily_shape()
    {
        Guid worldPlayerId = Guid.NewGuid();
        var day = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
        var expected = new DailyObjectivesDTO(day, day.AddDays(1), new()
        {
            new(1, 29, "Last Stand", "Successfully defend against 1 enemy attack",
                DailyObjectiveTierEnum.Fixed, 0, 1, DailyObjectiveStateEnum.InProgress)
        });
        var service = new StubDailyObjectiveService(expected);
        var controller = new DailyObjectivesController(service);

        ActionResult<DailyObjectivesDTO> action = await controller.Get(worldPlayerId);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        Assert.Equal(worldPlayerId, service.RequestedWorldPlayerId);
        Assert.DoesNotContain(typeof(DailyObjectivesController).GetMethods(), method =>
            method.Name.Contains("Claim", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubDailyObjectiveService : IDailyObjectiveService
    {
        private readonly DailyObjectivesDTO _response;
        public StubDailyObjectiveService(DailyObjectivesDTO response) => _response = response;
        public Guid RequestedWorldPlayerId { get; private set; }
        public Task<DailyObjectivesDTO> GetAsync(Guid worldPlayerId)
        {
            RequestedWorldPlayerId = worldPlayerId;
            return Task.FromResult(_response);
        }
        public Task ApplyProgressAsync(Guid worldPlayerId, DailyObjectiveProgressEvent progressEvent) => Task.CompletedTask;
        public Task ApplyProductionAsync(Guid worldPlayerId, DateTime intervalStartUtc, DateTime intervalEndUtc,
            double coinsPerHour = 0, double exoticResourcesPerHour = 0) => Task.CompletedTask;
    }
}
