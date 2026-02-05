using Newtonsoft.Json;
using Project.Modules.City;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapUnitVisualMovementController : MonoBehaviour
    {
        private UnitDeploymentDTO _data;
        private Vector3 _startWorldPosition;
        private Vector3 _targetWorldPosition;
        private bool _isInitialized = false;
        private UnityEngine.Tilemaps.Tilemap _tilemap;
        private DateTime _lastProcessedStepTime = DateTime.MinValue;

        public Guid DeploymentId => _data?.Id ?? Guid.Empty;
        private List<HexCoordinateDTO> _remainingPath = new List<HexCoordinateDTO>();

        public void InitializeMovement(UnitDeploymentDTO newData, UnityEngine.Tilemaps.Tilemap tilemap)
        {
            _tilemap = tilemap;

            if (_isInitialized && _data != null)
            {
                if (newData.LastStepTime == _data.LastStepTime && newData.Status == UnitDeploymentMovementStatusEnum.Moving)
                {
                    // Opdater kun non-positional data
                    _data.UnitStacks = newData.UnitStacks;
                    _data.WorldPlayerUserName = newData.WorldPlayerUserName;
                    _data.ArrivalTime = newData.ArrivalTime;
                    return;
                }
            }

            _data = newData;
            SetupCurrentStep();
            _isInitialized = true;
        }

        private void SetupCurrentStep()
        {
            if (_tilemap == null || _data == null) return;

            if (_data.Status == UnitDeploymentMovementStatusEnum.Stationed)
            {
                var pos = _tilemap.GetCellCenterWorld(new Vector3Int(_data.CurrentX, _data.CurrentY, 0));
                pos.z = -0.1f;
                _startWorldPosition = pos;
                _targetWorldPosition = pos;
                transform.position = pos;
                return;
            }

            _startWorldPosition = _tilemap.GetCellCenterWorld(new Vector3Int(_data.CurrentX, _data.CurrentY, 0));
            _targetWorldPosition = _tilemap.GetCellCenterWorld(new Vector3Int(_data.NextX, _data.NextY, 0));
            _startWorldPosition.z = -0.1f;
            _targetWorldPosition.z = -0.1f;
        }

        private void Update()
        {
            if (!_isInitialized || _data == null) return;

            if (_data.Status == UnitDeploymentMovementStatusEnum.Moving)
            {
                UpdateVisualPosition();

                if (DateTime.UtcNow >= _data.NextStepTime)
                {
                    WorldMapStateManager.Instance.RequestWorldMapChunkData(
                        (short)_data.CurrentX, (short)_data.CurrentY, 50, 50, true);
                }
            }
            else
            {
                var targetPos = _tilemap.GetCellCenterWorld(new Vector3Int(_data.CurrentX, _data.CurrentY, 0));
                targetPos.z = -0.1f;
                transform.position = targetPos;
            }
        }

        private void UpdateVisualPosition()
        {
            long startTicks = _data.LastStepTime.Ticks;
            long endTicks = _data.NextStepTime.Ticks;
            long currentTicks = DateTime.UtcNow.Ticks;

            // Hvis server-tiden er i fremtiden (klokkeskæv), clamp til 0
            if (currentTicks < startTicks)
            {
                transform.position = _startWorldPosition;
                return;
            }

            float t = (endTicks > startTicks)
                ? Mathf.Clamp01((float)(currentTicks - startTicks) / (endTicks - startTicks))
                : 1f;

            transform.position = Vector3.Lerp(_startWorldPosition, _targetWorldPosition, t);
        }

        [Serializable]
        public class HexCoordinateDTO
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}