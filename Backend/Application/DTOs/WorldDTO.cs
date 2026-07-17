using Application.DTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;

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

public record WorldMapCoordinateDTO(int X, int Y);

public record WorldIslandMapDTO(Guid Id, int CenterX, int CenterY);

public record WorldIslandDetailsDTO(
    Guid Id,
    int CenterX,
    int CenterY,
    bool HasOwnedCity,
    List<WorldIslandCityDTO> Cities,
    List<WorldIslandExoticResourceDTO> ExoticResources);

public record WorldIslandCityDTO(
    Guid Id,
    string CityName,
    Guid? WorldPlayerId,
    string? WorldPlayerName,
    int X,
    int Y,
    int Points,
    Guid? AllianceId,
    string? AllianceName,
    bool IsNPC = false);

public record WorldIslandExoticResourceDTO(
    int SlotIndex,
    ExoticResourceTypeEnum ResourceType,
    int Tier,
    double ProgressPercent,
    double OutputPerHour,
    double WoodInvestment,
    double StoneInvestment,
    double MetalInvestment,
    double CoinInvestment,
    double NextTierWoodCost,
    double NextTierStoneCost,
    double NextTierMetalCost,
    double NextTierCoinCost);

public class GetWorldMapChunkDTO
{
    public Guid worldId { get; set; }
    public short startX { get; set; }
    public short startY { get; set; }
    public byte width { get; set; } = 50;
    public byte height { get; set; } = 50;
}
