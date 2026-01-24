using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public DbSet<IdeologyFocus> IdeologyFocuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var enumListConverter = new EnumListConverter<ModifierTagEnum>();

            // Definerer en Comparer, så EF Core kan se ændringer i dine lister
            var enumListComparer = new ValueComparer<List<ModifierTagEnum>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            // Konfiguration af lister med Enums (Nu med Comparer for at fjerne advarsler)
            ConfigureEnumListProperty<City>(modelBuilder, e => e.ModifiersThatAffectsThis, enumListConverter, enumListComparer);
            ConfigureEnumListProperty<WorldPlayer>(modelBuilder, e => e.ModifiersThatAffectsThis, enumListConverter, enumListComparer);
            ConfigureEnumListProperty<UnitDeployment>(modelBuilder, e => e.ModifiersThatAffectsThis, enumListConverter, enumListComparer);
            ConfigureEnumListProperty<UnitStack>(modelBuilder, e => e.ModifiersThatAffectsThis, enumListConverter, enumListComparer);

            // Owned Entities (ModifiersInternal)
            ConfigureModifierStorage<City>(modelBuilder, "CityModifiers");
            ConfigureModifierStorage<Alliance>(modelBuilder, "AllianceModifiers");
            ConfigureModifierStorage<WorldPlayer>(modelBuilder, "PlayerModifiers");
            ConfigureModifierStorage<UnitDeployment>(modelBuilder, "UnitDeploymentModifiers");
            ConfigureModifierStorage<UnitStack>(modelBuilder, "UnitStackModifiers");
            ConfigureModifierStorage<World>(modelBuilder, "WorldModifiers");

            // Hierarki for Jobs
            modelBuilder.Entity<BaseJob>()
                .HasDiscriminator<string>("JobType")
                .HasValue<BuildingJob>("Building")
                .HasValue<RecruitmentJob>("RecruitmentSpeed")
                .HasValue<ResearchJob>("Research");

            modelBuilder.Entity<UnitDeployment>()
                .HasOne(ud => ud.OriginCity)
                .WithMany(c => c.OriginUnitDeployments)
                .HasForeignKey(ud => ud.OriginCityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnitDeployment>()
                .HasOne(ud => ud.TargetCity)
                .WithMany(c => c.TargetUnitDeployments)
                .HasForeignKey(ud => ud.TargetCityId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }

        private void ConfigureEnumListProperty<T>(
            ModelBuilder modelBuilder,
            System.Linq.Expressions.Expression<Func<T, List<ModifierTagEnum>>> propertyExpression,
            ValueConverter<List<ModifierTagEnum>, string> converter,
            ValueComparer<List<ModifierTagEnum>> comparer) where T : class
        {
            modelBuilder.Entity<T>()
                .Property(propertyExpression)
                .HasConversion(converter)
                .Metadata.SetValueComparer(comparer);
        }

        private void ConfigureModifierStorage<T>(ModelBuilder modelBuilder, string tableName) where T : class
        {
            modelBuilder.Entity<T>().OwnsMany<Modifier>("ModifiersInternal", a =>
            {
                a.ToTable(tableName);
                a.WithOwner().HasForeignKey(typeof(T).Name + "Id");
                a.Property<int>("Id");
                a.HasKey("Id");
            });
        }

        public class EnumListConverter<TEnum> : ValueConverter<List<TEnum>, string> where TEnum : Enum
        {
            public EnumListConverter() : base(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<TEnum>>(v) ?? new List<TEnum>())
            { }
        }
    }
}