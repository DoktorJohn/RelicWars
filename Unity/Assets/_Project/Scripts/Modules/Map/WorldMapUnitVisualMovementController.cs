using UnityEngine;
using System;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapUnitVisualMovementController : MonoBehaviour
    {
        private UnitDeploymentDTO _unitDeploymentData;
        private UnityEngine.Tilemaps.Tilemap _worldTilemap;
        private bool _isInitialized = false;

        public void InitializeMovement(UnitDeploymentDTO data, UnityEngine.Tilemaps.Tilemap tilemap)
        {
            _worldTilemap = tilemap;
            _unitDeploymentData = data;
            _isInitialized = true;

            Debug.Log($"<color=cyan>[VisualMovement]</color> Initialiseret for hær: {data.Id} ved origin city {data.OriginCityId}");
            UpdateVisualPositionToOriginCity();
        }

        private void Update()
        {
            if (!_isInitialized || _unitDeploymentData == null || _worldTilemap == null)
            {
                return;
            }

            UpdateVisualPositionToOriginCity();
        }

        private void UpdateVisualPositionToOriginCity()
        {
            if (_unitDeploymentData?.OriginCity == null)
            {
                return;
            }

            Vector3 worldPosition = _worldTilemap.GetCellCenterWorld(new Vector3Int(
                _unitDeploymentData.OriginCity.X,
                _unitDeploymentData.OriginCity.Y,
                0
            ));

            worldPosition.z = -0.1f;
            transform.position = worldPosition;
        }

        public Guid GetDeploymentId()
        {
            if (_unitDeploymentData == null)
            {
                Debug.LogWarning($"<color=red>[VisualMovement]</color> GetDeploymentId kaldt på uinitialiseret controller!");
                return Guid.Empty;
            }
            return _unitDeploymentData.Id;
        }
    }
}
