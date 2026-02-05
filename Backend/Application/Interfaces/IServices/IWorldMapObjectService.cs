using Domain.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IWorldMapObjectService
    {
        Task AddEntityToWorldMapAsync(IMapEntity entity);
        Task UpdateEntityPositionOnWorldMapAsync(IMapEntity entity, int oldX, int oldY);
        Task RemoveEntityFromWorldMapAsync(IMapEntity entity);
    }
}
