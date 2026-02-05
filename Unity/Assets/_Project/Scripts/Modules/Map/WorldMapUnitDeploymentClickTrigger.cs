using UnityEngine;
using System;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapUnitClickTrigger : MonoBehaviour
    {
        public Guid DeploymentId { get; private set; }
        public Vector2Int CurrentCoordinates { get; private set; }

        public void InitializeTrigger(Guid deploymentId, int x, int y)
        {
            DeploymentId = deploymentId;
            CurrentCoordinates = new Vector2Int(x, y);

            // Debug log så vi kan se i konsollen at ID'et er landet i triggeren
            if (deploymentId == Guid.Empty)
            {
                Debug.LogError($"<color=red>[UnitClickTrigger]</color> FEJL: Forsøger at initialisere med tomt ID på {x},{y}!");
            }
            else
            {
                Debug.Log($"<color=orange>[UnitClickTrigger]</color> Initialiseret: ID={deploymentId} på {x},{y}");
            }
        }
    }
}