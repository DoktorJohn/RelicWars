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
        void UpdateGlobalResourceState(WorldPlayer player, DateTime currentDateTime);
        Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request);
    }
}
