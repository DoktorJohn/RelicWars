using Domain.Abstraction;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class WorldMapObject
    {
        public Guid WorldId { get; set; }
        public short X { get; set; }
        public short Y { get; set; }

        public MapObjectTypeEnum Type { get; set; }
        public Guid? ReferenceEntityId { get; set; }

        public World World { get; set; }

    }
}
