using Application.DTOs;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IWorldPlayerService
    {
        Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid worldId);
        Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId);
        Task<WorldPlayerProfileDTO> UpdateWorldPlayerDescriptionAsync(Guid worldPlayerId, string description);
        Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId);
        Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query);
        void SyncGlobalResources(WorldPlayer player, DateTime currentDateTime);
        Task SyncGlobalResourcesAsync(WorldPlayer player, DateTime currentDateTime)
        {
            SyncGlobalResources(player, currentDateTime);
            return Task.CompletedTask;
        }
        Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request);
    }
}
