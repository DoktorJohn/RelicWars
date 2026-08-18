using Application.DTOs;
using Domain.Enums;

namespace Application.Interfaces.IServices
{
    public sealed record DailyObjectiveProgressEvent(
        DailyObjectiveProgressTypeEnum ProgressType,
        double Amount,
        DateTime OccurredAtUtc,
        UnitTypeEnum? UnitType = null,
        UnitCategoryEnum? UnitCategory = null,
        bool IsElite = false);

    public interface IDailyObjectiveService
    {
        Task<DailyObjectivesDTO> GetAsync(Guid worldPlayerId);
        Task<DailyObjectivesDTO> CollectAsync(Guid worldPlayerId, int definitionId, Guid cityId);
        Task ApplyProgressAsync(Guid worldPlayerId, DailyObjectiveProgressEvent progressEvent);
        Task ApplyProductionAsync(Guid worldPlayerId, DateTime intervalStartUtc, DateTime intervalEndUtc,
            double coinsPerHour = 0, double exoticResourcesPerHour = 0);
    }
}
