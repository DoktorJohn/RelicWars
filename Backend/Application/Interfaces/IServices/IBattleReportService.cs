using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IBattleReportService
    {
        Task<List<BattleReportDTO>> GetBattleReportsAsync(Guid worldPlayerId);
        Task<BattleReportUnreadStatusDTO> GetUnreadStatusAsync(Guid worldPlayerId);
        Task MarkBattleReportAsReadAsync(Guid worldPlayerId, Guid battleReportId);
        Task DeleteBattleReportAsync(Guid worldPlayerId, Guid battleReportId);
    }
}
