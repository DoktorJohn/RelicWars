using UnityEngine;
using System;
using Assets.Scripts.Domain.Enums;

namespace Assets.Scripts.Domain.Entities
{
    [Serializable]
    public class Building
    {
        public BuildingTypeEnum Type;
        public int Level;

        public Building(BuildingTypeEnum type, int level)
        {
            Type = type;
            Level = level;
        }
    }
}