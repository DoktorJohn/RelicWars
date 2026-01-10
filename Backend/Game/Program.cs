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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUnity", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

string buildingPath = "buildings.json";
string unitPath = "units.json";
string researchPath = "research.json";

if (!File.Exists(buildingPath)) BuildingDataGenerator.GenerateDefaultJson(buildingPath);
if (!File.Exists(unitPath)) UnitDataGenerator.GenerateDefaultJson(unitPath);
if (!File.Exists(researchPath)) ResearchDataGenerator.GenerateDefaultJson(researchPath);

var buildingReader = new BuildingDataReader();
buildingReader.Load(buildingPath);
var unitReader = new UnitDataReader();
unitReader.Load(unitPath);
var researchReader = new ResearchDataReader();
researchReader.Load(researchPath);

builder.Services.AddSingleton(buildingReader);
builder.Services.AddSingleton(unitReader);
builder.Services.AddSingleton(researchReader);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("ConnectionString 'DefaultConnection' mangler!");

builder.Services.AddDbContext<GameContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IWorldPlayerRepository, WorldPlayerRepository>();
builder.Services.AddScoped<IUnitDeploymentRepository, UnitDeploymentRepository>();
builder.Services.AddScoped<IBattleReportRepository, BattleReportRepository>();
builder.Services.AddScoped<IPlayerProfileRepository, PlayerProfileRepository>();
builder.Services.AddScoped<IWorldRepository, WorldRepository>();

builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<CombatService>();
builder.Services.AddScoped<ICityStatService, CityStatService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
builder.Services.AddScoped<IResearchService, ResearchService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<NPCSpawnerService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameWorldService, GameWorldService>();
builder.Services.AddScoped<IJwtService, JwtService>();


builder.Services.AddScoped<IResourceService, ResourceService>();

builder.Services.AddScoped<CityWorker>();
builder.Services.AddScoped<UnitDeploymentWorker>();

builder.Services.AddHostedService<GameEngineWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameContext>();
    var spawner = scope.ServiceProvider.GetRequiredService<NPCSpawnerService>();
    await context.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(context, spawner);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowUnity"); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();