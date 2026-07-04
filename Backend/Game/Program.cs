using Microsoft.EntityFrameworkCore;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Application.Services;
using Application.Interfaces.IServices;
using Application.Interfaces.IRepositories;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
using Application.Services.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Application.Generators;
using Application.Services.Authentication;
using Application.Utility;
using Application.Interfaces.IServices.IBuildings;
using Application.Services.Buildings;
using Application.Services.Jobs;
using Infrastructure.Workers;
using Game.Services;
using Game.Middleware;
using Application.Interfaces;
using Infrastructure.Persistence;
using Game.Contracts;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUnity", policy =>
    {
        policy.WithOrigins(allowedOrigins!)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("ConnectionString 'DefaultConnection' mangler!");

builder.Services.AddDbContext<GameContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        if (!builder.Environment.IsDevelopment())
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }
    });
});

string buildingPath = "buildings.json";
string unitPath = "units.json";
string researchPath = "research.json";
string rankingPath = "rankings.json";
string ideologyPath = "ideologies.json";
string ideologyFocusPath = "ideologyFocus.json";
string exoticResourcePath = "exotic-resources.json";

if (!File.Exists(buildingPath)) BuildingDataGenerator.GenerateDefaultJson(buildingPath);
if (!File.Exists(unitPath)) UnitDataGenerator.GenerateDefaultJson(unitPath);
if (!File.Exists(researchPath)) ResearchDataGenerator.GenerateDefaultJson(researchPath);
if (!File.Exists(ideologyPath)) IdeologyDataGenerator.GenerateDefaultJson(ideologyPath);
if (!File.Exists(ideologyFocusPath)) IdeologyFocusDataGenerator.GenerateDefaultJson(ideologyFocusPath);
if (!File.Exists(exoticResourcePath)) ExoticResourceDataGenerator.GenerateDefaultJson(exoticResourcePath);

var buildingReader = new BuildingDataReader();
buildingReader.Load(buildingPath);
var unitReader = new UnitDataReader();
unitReader.Load(unitPath);
var researchReader = new ResearchDataReader();
researchReader.Load(researchPath);
var rankingReader = new RankingDataReader();
rankingReader.Load(rankingPath);
var ideologyReader = new IdeologyDataReader();
ideologyReader.Load(ideologyPath);
var ideologyFocusReader = new IdeologyFocusDataReader();
ideologyFocusReader.Load(ideologyFocusPath);
var exoticResourceReader = new ExoticResourceDataReader();
exoticResourceReader.Load(exoticResourcePath);

builder.Services.AddSingleton(buildingReader);
builder.Services.AddSingleton(unitReader);
builder.Services.AddSingleton(researchReader);
builder.Services.AddSingleton(rankingReader);
builder.Services.AddSingleton(ideologyReader);
builder.Services.AddSingleton(ideologyFocusReader);
builder.Services.AddSingleton(exoticResourceReader);


builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<IWorldMapObjectRepository, WorldMapObjectRepository>();
builder.Services.AddScoped<IWorldMapObjectService, WorldMapObjectService>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IWorldPlayerRepository, WorldPlayerRepository>();
builder.Services.AddScoped<IUnitDeploymentRepository, UnitDeploymentRepository>();
builder.Services.AddScoped<IBattleReportRepository, BattleReportRepository>();
builder.Services.AddScoped<IBugReportRepository, BugReportRepository>();
builder.Services.AddScoped<IPlayerProfileRepository, PlayerProfileRepository>();
builder.Services.AddScoped<IWorldRepository, WorldRepository>();
builder.Services.AddScoped<IWorldIslandRepository, WorldIslandRepository>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IExoticResourceService, ExoticResourceService>();
builder.Services.AddScoped<IResistanceService, ResistanceService>();
builder.Services.AddScoped<CombatService>();
builder.Services.AddScoped<ICityStatService, CityStatService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
builder.Services.AddScoped<IMarketPlaceService, MarketPlaceService>();
builder.Services.AddScoped<IResearchService, ResearchService>();
builder.Services.AddScoped<ITownHallService, TownHallService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<NPCSpawnerService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorldService, WorldService>();
builder.Services.AddScoped<IWorldPlayerService, WorldPlayerService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<CityWorker>();
builder.Services.AddScoped<UnitDeploymentWorker>();
builder.Services.AddScoped<RecruitmentTimeCalculationService>();
builder.Services.AddScoped<ConstructionTimeCalculator>();
builder.Services.AddHostedService<GameEngineWorker>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IResourceBuildingService, ResourceBuildingService>();
builder.Services.AddScoped<IHousingService, HousingService>();
builder.Services.AddScoped<IBarracksService, BarracksService>();
builder.Services.AddScoped<IStableService, StableService>();
builder.Services.AddScoped<IWorkshopService, WorkshopService>();
builder.Services.AddScoped<IWallService, WallService>();
builder.Services.AddScoped<IUniversityService, UniversityService>();
builder.Services.AddScoped<InstantUtility>();
builder.Services.AddScoped<InstantFocusGrantService>();
builder.Services.AddScoped<FocusEnactmentPolicy>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddScoped<IAllianceService, AllianceService>();
builder.Services.AddScoped<IAllianceRepository, AllianceRepository>();
builder.Services.AddScoped<IModifierService, ModifierService>();
builder.Services.AddScoped<IModifierCollectorService, ModifierCollectorService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRandomService, RandomService>();
builder.Services.AddScoped<IMessagingRepository, MessagingRepository>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IBattleReportService, BattleReportService>();
builder.Services.AddScoped<IBugReportService, BugReportService>();
builder.Services.AddScoped<IUnitDeploymentService, UnitDeploymentService>();
builder.Services.AddScoped<IDeploymentPermissionService, DeploymentPermissionService>();
builder.Services.AddScoped<DeploymentModifierSnapshotService>();
builder.Services.AddScoped<UnitMovementCalculator>();
builder.Services.AddScoped<CityPointCalculator>();
builder.Services.AddScoped<IIdeologyFocusRepository, IdeologyFocusRepository>();
builder.Services.AddScoped<IIdeologyFocusService, IdeologyFocusService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPlayerAccessService, PlayerAccessService>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiErrorResultFilter>();
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new ApiError(
            "request.validation_failed",
            "Anmodningen indeholder ugyldige felter.",
            details));
    };
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameContext>();
    var spawner = scope.ServiceProvider.GetRequiredService<NPCSpawnerService>();

    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context, spawner);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowUnity");
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
