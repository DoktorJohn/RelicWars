using System;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Manager;
using Project.Network.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiIslandWindowController : MonoBehaviour, IUguiWindowPayloadReceiver
    {
        [SerializeField] private Transform rowsContainer;
        [SerializeField] private UguiIslandPlayerDataRowView rowPrefab;

        private int _loadVersion;

        private void OnEnable()
        {
            ClearRows();
        }

        private void OnDisable()
        {
            _loadVersion++;
            StopAllCoroutines();
        }

        public void OnOpen(object payload)
        {
            int loadVersion = ++_loadVersion;
            ClearRows();

            if (payload is not Guid islandId || islandId == Guid.Empty)
            {
                Debug.LogError("[UguiIslandWindowController] Cannot load an island without a valid island id.", this);
                return;
            }

            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.World == null)
            {
                Debug.LogError("[UguiIslandWindowController] Network is unavailable.", this);
                return;
            }

            StartCoroutine(network.World.GetIslandDetails(islandId, network.JwtToken, details =>
            {
                if (!isActiveAndEnabled || loadVersion != _loadVersion)
                    return;

                if (details == null)
                {
                    Debug.LogError($"[UguiIslandWindowController] Island {islandId} could not be loaded.", this);
                    return;
                }

                Render(details.Cities);
            }));
        }

        private void Render(IEnumerable<WorldIslandCityDTO> cities)
        {
            ClearRows();
            if (rowsContainer == null || rowPrefab == null)
            {
                Debug.LogError("[UguiIslandWindowController] Rows container or IslandPlayerDataRow prefab is not assigned.", this);
                return;
            }

            foreach (WorldIslandCityDTO city in (cities ?? Array.Empty<WorldIslandCityDTO>())
                         .OrderByDescending(city => city.Points)
                         .ThenBy(city => city.CityName))
            {
                UguiIslandPlayerDataRowView row = Instantiate(rowPrefab, rowsContainer, false);
                row.gameObject.SetActive(true);
                row.Bind(city);
            }

            Canvas.ForceUpdateCanvases();
            if (rowsContainer is RectTransform rowsRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowsRect);
        }

        private void ClearRows()
        {
            if (rowsContainer == null)
                return;

            for (int index = rowsContainer.childCount - 1; index >= 0; index--)
            {
                Transform child = rowsContainer.GetChild(index);
                if (child.GetComponent<UguiIslandPlayerDataRowView>() != null)
                    Destroy(child.gameObject);
            }
        }
    }
}
