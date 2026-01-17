using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IAllianceService
    {
        Task<AllianceDTO> GetAllianceInfo(Guid allianceId);
        Task<AllianceDTO> CreateAlliance(CreateAllianceDTO dto);
        Task<bool> DisbandAlliance(DisbandAllianceDTO dto);
        Task<bool> InviteToAlliance(InviteToAllianceDTO dto);
        Task<bool> KickPlayer(KickPlayerFromAllianceDTO dto);
    }
}
