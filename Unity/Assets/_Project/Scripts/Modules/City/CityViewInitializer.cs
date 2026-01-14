using UnityEngine;
using System;
using Project.Modules.City;
using Project.Network.Manager; // Sørg for at CityResourceService ligger i dette namespace

namespace Project.Modules.CityView
{
    public class CityViewInitializer : MonoBehaviour
    {
        private void Start()
        {
            ExecuteTraceableInitialization();
        }

        private void ExecuteTraceableInitialization()
        {
            Debug.Log("[DEBUG-INIT] CityViewInitializer startet.");

            // 1. Validering af NetworkManager i stedet for ApiService
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[DEBUG-INIT] FEJL: NetworkManager.Instance er NULL. Sørg for at starte fra Bootstrap/Login-scenen.");
                return;
            }

            // 2. Hent ID fra den nye property 'ActiveCityId'
            Guid? activeCityId = NetworkManager.Instance.ActiveCityId;

            if (activeCityId.HasValue && activeCityId.Value != Guid.Empty)
            {
                Debug.Log($"[DEBUG-INIT] FUNDET: Aktivt CityId er {activeCityId.Value}. Sender anmodning til ResourceService.");

                // Vi antager at CityResourceService er din lokale UI-manager, der håndterer visningen.
                // Hvis denne klasse også fejler, skal den opdateres til at bruge NetworkManager.Instance.City.GetDetailedCityInfo(...)
                if (CityResourceService.Instance != null)
                {
                    CityResourceService.Instance.InitiateResourceRefresh(activeCityId.Value);
                }
                else
                {
                    Debug.LogError("[DEBUG-INIT] FEJL: CityResourceService.Instance findes ikke i scenen.");
                }
            }
            else
            {
                Debug.LogError("[DEBUG-INIT] KRITISK FEJL: NetworkManager har intet ActiveCityId. Login eller World Selection er fejlet.");
            }
        }
    }
}