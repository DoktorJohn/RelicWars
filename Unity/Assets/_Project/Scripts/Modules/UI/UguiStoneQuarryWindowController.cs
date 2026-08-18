using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiStoneQuarryWindowController : MonoBehaviour
    {
        private TMP_Text _currentLevelLabel;
        private TMP_Text _productionHeaderLabel;
        private Transform _rowsRoot;
        private GameObject _rowTemplate;
        private readonly List<GameObject> _runtimeRows = new();
        private int _requestVersion;

        private void Awake()
        {
            _currentLevelLabel = FindComponent<TMP_Text>(transform, "Current level");
            Transform productionHeader = FindTransform(transform, "ProductionHeader");
            _productionHeaderLabel = productionHeader != null
                ? productionHeader.GetComponentInChildren<TMP_Text>(true)
                : null;
            _rowTemplate = FindTransform(transform, "StoneQuarryBuildingLevelDataRow")?.gameObject;
            _rowsRoot = _rowTemplate != null ? _rowTemplate.transform.parent : null;

            SetText(_productionHeaderLabel, "STONE / HOUR");
            SetActive(_rowTemplate, false);
        }

        private void OnEnable()
        {
            ClearRows();
            LoadProjection();
        }

        private void OnDisable()
        {
            _requestVersion++;
            StopAllCoroutines();
            ClearRows();
        }

        private void LoadProjection()
        {
            int version = ++_requestVersion;
            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.Building == null || !network.ActiveCityId.HasValue)
            {
                ShowEmptyState();
                return;
            }

            StartCoroutine(network.Building.GetResourceProductionInfo(
                network.ActiveCityId.Value,
                BuildingTypeEnum.StoneQuarry,
                network.JwtToken,
                dataList =>
                {
                    if (!CanApply(version)) return;

                    if (dataList == null || dataList.Count == 0)
                    {
                        ShowEmptyState();
                        return;
                    }

                    Render(dataList);
                }));
        }

        private void Render(IEnumerable<ResourceBuildingInfoDTO> dataList)
        {
            List<ResourceBuildingInfoDTO> levels = dataList
                .OrderBy(item => item.Level)
                .ToList();

            ResourceBuildingInfoDTO currentLevel = levels.Find(item => item.IsCurrentLevel);
            SetText(_currentLevelLabel, currentLevel != null ? $"LEVEL {currentLevel.Level}" : "-");

            ClearRows();
            if (_rowsRoot == null || _rowTemplate == null) return;

            foreach (ResourceBuildingInfoDTO level in levels)
            {
                GameObject row = Instantiate(_rowTemplate, _rowsRoot, false);
                row.name = _rowTemplate.name;
                BindRow(row.transform, level);
                row.SetActive(true);
                _runtimeRows.Add(row);
            }
        }

        private static void BindRow(Transform row, ResourceBuildingInfoDTO level)
        {
            TMP_Text levelText = FindComponent<TMP_Text>(row, "LevelText");
            TMP_Text productionText = FindComponent<TMP_Text>(row, "PopulationText");
            SetText(levelText, level.Level.ToString("N0"));
            SetText(productionText, $"+{level.ProductionPrHour:N0}");

            SetBold(levelText, level.IsCurrentLevel);
            SetBold(productionText, level.IsCurrentLevel);
            SetActive(FindTransform(row, "Background Highlit")?.gameObject, level.IsCurrentLevel);
        }

        private void ShowEmptyState()
        {
            SetText(_currentLevelLabel, "-");
            ClearRows();
        }

        private void ClearRows()
        {
            foreach (GameObject row in _runtimeRows)
            {
                if (row != null) Destroy(row);
            }

            _runtimeRows.Clear();
            SetActive(_rowTemplate, false);
        }

        private bool CanApply(int version) => isActiveAndEnabled && version == _requestVersion;

        private static T FindComponent<T>(Transform root, string objectName) where T : Component
        {
            Transform target = FindTransform(root, objectName);
            if (target == null) return null;

            T component = target.GetComponent<T>();
            return component != null ? component : target.GetComponentInChildren<T>(true);
        }

        private static Transform FindTransform(Transform root, string objectName)
        {
            if (root == null) return null;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child.name.Equals(objectName, StringComparison.Ordinal)) return child;

                Transform nested = FindTransform(child, objectName);
                if (nested != null) return nested;
            }

            return null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }

        private static void SetBold(TMP_Text text, bool bold)
        {
            if (text == null) return;
            text.fontStyle = bold
                ? text.fontStyle | FontStyles.Bold
                : text.fontStyle & ~FontStyles.Bold;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }
    }
}
