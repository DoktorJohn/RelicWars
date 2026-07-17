using UnityEngine;
using System;
using Project.Modules.City;
using Project.Network.Manager; // Sørg for at CityStateManager ligger i dette namespace
using Project.Modules.WorldPlayer;

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
            string worldPlayerIdString = NetworkManager.Instance.WorldPlayerId;

            if (activeCityId.HasValue && activeCityId.Value != Guid.Empty)
            {
                Debug.Log($"[DEBUG-INIT] FUNDET: Aktivt CityId er {activeCityId.Value}. Sender anmodning til ResourceService.");

                // Vi antager at CityStateManager er din lokale UI-manager, der håndterer visningen.
                // Hvis denne klasse også fejler, skal den opdateres til at bruge NetworkManager.Instance.City.GetDetailedCityInfo(...)
                if (CityStateManager.Instance != null)
                {
                    CityStateManager.Instance.StartPollingForCity(activeCityId.Value);
                }
                else
                {
                    Debug.LogError("[DEBUG-INIT] FEJL: CityStateManager.Instance findes ikke i scenen.");
                }

                if (!string.IsNullOrEmpty(worldPlayerIdString) && Guid.TryParse(worldPlayerIdString, out Guid worldPlayerId))
                {
                    if (WorldPlayerStateManager.Instance != null)
                    {
                        WorldPlayerStateManager.Instance.InitiateEconomyRefresh(worldPlayerId);
                    }
                    else
                    {
                        Debug.LogError("[DEBUG-INIT] FEJL: WorldPlayerStateManager.Instance findes ikke i scenen.");
                    }
                }
                else
                {
                    Debug.LogError("[DEBUG-INIT] FEJL: WorldPlayerId er ugyldig eller mangler i NetworkManager.");
                }
            }
            else
            {
                Debug.LogError("[DEBUG-INIT] KRITISK FEJL: NetworkManager har intet ActiveCityId. Login eller World Selection er fejlet.");
            }
        }
    }
}
