using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Models;
using Project.Modules.City;
using UnityEngine.Tilemaps;
using Project.Scripts.Domain.DTOs;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapEntityManager : MonoBehaviour
    {
        public static WorldMapEntityManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject _cityPrefab;
        [SerializeField] private GameObject _unitDeploymentPrefab;
        [SerializeField] private Transform _objectContainer;

        [Header("Indstillinger")]
        [SerializeField] private int _cityLabelSortingOrder = 10;
        [SerializeField] private int _unitDeploymentLabelSortingOrder = 20;
        [SerializeField] private string _unitLayerName = "Units";

        public Tilemap TerrainTilemap;
        private Dictionary<Guid, GameObject> _activeUnitVisuals = new();
        private Dictionary<Vector2Int, List<GameObject>> _activeMapObjectsPerChunk = new();

        private void Awake() => Instance = this;

        private void Start()
        {
            if (WorldMapStateManager.Instance != null)
                WorldMapStateManager.Instance.OnChunkDataReady += HandleEntitySynchronizationRequest;

            if (WorldMapInteractionHandler.Instance != null)
                WorldMapInteractionHandler.Instance.OnSelectionChanged += SyncUnitSelectionVisuals;

            Debug.Log("<color=green>[WorldMapEntityManager]</color> Start: System klar til synkronisering.");
        }

        private void OnDestroy()
        {
            if (WorldMapStateManager.Instance != null)
                WorldMapStateManager.Instance.OnChunkDataReady -= HandleEntitySynchronizationRequest;

            if (WorldMapInteractionHandler.Instance != null)
                WorldMapInteractionHandler.Instance.OnSelectionChanged -= SyncUnitSelectionVisuals;
        }

        private void HandleEntitySynchronizationRequest(WorldMapChunkResponseDTO data)
        {
            Vector2Int key = new Vector2Int(data.ChunkX, data.ChunkY);
            Debug.Log($"<color=green>[WorldMapEntityManager]</color> Synkroniserer visuelle objekter for chunk {key}.");

            if (_activeMapObjectsPerChunk.TryGetValue(key, out List<GameObject> existingObjects))
            {
                Debug.Log($"<color=yellow>[WorldMapEntityManager]</color> Rydder {existingObjects.Count} visuelle objekter i chunk {key} for at genopbygge.");
                foreach (var obj in existingObjects)
                {
                    if (obj == null) continue;

                    var movementController = obj.GetComponent<WorldMapUnitVisualMovementController>();
                    if (movementController != null)
                    {
                        Guid id = movementController.GetDeploymentId();
                        if (id != Guid.Empty)
                        {
                            Debug.Log($"<color=yellow>[WorldMapEntityManager]</color> Fjerner hær-visual {id} fra tracking ordbog.");
                            _activeUnitVisuals.Remove(id);
                        }
                    }
                    Destroy(obj);
                }
                existingObjects.Clear();
            }
            else
            {
                _activeMapObjectsPerChunk[key] = new List<GameObject>();
            }

            // 1. Spawn Byer
            foreach (var city in data.Cities)
            {
                Vector3 worldPos = TerrainTilemap.GetCellCenterWorld(new Vector3Int(city.X, city.Y, 0));
                GameObject inst = Instantiate(_cityPrefab, worldPos, Quaternion.identity, _objectContainer);
                inst.name = $"City_{city.CityName}";

                var uiDoc = inst.GetComponent<UIDocument>();
                if (uiDoc != null) uiDoc.sortingOrder = _cityLabelSortingOrder;

                inst.GetComponent<WorldMapCityInteractionLabelController>()?.InitializeCityInteractionLabel(city.CityName, city.Points);
                _activeMapObjectsPerChunk[key].Add(inst);
            }

            // 2. Håndter Units
            if (data.UnitDeployments != null)
            {
                Debug.Log($"<color=green>[WorldMapEntityManager]</color> Behandler {data.UnitDeployments.Count} hære i chunk {key}.");
                foreach (var unit in data.UnitDeployments)
                {
                    GameObject unitVisual = SynchronizeUnitDeploymentVisual(unit);
                    if (unitVisual != null)
                    {
                        _activeMapObjectsPerChunk[key].Add(unitVisual);
                    }
                }
            }
        }

        private GameObject SynchronizeUnitDeploymentVisual(UnitDeploymentDTO data)
        {
            GameObject unitObj;
            if (_activeUnitVisuals.TryGetValue(data.Id, out GameObject existing) && existing != null)
            {
                Debug.Log($"<color=cyan>[WorldMapEntityManager]</color> GENBRUGER visual for hær {data.Id}. Opdaterer position.");
                unitObj = existing;
                unitObj.GetComponent<WorldMapUnitVisualMovementController>().InitializeMovement(data, TerrainTilemap);
            }
            else
            {
                Debug.Log($"<color=green>[WorldMapEntityManager]</color> SPANWER NY visual for hær {data.Id}.");
                unitObj = Instantiate(_unitDeploymentPrefab, Vector3.zero, Quaternion.identity, _objectContainer);
                unitObj.name = $"UnitDeployment_{data.Id}";

                int layer = LayerMask.NameToLayer(_unitLayerName);
                if (layer != -1) unitObj.layer = layer;

                var uiDoc = unitObj.GetComponent<UIDocument>();
                if (uiDoc != null) uiDoc.sortingOrder = _unitDeploymentLabelSortingOrder;

                var moveCtrl = unitObj.GetComponent<WorldMapUnitVisualMovementController>() ?? unitObj.AddComponent<WorldMapUnitVisualMovementController>();
                moveCtrl.InitializeMovement(data, TerrainTilemap);
                _activeUnitVisuals[data.Id] = unitObj;
            }

            var trigger = unitObj.GetComponent<WorldMapUnitClickTrigger>();
            if (trigger != null) trigger.InitializeTrigger(data.Id, data.CurrentX, data.CurrentY);

            int qty = data.UnitStacks?.Sum(s => s.Quantity) ?? 0;
            unitObj.GetComponent<WorldMapUnitDeploymentLabelController>()?.InitializeUnitDeploymentLabel(data.Name, qty);
            UpdateUnitVisualScale(data.Id, unitObj);

            return unitObj;
        }

        private void SyncUnitSelectionVisuals(Guid? selectedId)
        {
            foreach (var kvp in _activeUnitVisuals) UpdateUnitVisualScale(kvp.Key, kvp.Value);
        }

        private void UpdateUnitVisualScale(Guid id, GameObject obj)
        {
            if (obj == null) return;
            bool isSelected = WorldMapInteractionHandler.Instance.SelectedDeploymentId == id;
            obj.transform.localScale = isSelected ? new Vector3(1.3f, 1.3f, 1f) : Vector3.one;
        }

        public void RemoveUnitVisualExplicitly(Guid id)
        {
            if (_activeUnitVisuals.TryGetValue(id, out GameObject obj))
            {
                Debug.Log($"<color=red>[WorldMapEntityManager]</color> EKSPLICIT SLETNING af visual for hær {id}.");
                _activeUnitVisuals.Remove(id);
                if (obj != null) Destroy(obj);
            }
            else
            {
                Debug.LogWarning($"<color=orange>[WorldMapEntityManager]</color> Forsøgte eksplicit sletning af {id}, men visual findes ikke.");
            }
        }
    }
}