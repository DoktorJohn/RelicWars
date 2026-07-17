using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;

namespace Application.Interfaces.IServices;

public interface IUnitUnlockCatalog
{
    ResearchData? GetUnitUnlock(UnitTypeEnum unitType);
    bool HasSubjugationUnlock(WorldPlayer worldPlayer);
}
