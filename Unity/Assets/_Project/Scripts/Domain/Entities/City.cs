using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Assets.Scripts.Domain.Entities
{
    [Serializable]
    public class City
    {
        public Guid Id;
        public string Name;
        public int X;
        public int Y;

        // Ressourcer
        public double Wood;
        public double Stone;
        public double Metal;

        public List<Building> Buildings;

        public City()
        {
            Buildings = new List<Building>();
        }
    }
}