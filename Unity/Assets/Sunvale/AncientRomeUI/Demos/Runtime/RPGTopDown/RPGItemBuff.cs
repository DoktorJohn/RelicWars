using System;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    [Serializable]
    public enum RPGStatModifierType
    {
        flat,
        percent
    }

    [Serializable]
    public class RPGItemBuff
    {
        public RPGStatType statType;
        public RPGStatModifierType modifierType;
        public float value;

        public RPGItemBuff()
        {
                    
        }

        public RPGItemBuff(RPGStatType statType, RPGStatModifierType modifierType, float value)
        {
            this.statType = statType;
            this.modifierType = modifierType;
            this.value = value;
        }
    }
}
