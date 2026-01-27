using Domain.Enums;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Generators;

namespace Infrastructure.Context
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(GameContext context, NPCSpawnerService spawner)
        {
            if (await context.World.AnyAsync()) return;

            Console.WriteLine("--- Seeding World & Initial Data ---");

            var world = new World
            {
                Id = Guid.NewGuid(),
                Name = "Alpha World",
                Abbrevation = "ALFA",
                Width = 1000,
                Height = 1000,
                MapSeed = 42069,
                PlayerCount = 0
            };

            context.World.Add(world);
            await context.SaveChangesAsync();
        }
    }
}
