using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using Project.Scripts.Domain.Enums;

[CreateAssetMenu(menuName = "World/Biome Visuals")]
public class BiomeVisuals : ScriptableObject
{
    [Header("Special Tiles")]
    public TileBase CityTier0To20Tile;
    public TileBase CityTier21To40Tile;
    public TileBase CityTier41To60Tile;
    public TileBase CityTier61To80Tile;
    public TileBase CityTier81To100Tile;
    public TileBase FutureCitySiteTile;
    public TileBase NPCVillageTile;

    [System.Serializable]
    public class BiomeBinding
    {
        public WorldBiomeVariantType Type;
        public TileBase TileAsset;
    }

    public List<BiomeBinding> Biomes = new List<BiomeBinding>();

    private Dictionary<WorldBiomeVariantType, TileBase> _tileCache;

    public TileBase GetTile(WorldBiomeVariantType type)
    {
        if (_tileCache == null)
        {
            _tileCache = Biomes.ToDictionary(b => b.Type, b => b.TileAsset);
        }

        if (_tileCache.TryGetValue(type, out TileBase tile))
        {
            return tile;
        }

        Debug.LogWarning($"[BiomeVisuals] Mangler sprite for: {type}");
        return null;
    }

    public TileBase GetCityTile(int points, int maximumPoints)
    {
        if (maximumPoints <= 0)
        {
            Debug.LogWarning("[BiomeVisuals] Maximum city points must be greater than zero.");
            return CityTier0To20Tile;
        }

        long percentageValue = (long)Mathf.Max(0, points) * 100;
        if (percentageValue <= (long)maximumPoints * 20) return CityTier0To20Tile;
        if (percentageValue <= (long)maximumPoints * 40) return CityTier21To40Tile;
        if (percentageValue <= (long)maximumPoints * 60) return CityTier41To60Tile;
        if (percentageValue <= (long)maximumPoints * 80) return CityTier61To80Tile;
        return CityTier81To100Tile;
    }

    [ContextMenu("Clear Cache")]
    public void ClearCache() => _tileCache = null;
}
