using System;
using System.Collections.Generic;

[Serializable]
public class WorldAvailableResponseDTO
{
    public string WorldId;
    public string WorldName;
    public int CurrentPlayerCount;
    public int MaxPlayerCapacity;
    public bool IsCurrentPlayerMember;
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