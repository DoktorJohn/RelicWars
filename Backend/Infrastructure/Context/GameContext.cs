using Domain.Entities;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Context
{
    public class GameContext : DbContext
    {
        public GameContext(DbContextOptions<GameContext> options) : base(options)
        {

        }

        public DbSet<Alliance> Alliances { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<WorldPlayer> WorldPlayers { get; set; } 
        public DbSet<World> World { get; set; }
        public DbSet<BaseJob> Jobs { get; set; }
        public DbSet<PlayerProfile> PlayerProfiles { get; set; }

        public DbSet<UnitDeployment> UnitDeployments { get; set; }
        public DbSet<UnitStack> UnitStacks { get; set; }

        public DbSet<BuildingJob> BuildingJobs { get; set; }
        public DbSet<RecruitmentJob> RecruitmentJobs { get; set; }
        public DbSet<Research> Researches { get; set; }
        public DbSet<BattleReport> BattleReports { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Fortæl EF eksplicit om dit hierarki
            modelBuilder.Entity<BaseJob>()
                .HasDiscriminator<string>("JobType") // EF opretter denne kolonne automatisk
                .HasValue<BuildingJob>("Building")
                .HasValue<RecruitmentJob>("Recruitment")
                .HasValue<ResearchJob>("Research"); // Dette SKAL matche klassenavnet eller en streng

            base.OnModelCreating(modelBuilder);
        }
    }
}
