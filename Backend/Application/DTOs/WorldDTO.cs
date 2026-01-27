using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record WorldDTO(
    Guid Id,
    string Name,
    string Abbreviation,
    int XAxis,
    int YAxis
);

    public record WorldAvailableResponseDTO(
        Guid WorldId,
        string WorldName,
        int CurrentPlayerCount,
        int MaxPlayerCapacity,
        bool IsCurrentPlayerMember);

}

public class WorldMapObjectDTO
{
    public short X { get; set; }
    public short Y { get; set; }
    public byte Type { get; set; }
    public Guid? ReferenceEntityId { get; set; }
}

public class WorldMapChunkResponseDTO
{
    public int WorldSeed { get; set; }
    public int ChunkX { get; set; }
    public int ChunkY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public byte[] TerrainData { get; set; }

    public List<WorldMapObjectDTO> MapObjects { get; set; }
}

public class GetWorldMapChunkDTO
{
    public Guid worldId { get; set; }
    public short startX { get; set; }
    public short startY { get; set; }
    public byte width { get; set; } = 50;
    public byte height { get; set; } = 50;
}