using Domain.Entities;
using Domain.Enums;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IWorldMapObjectRepository 
    {
        Task AddAsync(WorldMapObject worldMapObject);
        Task<WorldMapObject?> GetWorldMapObjectByReferenceIdAsync(Guid referenceId);
        Task<List<WorldMapObject>> GetObjectsInAreaAsync(Guid worldId, short startX, short startY, byte width, byte height);
        Task DeleteAtCoordinatesAsync(Guid worldId, short x, short y);
        Task DeleteByReferenceIdAsync(Guid referenceEntityId);
        Task UpdateAsync(WorldMapObject worldMapObject);
        Task<WorldMapObject?> GetCityOnCoordinatesAsync(Guid worldId, short X, short Y);
        Task<List<WorldMapObject>> GetObjectsByTypeAsync(Guid id, MapObjectTypeEnum type);
    }
}
