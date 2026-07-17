using Application.Interfaces.IRepositories;
using Domain.Entities;
using Domain.Enums;
using Domain.Workers;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CityRepository : ICityRepository
    {
        private readonly GameContext _context;

        public CityRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<City?> GetByIdAsync(Guid cityIdentifier)
        {
            return await _context.Cities
                .AsSplitQuery()
                .Include(city => city.Buildings)
                .Include(city => city.UnitStacks)
                .Include(city => city.ExoticResources)
                .Include(city => city.ActiveFocuses)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player.ModifiersInternal)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player.CompletedResearches)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player.Cities)
                        .ThenInclude(c => c.Buildings)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player.Cities)
                        .ThenInclude(c => c.UnitStacks)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player.Cities)
                        .ThenInclude(c => c.ActiveFocuses)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player.Cities)
                        .ThenInclude(c => c.OriginUnitDeployments)
                            .ThenInclude(d => d.UnitStacks)
                .FirstOrDefaultAsync(city => city.Id == cityIdentifier);
        }

        public async Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityIdentifier)
        {
            return await _context.Cities
                 .AsSplitQuery()
                 .Include(city => city.Buildings)
                 .Include(city => city.ActiveFocuses)
                 .Include(city => city.ExoticResources)
                 .Include(city => city.WorldPlayer)
                     .ThenInclude(worldPlayer => worldPlayer.PlayerProfile)
                 .Include(city => city.WorldPlayer)
                     .ThenInclude(player => player.ModifiersInternal)
                 .Include(city => city.WorldPlayer)
                     .ThenInclude(player => player.CompletedResearches)
                 .Include(city => city.WorldPlayer)
                     .ThenInclude(player => player.Alliance) 
                 .Include(city => city.WorldPlayer)
                     .ThenInclude(player => player.Cities)
                         .ThenInclude(c => c.Buildings)
                 .Include(city => city.WorldPlayer)
                     .ThenInclude(player => player.Cities)
                         .ThenInclude(c => c.UnitStacks)
                 .Include(city => city.WorldPlayer)
                     .ThenInclude(player => player.Cities)
                         .ThenInclude(c => c.ActiveFocuses)
                 .Include(city => city.WorldPlayer)
                     .ThenInclude(player => player.Cities)
                         .ThenInclude(c => c.OriginUnitDeployments)
                             .ThenInclude(d => d.UnitStacks)
                 .Include(city => city.UnitStacks)
                 .Include(city => city.OriginUnitDeployments)
                     .ThenInclude(deployment => deployment.UnitStacks)
                 .FirstOrDefaultAsync(city => city.Id == cityIdentifier);
        }

        public async Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityIdentifier)
        {
            return await _context.Cities
                .AsSplitQuery()
                .AsNoTracking()
                .Include(city => city.Buildings)
                .Include(city => city.ActiveFocuses)
                .Include(city => city.ModifiersInternal)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(worldPlayer => worldPlayer.ModifiersInternal)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(worldPlayer => worldPlayer.CompletedResearches)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(worldPlayer => worldPlayer.Alliance)
                        .ThenInclude(alliance => alliance.ModifiersInternal)
                .FirstOrDefaultAsync(city => city.Id == cityIdentifier);
        }

        public Task<City?> GetForJobProcessingAsync(Guid cityIdentifier, bool includeWorldPlayer)
        {
            IQueryable<City> query = _context.Cities
                .AsSplitQuery()
                .Include(city => city.Buildings)
                .Include(city => city.UnitStacks)
                .Include(city => city.ExoticResources)
                .Include(city => city.ActiveFocuses)
                .Include(city => city.ModifiersInternal);

            if (includeWorldPlayer)
            {
                query = query
                    .Include(city => city.WorldPlayer)
                        .ThenInclude(player => player!.ModifiersInternal)
                    .Include(city => city.WorldPlayer)
                        .ThenInclude(player => player!.CompletedResearches)
                    .Include(city => city.WorldPlayer)
                        .ThenInclude(player => player!.Cities)
                            .ThenInclude(playerCity => playerCity.Buildings)
                    .Include(city => city.WorldPlayer)
                        .ThenInclude(player => player!.Cities)
                            .ThenInclude(playerCity => playerCity.UnitStacks)
                    .Include(city => city.WorldPlayer)
                        .ThenInclude(player => player!.Cities)
                            .ThenInclude(playerCity => playerCity.ActiveFocuses);
            }

            return query.FirstOrDefaultAsync(city => city.Id == cityIdentifier);
        }

        public async Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids)
        {
            return await _context.Cities
                .Where(c => ids.Contains(c.Id))
                .Include(city => city.ExoticResources)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(worldPlayer => worldPlayer!.Alliance)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(worldPlayer => worldPlayer!.PlayerProfile)
                .ToListAsync();
        }

        public async Task<List<City>> GetAllAsync()
        {
            return await _context.Cities
                .Include(cityEntity => cityEntity.Buildings)
                .Include(cityEntity => cityEntity.ExoticResources)

                .Include(cityEntity => cityEntity.UnitStacks)

                .Include(cityEntity => cityEntity.WorldPlayer)
                    .ThenInclude(playerEntity => playerEntity!.PlayerProfile)

                .Include(cityEntity => cityEntity.WorldPlayer)
                    .ThenInclude(playerEntity => playerEntity!.ModifiersInternal)

                .Include(cityEntity => cityEntity.WorldPlayer)
                    .ThenInclude(playerEntity => playerEntity!.Alliance)

                .ToListAsync();
        }

        public Task<List<City>> GetNPCsForBuildingAutomationAsync()
        {
            return _context.Cities
                .AsSplitQuery()
                .Where(city => city.IsNPC &&
                               city.WorldPlayerId == null &&
                               city.Points < Application.Services.Workers.NPCBuildingWorker.TargetCityPoints &&
                               !_context.Jobs.OfType<BuildingJob>().Any(job =>
                                   !job.IsCompleted && job.CityId == city.Id))
                .Include(city => city.Buildings)
                .Include(city => city.ExoticResources)
                .Include(city => city.ActiveFocuses)
                .Include(city => city.ModifiersInternal)
                .ToListAsync();
        }

        public Task<List<City>> GetCitiesForNPCBackfillAsync()
        {
            return _context.Cities
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<City?> GetByCoordinatesAsync(int x, int y)
        {
            return await _context.Cities
                .AsSplitQuery()
                .Include(city => city.Buildings)
                .Include(city => city.ActiveFocuses)
                .Include(city => city.ExoticResources)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player!.ModifiersInternal)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player!.CompletedResearches)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player!.Alliance)
                .Include(city => city.UnitStacks)
                .FirstOrDefaultAsync(city => city.X == x && city.Y == y);
        }

        public async Task UpdateAsync(City city)
        {
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(City city)
        {
            await _context.Cities.AddAsync(city);
            await _context.SaveChangesAsync();
        }

        public async Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities)
        {
            if (cities.Count == 0)
            {
                return;
            }

            var mapObjects = cities.Select(city => new WorldMapObject
            {
                Id = Guid.NewGuid(),
                WorldId = city.WorldId,
                X = checked((short)city.X),
                Y = checked((short)city.Y),
                Type = MapObjectTypeEnum.City,
                ReferenceEntityId = city.Id
            }).ToList();

            await _context.Cities.AddRangeAsync(cities);
            await _context.WorldMapObjects.AddRangeAsync(mapObjects);
            await _context.SaveChangesAsync();
        }

        public async Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId)
        {
            return await _context.Cities
                .Where(c => c.WorldPlayerId == worldPlayerId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId)
        {
            var city = await _context.Cities
                .AsNoTracking()
                .Select(c => new { c.Id, c.WorldPlayerId })
                .FirstOrDefaultAsync(c => c.Id == cityId);

            return city?.WorldPlayerId;
        }

        public async Task UpdateRangeAsync(List<City> cities)
        {
            _context.Cities.UpdateRange(cities);
            await _context.SaveChangesAsync();
        }
    }
}
