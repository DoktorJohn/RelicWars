using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using Project.Scripts.Domain.Enums;

[CreateAssetMenu(menuName = "World/Biome Visuals")]
public class BiomeVisuals : ScriptableObject
{
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

    [ContextMenu("Clear Cache")]
    public void ClearCache() => _tileCache = null;
}