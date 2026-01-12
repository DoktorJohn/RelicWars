using UnityEngine;
using System;
using Project.Modules.City;

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

            if (ApiService.Instance == null)
            {
                Debug.LogError("[DEBUG-INIT] FEJL: ApiService.Instance er NULL. Sørg for at starte fra Login-scenen.");
                return;
            }

            Guid? activeCityId = ApiService.Instance.CurrentlySelectedActiveCityId;

            if (activeCityId.HasValue && activeCityId.Value != Guid.Empty)
            {
                Debug.Log($"[DEBUG-INIT] FUNDET: Aktivt CityId er {activeCityId.Value}. Sender anmodning til ResourceService.");

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
                Debug.LogError("[DEBUG-INIT] KRITISK FEJL: ApiService har intet CurrentlySelectedActiveCityId. Login eller World Selection er fejlet.");
            }
        }
    }
}