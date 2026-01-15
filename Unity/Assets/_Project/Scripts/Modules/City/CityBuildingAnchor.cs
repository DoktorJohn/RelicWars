using UnityEngine;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.City
{
    /// <summary>
    /// Placeres på et tomt GameObject i scenen for at markere, hvor en specifik bygning skal spawne.
    /// </summary>
    public class CityBuildingAnchor : MonoBehaviour
    {
        [Header("Anchor Indstillinger")]
        public BuildingTypeEnum BuildingType;

        // Visualisering i Editoren så du kan se dine anchors
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1, 1, 1));

            // Lille hjælpelinje til at se retningen (Z-aksen / Forward)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.0f);
        }
    }
}