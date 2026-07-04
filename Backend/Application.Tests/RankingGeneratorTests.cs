using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Generators;
using Domain.User;
using System.Text.Json;

namespace Application.Tests;

public class RankingGeneratorTests
{
    [Fact]
    public void GenerateRankingSnapshotIncludesAllianceId()
    {
        var alliance = new Alliance
        {
            Id = Guid.NewGuid(),
            Name = "Legion",
            Tag = "LEG"
        };

        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            AllianceId = alliance.Id,
            Alliance = alliance,
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Player" },
            Cities = new List<City>()
        };

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Capital",
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            Buildings = new List<Building>
            {
                new()
                {
                    Type = BuildingTypeEnum.TownHall,
                    Level = 1
                }
            }
        };
        player.Cities.Add(city);

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            RankingGenerator.GenerateRankingSnapshot(tempPath, new List<City> { city }, TestData.BuildingReader());

            var json = File.ReadAllText(tempPath);
            var data = JsonSerializer.Deserialize<List<RankingEntryData>>(json);

            Assert.NotNull(data);
            var entry = Assert.Single(data);
            Assert.Equal(alliance.Id, entry.AllianceId);
            Assert.Equal(player.Id, entry.WorldPlayerId);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
