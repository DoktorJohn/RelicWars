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
        public DbSet<AllianceInvitation> AllianceInvitations { get; set; }
        public DbSet<AllianceRelation> AllianceRelations { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<CityExoticResource> CityExoticResources { get; set; }
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
        public DbSet<WorldMapObject> WorldMapObjects { get; set; }
        public DbSet<WorldIsland> WorldIslands { get; set; }
        public DbSet<WorldIslandExoticResource> WorldIslandExoticResources { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<BugReport> BugReports { get; set; }
        public DbSet<DailyObjectiveSet> DailyObjectiveSets { get; set; }
        public DbSet<DailyObjectiveAssignment> DailyObjectiveAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var enumListConverter = new EnumListConverter<ModifierTagEnum>();

            // Definerer en Comparer, så EF Core kan se ændringer i dine lister
            var enumListComparer = new ValueComparer<List<ModifierTagEnum>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            // Konfiguration af lister med Enums
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

            modelBuilder.Entity<BaseJob>()
                .HasIndex(job => new { job.IsCompleted, job.ExecutionTime })
                .HasDatabaseName("IX_Jobs_Due");

            // --- UNIT DEPLOYMENT KONFIGURATION (Løsning på Multiple Cascade Paths) ---
            modelBuilder.Entity<UnitDeployment>(entity =>
            {
                entity.Property(deployment => deployment.DepartureTime).HasColumnType("datetime2(3)");
                entity.Property(deployment => deployment.ArrivalTime).HasColumnType("datetime2(3)");
                entity.Property(deployment => deployment.StationedAt).HasColumnType("datetime2(3)");

                entity.HasIndex(deployment => new { deployment.Phase, deployment.UnitDeploymentMovementStatus, deployment.ArrivalTime })
                    .HasDatabaseName("IX_UnitDeployments_DueMovement");

                entity.HasIndex(deployment => new { deployment.TargetCityId, deployment.Type, deployment.Phase })
                    .HasDatabaseName("IX_UnitDeployments_TargetSupport");

                entity.HasIndex(deployment => deployment.WorldId)
                    .HasDatabaseName("IX_UnitDeployments_WorldId");
                // FIX: Sæt World relation til Restrict for at undgå multiple cascade paths i SQL Server
                entity.HasOne(ud => ud.World)
                    .WithMany()
                    .HasForeignKey(ud => ud.WorldId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ud => ud.TargetCity)
                    .WithMany(c => c.TargetUnitDeployments)
                    .HasForeignKey(ud => ud.TargetCityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ud => ud.OriginCity)
                    .WithMany(c => c.OriginUnitDeployments)
                    .HasForeignKey(ud => ud.OriginCityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ud => ud.OwnerWorldPlayer)
                    .WithMany(p => p.UnitDeployments)
                    .HasForeignKey(ud => ud.WorldPlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UnitStack>(entity =>
            {
                entity.HasOne(us => us.City)
                    .WithMany(c => c.UnitStacks)
                    .HasForeignKey(us => us.CityId)
                    .OnDelete(DeleteBehavior.Restrict); // FIX: Restrict for at undgå multiple paths fra WorldPlayer

                entity.HasOne(us => us.UnitDeployment)
                    .WithMany(ud => ud.UnitStacks)
                    .HasForeignKey(us => us.UnitDeploymentId)
                    .OnDelete(DeleteBehavior.Cascade); // Vi beholder cascade her, da en hær slettes ofte
            });

            modelBuilder.Entity<Building>()
                .HasOne(b => b.City)
                .WithMany(c => c.Buildings)
                .HasForeignKey(b => b.CityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorldMapObject>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.WorldId, e.X, e.Y, e.Type })
                      .IsUnique()
                      .HasDatabaseName("UX_WorldMapObjects_World_Coordinates_Type");

                entity.HasOne(d => d.World)
                      .WithMany(p => p.MapObjects)
                      .HasForeignKey(d => d.WorldId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Type).HasConversion<byte>();
            });

            modelBuilder.Entity<City>()
                .HasIndex(c => new { c.WorldId, c.X, c.Y })
                .IsUnique()
                .HasDatabaseName("UX_Cities_World_Coordinates");

            modelBuilder.Entity<CityExoticResource>(entity =>
            {
                entity.HasIndex(resource => new { resource.CityId, resource.ResourceType })
                    .IsUnique()
                    .HasDatabaseName("IX_CityExoticResources_City_Type");

                entity.HasOne(resource => resource.City)
                    .WithMany(city => city.ExoticResources)
                    .HasForeignKey(resource => resource.CityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(resource => resource.ResourceType).HasConversion<int>();
            });

            modelBuilder.Entity<WorldPlayer>()
                .HasIndex(player => new { player.PlayerProfileId, player.WorldId })
                .IsUnique()
                .HasDatabaseName("UX_WorldPlayers_Profile_World");

            modelBuilder.Entity<DailyObjectiveSet>(entity =>
            {
                entity.Property(set => set.DayStartUtc).HasColumnType("date");
                entity.Property(set => set.RowVersion).IsRowVersion();
                entity.HasIndex(set => set.WorldPlayerId).IsUnique();
                entity.HasOne(set => set.WorldPlayer)
                    .WithOne(player => player.DailyObjectiveSet)
                    .HasForeignKey<DailyObjectiveSet>(set => set.WorldPlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DailyObjectiveAssignment>(entity =>
            {
                entity.Property(assignment => assignment.RowVersion).IsRowVersion();
                entity.HasIndex(assignment => new { assignment.DailyObjectiveSetId, assignment.Slot }).IsUnique();
                entity.HasIndex(assignment => new { assignment.DailyObjectiveSetId, assignment.DefinitionId }).IsUnique();
                entity.HasOne(assignment => assignment.DailyObjectiveSet)
                    .WithMany(set => set.Assignments)
                    .HasForeignKey(assignment => assignment.DailyObjectiveSetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasMany(c => c.Participants)
                    .WithOne(p => p.Conversation)
                    .HasForeignKey(p => p.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WorldIsland>(entity =>
            {
                entity.HasIndex(island => new { island.WorldId, island.CellX, island.CellY })
                    .IsUnique()
                    .HasDatabaseName("IX_WorldIslands_World_Cell");

                entity.HasOne(island => island.World)
                    .WithMany(world => world.Islands)
                    .HasForeignKey(island => island.WorldId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(island => island.Shape).HasConversion<int>();
            });

            modelBuilder.Entity<WorldIslandExoticResource>(entity =>
            {
                entity.HasIndex(resource => new { resource.WorldIslandId, resource.SlotIndex })
                    .IsUnique()
                    .HasDatabaseName("IX_WorldIslandExoticResources_Island_Slot");

                entity.HasIndex(resource => new { resource.WorldIslandId, resource.ResourceType })
                    .IsUnique()
                    .HasDatabaseName("IX_WorldIslandExoticResources_Island_Type");

                entity.HasOne(resource => resource.WorldIsland)
                    .WithMany(island => island.ExoticResources)
                    .HasForeignKey(resource => resource.WorldIslandId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(resource => resource.ResourceType).HasConversion<int>();
                entity.Property(resource => resource.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.HasOne(cp => cp.WorldPlayer)
                    .WithMany(p => p.ConversationParticipants)
                    .HasForeignKey(cp => cp.WorldPlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(cp => new { cp.ConversationId, cp.WorldPlayerId })
                    .IsUnique();
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BugReport>(entity =>
            {
                entity.Property(report => report.Description).HasMaxLength(4000).IsRequired();
                entity.HasIndex(report => report.PlayerProfileId);
                entity.HasOne(report => report.PlayerProfile)
                    .WithMany()
                    .HasForeignKey(report => report.PlayerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AllianceInvitation>(entity =>
            {
                entity.HasOne(i => i.Alliance).WithMany().HasForeignKey(i => i.AllianceId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(i => i.InvitedWorldPlayer).WithMany().HasForeignKey(i => i.InvitedWorldPlayerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.InvitedByWorldPlayer).WithMany().HasForeignKey(i => i.InvitedByWorldPlayerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(i => new { i.AllianceId, i.InvitedWorldPlayerId });
            });

            modelBuilder.Entity<Alliance>(entity =>
            {
                entity.HasOne(a => a.World).WithMany().HasForeignKey(a => a.WorldId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(a => new { a.WorldId, a.Name });
            });

            modelBuilder.Entity<AllianceRelation>(entity =>
            {
                entity.Property(r => r.RelationType).HasConversion<int>();
                entity.Property(r => r.Status).HasConversion<int>();
                entity.HasOne(r => r.World).WithMany().HasForeignKey(r => r.WorldId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.AllianceA).WithMany(a => a.RelationsAsAllianceA).HasForeignKey(r => r.AllianceIdA).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.AllianceB).WithMany(a => a.RelationsAsAllianceB).HasForeignKey(r => r.AllianceIdB).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(r => new { r.AllianceIdA, r.AllianceIdB, r.RelationType, r.Status });
                entity.HasCheckConstraint("CK_AllianceRelations_DifferentAlliances", "[AllianceIdA] <> [AllianceIdB]");
            });

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
