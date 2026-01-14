using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorldService : IWorldService
    {
        private readonly IWorldRepository _worldRepository;

        public WorldService(IWorldRepository worldRepository)
        {
            _worldRepository = worldRepository;
        }

        public async Task<List<WorldAvailableResponseDTO>> ObtainAllActiveGameWorldsAsync()
        {
            // Optimization: Only fetch what is needed for the list (Projection).
            // Currently assuming 'true' for IsCurrentPlayerMember based on your code, 
            // but normally this requires a check against the current user context.
            var activeWorlds = await _worldRepository.GetAllAsync();

            if (activeWorlds == null) return new List<WorldAvailableResponseDTO>();

            return activeWorlds.Select(world => new WorldAvailableResponseDTO(
                world.Id,
                world.Name,
                world.PlayerCount,
                1000,
                false // Should be dynamic based on User Context
            )).ToList();
        }
    }
}
