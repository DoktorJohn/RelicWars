using UnityEngine;
using System.Collections.Generic;

namespace Sunvale.AncientRomeUI.Demos
{
    [CreateAssetMenu(fileName = "DemoSpriteCollection", menuName = "Ancient Rome UI/Demo Sprite Collection")]
    public class DemoSpriteCollection : ScriptableObject
    {
        [Header("RPG Demo")]
        public List<Sprite> rpgPortraits;

        [Header("Strategy Ledger Demo")]
        public List<Sprite> strategyLedgerPortraits;

        [Header("Strategy Top Down Demo")]
        public List<Sprite> strategyTopDownPortraits;
        public List<Sprite> strategyTopDownUnits;
    }
}
