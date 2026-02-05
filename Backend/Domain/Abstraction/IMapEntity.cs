using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Abstraction
{
    public interface IMapEntity
    {
        Guid Id { get; }
        int X { get; }
        int Y { get; }
        Guid WorldId { get; }
        MapObjectTypeEnum MapObjectType { get; }
    }
}
