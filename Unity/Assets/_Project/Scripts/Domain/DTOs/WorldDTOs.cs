using Assets.Scripts.Domain.Enums;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;

[Serializable]
public class WorldAvailableResponseDTO
{
    public string WorldId;
    public string WorldName;
    public int CurrentPlayerCount;
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
    public int WorldWidth { get; set; }
    public int WorldHeight { get; set; }
    public int ChunkX { get; set; }
    public int ChunkY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int MaximumCityPoints { get; set; }
    public List<WorldMapObjectDTO> MapObjects { get; set; } = new();
    public List<CityDTO> Cities { get; set; } = new();
    public List<WorldMapCoordinateDTO> FutureCitySites { get; set; } = new();
    public List<WorldIslandMapDTO> Islands { get; set; } = new();
}

public class WorldMapCoordinateDTO
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class WorldIslandMapDTO
{
    public Guid Id { get; set; }
    public int CenterX { get; set; }
    public int CenterY { get; set; }
}

public class WorldIslandDetailsDTO
{
    public Guid Id { get; set; }
    public int CenterX { get; set; }
    public int CenterY { get; set; }
    public bool HasOwnedCity { get; set; }
    public List<WorldIslandCityDTO> Cities { get; set; } = new();
    public List<WorldIslandResourceDTO> ExoticResources { get; set; } = new();
}

public class WorldIslandCityDTO
{
    public Guid Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public Guid? WorldPlayerId { get; set; }
    public string WorldPlayerName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Points { get; set; }
    public Guid? AllianceId { get; set; }
    public string AllianceName { get; set; } = string.Empty;
    public bool IsNPC { get; set; }
}

public class WorldIslandResourceDTO
{
    public int SlotIndex { get; set; }
    public ExoticResourceTypeEnum ResourceType { get; set; }
    public int Tier { get; set; }
    public double ProgressPercent { get; set; }
    public double OutputPerHour { get; set; }
    public double WoodInvestment { get; set; }
    public double StoneInvestment { get; set; }
    public double MetalInvestment { get; set; }
    public double CoinInvestment { get; set; }
    public double NextTierWoodCost { get; set; }
    public double NextTierStoneCost { get; set; }
    public double NextTierMetalCost { get; set; }
    public double NextTierCoinCost { get; set; }
}

public class GetWorldMapChunkDTO
{
    public Guid worldId { get; set; }
    public short startX { get; set; }
    public short startY { get; set; }
    public byte width { get; set; } = 50;
    public byte height { get; set; } = 50;
}
