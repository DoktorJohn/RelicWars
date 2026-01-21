using Domain.Abstraction;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public struct Tile : IModifierProvider
    {
        public BiomeEnum TileBiome { get; set; }
        public byte Citycount { get; set; }
        public Modifier ModifierInternal { get; set; }

        public IEnumerable<Modifier> GetModifiers()
        {
            if (ModifierInternal == null)
            {
                yield break;
            }

            yield return ModifierInternal;
        }
    }
}
