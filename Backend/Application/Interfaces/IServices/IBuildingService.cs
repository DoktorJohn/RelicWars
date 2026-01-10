using Application.DTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IBuildingService
    {
        Task<BuildingResult> QueueUpgradeAsync(Guid cityId, BuildingTypeEnum type);
    }
}
