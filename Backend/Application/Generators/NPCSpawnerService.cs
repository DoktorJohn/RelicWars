using Application.Interfaces.IRepositories;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Generators
{
    public class NPCSpawnerService
    {
        private readonly ICityRepository _cityRepo;
        private readonly Random _random = new();

        public NPCSpawnerService(ICityRepository cityRepo)
        {
            _cityRepo = cityRepo;
        }

        public async Task SpawnInitialNPCsAsync(int count, int mapRange)
        {
            var existingCities = await _cityRepo.GetAllAsync();

            // Vi bruger en HashSet til hurtigt at tjekke om koordinaterne er optaget
            var occupied = existingCities.Select(c => (c.X, c.Y)).ToHashSet();

            for (int i = 0; i < count; i++)
            {
                int x, y;
                int attempts = 0;

                // Prøv at finde en ledig plads (max 100 forsøg så vi ikke looper evigt)
                do
                {
                    x = _random.Next(-mapRange, mapRange);
                    y = _random.Next(-mapRange, mapRange);
                    attempts++;
                } while (occupied.Contains((x, y)) && attempts < 100);

                if (attempts >= 100) continue;

                var npcCity = new City
                {
                    Id = Guid.NewGuid(),
                    Name = GenerateNPCName(),
                    X = x,
                    Y = y,
                    IsNPC = true,
                    Wood = 500,
                    Stone = 500,
                    Metal = 500,
                    LastResourceUpdate = DateTime.UtcNow,
                    Buildings = new List<Building>
                    {
                        new Building { Type = BuildingTypeEnum.TimberCamp, Level = _random.Next(1, 3) },
                        new Building { Type = BuildingTypeEnum.StoneQuarry, Level = _random.Next(1, 3) }
                    },
                    UnitStacks = new List<UnitStack>
                    {
                        new UnitStack { Type = UnitTypeEnum.Militia, Quantity = _random.Next(5, 25) }
                    }
                };

                await _cityRepo.AddAsync(npcCity);
                occupied.Add((x, y));
            }
        }

        private string GenerateNPCName()
        {
            string[] names = { "Ruins of", "Old", "Lost", "Shadow", "Iron", "Grim" };
            string[] sites = { "Crest", "Watch", "Keep", "Falls", "Mine", "Grave" };
            return $"{names[_random.Next(names.Length)]} {sites[_random.Next(sites.Length)]}";
        }
    }
}
