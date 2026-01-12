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
            if (await context.Cities.AnyAsync()) return;

            Console.WriteLine("--- Seeding World & Initial Data ---");

            // 1. Opret Verden
            var world = new World("Alpha World", "ALFA", 100, 100);
            context.World.Add(world);

            // Gem grunddata først, så spawneren kan se at pladsen (50,50) er optaget
            await context.SaveChangesAsync();

            //// 4. Spawn NPC byer omkring spilleren
            //Console.WriteLine("--- Spawning NPC Cities ---");
            //await spawner.SpawnInitialNPCsAsync(30, 40); // 30 byer inden for en radius af 40

        }
    }
}
