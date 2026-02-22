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
        Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid profileId, Guid worldId);
        Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId);
        Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId);
        Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query);
        void UpdateGlobalResourceState(WorldPlayer player, DateTime currentDateTime);
        Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request);
        Task<bool> ApplyAlphaCheatAsync(Guid worldPlayerId, Guid cityId);
    }
}
