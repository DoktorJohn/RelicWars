using UnityEngine;
using System;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapUnitClickTrigger : MonoBehaviour
    {
        public Guid DeploymentId { get; private set; }
        public Vector2Int CurrentCoordinates { get; private set; }

        public void InitializeTrigger(Guid id, int x, int y)
        {
            DeploymentId = id;
            CurrentCoordinates = new Vector2Int(x, y);

            if (id == Guid.Empty)
            {
                Debug.LogError($"<color=red>[UnitClickTrigger]</color> Initialiseret med tomt ID på {x},{y}!");
            }
            else
            {
                Debug.Log($"<color=orange>[UnitClickTrigger]</color> Klar til klik: {id} på {x},{y}");
            }
        }
    }
}