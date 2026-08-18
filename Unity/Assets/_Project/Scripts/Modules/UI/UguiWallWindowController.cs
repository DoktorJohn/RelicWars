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
    public sealed class UguiWallWindowController : MonoBehaviour
    {
        private TMP_Text _currentLevelLabel;
        private Transform _rowsRoot;
        private GameObject _rowTemplate;
        private readonly List<GameObject> _runtimeRows = new();
        private int _requestVersion;

        private void Awake()
        {
            _currentLevelLabel = FindComponent<TMP_Text>(transform, "Current level");
            _rowTemplate = FindTransform(transform, "WallBuildingLevelDataRow")?.gameObject;
            _rowsRoot = _rowTemplate != null ? _rowTemplate.transform.parent : null;

            // The authored row remains an editor preview and is never runtime table content by itself.
            SetActive(_rowTemplate, false);
        }

        private void OnEnable()
        {
            ClearRows();
            LoadWallProjection();
        }

        private void OnDisable()
        {
            _requestVersion++;
            StopAllCoroutines();
            ClearRows();
        }

        private void LoadWallProjection()
        {
            int version = ++_requestVersion;
            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.Building == null || !network.ActiveCityId.HasValue)
            {
                ShowEmptyState();
                return;
            }

            Guid cityId = network.ActiveCityId.Value;
            StartCoroutine(network.Building.GetWallInfo(cityId, network.JwtToken, dataList =>
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

        private void Render(IEnumerable<WallInfoDTO> dataList)
        {
            List<WallInfoDTO> levels = dataList
                .OrderBy(item => item.Level)
                .ToList();

            WallInfoDTO currentLevel = levels.Find(item => item.IsCurrentLevel);
            SetText(_currentLevelLabel, currentLevel != null ? $"LEVEL {currentLevel.Level}" : "-");

            ClearRows();
            if (_rowsRoot == null || _rowTemplate == null) return;

            foreach (WallInfoDTO level in levels)
            {
                GameObject row = Instantiate(_rowTemplate, _rowsRoot, false);
                row.name = _rowTemplate.name;
                BindRow(row.transform, level);
                row.SetActive(true);
                _runtimeRows.Add(row);
            }
        }

        private static void BindRow(Transform row, WallInfoDTO level)
        {
            TMP_Text levelText = FindComponent<TMP_Text>(row, "LevelText");
            TMP_Text defenceText = FindComponent<TMP_Text>(row, "DefenceText");
            SetText(levelText, level.Level.ToString("N0"));
            SetText(defenceText, FormatDefence(level));

            SetBold(levelText, level.IsCurrentLevel);
            SetBold(defenceText, level.IsCurrentLevel);
            SetActive(FindTransform(row, "Background Highlit")?.gameObject, level.IsCurrentLevel);
        }

        private static string FormatDefence(WallInfoDTO level)
        {
            if (level.DefensiveModifier == null) return "0%";

            return level.DefensiveModifier.ModifierType == ModifierTypeEnum.Increased
                ? $"+{level.DefensiveModifier.Value * 100:0.#}%"
                : $"+{level.DefensiveModifier.Value:0.#}";
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
