using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;

namespace Infrastructure.Repositories
{
    public class UnitDeploymentRepository : IUnitDeploymentRepository
    {
        private readonly GameContext _context;

        public UnitDeploymentRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<List<UnitDeployment>> GetUnitDeploymentsWithStacksByListOfIdsAsync(List<Guid> ids)
        {
            return await _context.UnitDeployments
                .Include(ud => ud.OriginCity)
                .Include(ud => ud.TargetCity)
                .Include(ud => ud.UnitStacks)
                .Include(ud => ud.OwnerWorldPlayer)
                    .ThenInclude(ud => ud!.PlayerProfile)
                .Where(ud => ids.Contains(ud.Id))
                .ToListAsync();
        }

        public async Task<List<UnitDeployment>> GetActiveDeploymentsByWorldPlayerIdAsync(Guid worldPlayerId)
        {
            return await _context.UnitDeployments
                .AsNoTracking()
                .Include(ud => ud.OriginCity)
                    .ThenInclude(city => city.WorldPlayer)
                        .ThenInclude(player => player!.PlayerProfile)
                .Include(ud => ud.OriginCity)
                    .ThenInclude(city => city.WorldPlayer)
                        .ThenInclude(player => player!.Alliance)
                .Include(ud => ud.TargetCity)
                    .ThenInclude(city => city!.WorldPlayer)
                        .ThenInclude(player => player!.PlayerProfile)
                .Include(ud => ud.TargetCity)
                    .ThenInclude(city => city!.WorldPlayer)
                        .ThenInclude(player => player!.Alliance)
                .Include(ud => ud.UnitStacks)
                .Include(ud => ud.OwnerWorldPlayer)
                    .ThenInclude(ud => ud!.PlayerProfile)
                .Where(ud => ud.WorldPlayerId == worldPlayerId)
                .OrderBy(ud => ud.Phase == UnitDeploymentPhaseEnum.Stationed ? 1 : 0)
                .ThenBy(ud => ud.Phase == UnitDeploymentPhaseEnum.Stationed ? DateTime.MaxValue : ud.ArrivalTime)
                .ThenByDescending(ud => ud.Phase == UnitDeploymentPhaseEnum.Stationed ? ud.StationedAt : null)
                .ThenBy(ud => ud.DepartureTime)
                .ThenBy(ud => ud.DateCreated)
                .ThenBy(ud => ud.Id)
                .ToListAsync();
        }

        public async Task<List<UnitDeployment>> GetIncomingAttacksByTargetOwnerIdAsync(Guid worldPlayerId)
        {
            return await _context.UnitDeployments
                .AsNoTracking()
                .Include(deployment => deployment.TargetCity)
                .Include(deployment => deployment.OwnerWorldPlayer)
                    .ThenInclude(player => player!.PlayerProfile)
                .Where(deployment => deployment.Type == UnitDeploymentTypeEnum.Attack
                    && deployment.Phase == UnitDeploymentPhaseEnum.Outbound
                    && deployment.TargetCity != null
                    && deployment.TargetCity.WorldPlayerId == worldPlayerId)
                .OrderBy(deployment => deployment.ArrivalTime)
                .ThenBy(deployment => deployment.DepartureTime)
                .ThenBy(deployment => deployment.DateCreated)
                .ThenBy(deployment => deployment.Id)
                .ToListAsync();
        }

        public async Task<List<UnitDeployment>> GetStationedSupportByTargetCityIdAsync(Guid targetCityId)
        {
            return await _context.UnitDeployments
                .Include(deployment => deployment.UnitStacks)
                .Include(deployment => deployment.OwnerWorldPlayer)
                .Include(deployment => deployment.OriginCity)
                .Where(deployment => deployment.TargetCityId == targetCityId
                    && deployment.Type == UnitDeploymentTypeEnum.Support
                    && deployment.Phase == UnitDeploymentPhaseEnum.Stationed)
                .OrderBy(deployment => deployment.WorldPlayerId)
                .ThenBy(deployment => deployment.Id)
                .ToListAsync();
        }

        public async Task<List<UnitDeployment>> GetStationedSupportAsync(int batchSize)
        {
            return await _context.UnitDeployments
                .Include(deployment => deployment.UnitStacks)
                .Include(deployment => deployment.OwnerWorldPlayer)
                .Include(deployment => deployment.OriginCity)
                .Include(deployment => deployment.TargetCity)!.ThenInclude(city => city!.WorldPlayer)
                .Where(deployment => deployment.Type == UnitDeploymentTypeEnum.Support
                    && deployment.Phase == UnitDeploymentPhaseEnum.Stationed)
                .OrderBy(deployment => deployment.Id)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task AddAsync(UnitDeployment deployment)
        {
            await _context.UnitDeployments.AddAsync(deployment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UnitDeployment deployment)
        {
            _context.UnitDeployments.Update(deployment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(UnitDeployment deployment)
        {
            _context.UnitDeployments.Remove(deployment);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UnitDeployment>> GetDueMovementsAsync(DateTime now, int batchSize)
        {
            return await _context.UnitDeployments
                .AsSplitQuery()
                .Include(ud => ud.OriginCity)
                    .ThenInclude(city => city.ActiveFocuses)
                .Include(ud => ud.OriginCity)
                    .ThenInclude(city => city.WorldPlayer)
                        .ThenInclude(player => player!.ModifiersInternal)
                .Include(ud => ud.OriginCity)
                    .ThenInclude(city => city.WorldPlayer)
                        .ThenInclude(player => player!.CompletedResearches)
                .Include(ud => ud.TargetCity)
                    .ThenInclude(city => city!.UnitStacks)
                .Include(ud => ud.TargetCity)
                    .ThenInclude(city => city!.ActiveFocuses)
                .Include(ud => ud.TargetCity)
                    .ThenInclude(city => city!.WorldPlayer)
                        .ThenInclude(player => player!.ModifiersInternal)
                .Include(ud => ud.TargetCity)
                    .ThenInclude(city => city!.WorldPlayer)
                        .ThenInclude(player => player!.CompletedResearches)
                .Include(ud => ud.UnitStacks)
                .Include(ud => ud.OwnerWorldPlayer)
                    .ThenInclude(player => player!.ModifiersInternal)
                .Include(ud => ud.OwnerWorldPlayer)
                    .ThenInclude(player => player!.CompletedResearches)
                .Where(ud => ud.Phase != UnitDeploymentPhaseEnum.Stationed
                    && ud.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Moving
                    && ud.ArrivalTime <= now)
                .OrderBy(ud => ud.ArrivalTime)
                .ThenBy(ud => ud.DepartureTime)
                .ThenBy(ud => ud.DateCreated)
                .ThenBy(ud => ud.Id)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task<UnitDeployment?> GetByIdAsync(Guid id)
        {
            return await _context.UnitDeployments
                .Include(ud => ud.OriginCity)
                .Include(ud => ud.TargetCity)
                .Include(ud => ud.UnitStacks)
                .Include(ud => ud.OwnerWorldPlayer)
                    .ThenInclude(ud => ud!.PlayerProfile)
                .Where(ud => ud.Id == id).FirstOrDefaultAsync();
        }

    }
}
