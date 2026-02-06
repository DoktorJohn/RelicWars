using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<UnitDeployment>> GetActiveDeploymentsAsync()
        {
            return await _context.UnitDeployments
                .Include(ud => ud.OriginCity)
                .Include(ud => ud.TargetCity)
                .Include(ud => ud.UnitStacks)
                .Include(ud => ud.OwnerWorldPlayer)
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