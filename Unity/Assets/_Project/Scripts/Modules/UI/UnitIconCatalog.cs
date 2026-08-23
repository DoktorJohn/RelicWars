using Assets.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Modules.UI
{
    [CreateAssetMenu(fileName = "UnitIconCatalog", menuName = "Relic Wars/UI/Unit Icon Catalog")]
    public sealed class UnitIconCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public UnitTypeEnum UnitType;
            public Sprite Sprite;
        }

        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private List<Entry> entries = new();

        private Dictionary<UnitTypeEnum, Sprite> _lookup;

        public Sprite FallbackSprite => fallbackSprite;

        public bool TryGetSprite(UnitTypeEnum unitType, out Sprite sprite)
        {
            EnsureLookup();
            return _lookup.TryGetValue(unitType, out sprite) && sprite != null;
        }

        private void OnEnable() => _lookup = null;

#if UNITY_EDITOR
        private void OnValidate() => _lookup = null;
#endif

        private void EnsureLookup()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<UnitTypeEnum, Sprite>();
            foreach (Entry entry in entries)
            {
                if (entry.UnitType != UnitTypeEnum.None && entry.Sprite != null)
                    _lookup[entry.UnitType] = entry.Sprite;
            }
        }
    }
}
