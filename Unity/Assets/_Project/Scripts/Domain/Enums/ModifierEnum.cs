using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Domain.Enums
{
    public enum ModifierTypeEnum
    {
        Flat,
        Increased,
        Decreased
    }

    public enum ModifierTagEnum
    {
        Wood, Stone, Metal, Silver,
        ResourceProduction,
        Recruitment, Construction, Research, Population, Storage,

        Infantry, Cavalry, Siege,
        Upkeep, Speed, Power, Wall
    }
}
