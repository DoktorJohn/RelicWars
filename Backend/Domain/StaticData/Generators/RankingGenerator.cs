using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Domain.StaticData.Generators
{
    public static class RankingGenerator
    {
        /// <summary>
        /// Gennemløber alle byer, udregner point for hver spiller og skriver en sorteret rangliste til JSON.
        /// </summary>
        public static void GenerateRankingSnapshot(string storagePath, List<City> allCities, BuildingDataReader buildingDataReader)
        {
            var playerPointMap = new Dictionary<Guid, RankingEntryData>();

            foreach (var city in allCities)
            {
                if (city.WorldPlayer == null) continue;

                Guid worldPlayerId = city.WorldPlayer.Id;
                int pointsCalculatedFromCity = CalculateTotalPointsInSpecificCity(city, buildingDataReader);

                if (playerPointMap.TryGetValue(worldPlayerId, out var existingEntry))
                {
                    existingEntry.TotalPoints += pointsCalculatedFromCity;
                    existingEntry.CityCount += 1;
                }
                else
                {
                    playerPointMap[worldPlayerId] = new RankingEntryData
                    {
                        WorldPlayerId = worldPlayerId, // VIGTIGT: Gemmer ID til senere opslag
                        PlayerName = city.WorldPlayer.PlayerProfile.UserName!,
                        AllianceName = "Ingen Alliance",
                        TotalPoints = pointsCalculatedFromCity,
                        CityCount = 1,
                        LastUpdated = DateTime.UtcNow
                    };
                }
            }

            var sortedRankings = playerPointMap.Values
                .OrderByDescending(entry => entry.TotalPoints)
                .ToList();

            for (int i = 0; i < sortedRankings.Count; i++)
            {
                sortedRankings[i].Rank = i + 1;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(storagePath, JsonSerializer.Serialize(sortedRankings, options));
        }

        private static int CalculateTotalPointsInSpecificCity(City city, BuildingDataReader reader)
        {
            int accumulatedPoints = 0;

            foreach (var building in city.Buildings)
            {
                if (building.Level <= 0) continue;

                // Vi henter point-værdien for det specifikke niveau fra vores statiske data
                var buildingLevelData = reader.GetConfig<BuildingLevelData>(building.Type, building.Level);
                if (buildingLevelData != null)
                {
                    accumulatedPoints += buildingLevelData.Points;
                }
            }

            return accumulatedPoints;
        }
    }
}