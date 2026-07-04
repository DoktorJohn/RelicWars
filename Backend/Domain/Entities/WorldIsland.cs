using Domain.Abstraction;
using Domain.Enums;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class WorldIsland : BaseEntity
    {
        public Guid WorldId { get; set; }
        public World? World { get; set; }
        public int CellX { get; set; }
        public int CellY { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public IslandShapeEnum Shape { get; set; }
        public float MajorRadius { get; set; }
        public float MinorRadius { get; set; }
        public float RotationDegrees { get; set; }
        public float EdgeRoughness { get; set; }
        public List<WorldIslandExoticResource> ExoticResources { get; set; } = new();
    }
}
