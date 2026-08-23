using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiUniversityWindowController : MonoBehaviour
    {
        private TMP_Text _currentLevelLabel;
        private Transform _rowsRoot;
        private GameObject _rowTemplate;
        private readonly List<GameObject> _runtimeRows = new();
        private int _requestVersion;

        private void Awake()
        {
            _currentLevelLabel = FindComponent<TMP_Text>(transform, "Current level");
            _rowTemplate = FindTransform(transform, "UniversityBuildingLevelDataRow")?.gameObject;
            _rowsRoot = _rowTemplate != null ? _rowTemplate.transform.parent : null;

            // The authored row is a runtime template, never table content by itself.
            SetActive(_rowTemplate, false);
        }

        private void OnEnable()
        {
            ClearRows();
            LoadUniversityProjection();
        }

        private void OnDisable()
        {
            _requestVersion++;
            StopAllCoroutines();
            ClearRows();
        }

        private void LoadUniversityProjection()
        {
            int version = ++_requestVersion;
            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.Building == null || !network.ActiveCityId.HasValue)
            {
                ShowEmptyState();
                return;
            }

            Guid cityId = network.ActiveCityId.Value;
            StartCoroutine(network.Building.GetUniversityInfo(cityId, network.JwtToken, dataList =>
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

        private void Render(IEnumerable<UniversityInfoDTO> dataList)
        {
            List<UniversityInfoDTO> levels = dataList
                .OrderBy(item => item.Level)
                .ToList();

            UniversityInfoDTO currentLevel = levels.Find(item => item.IsCurrentLevel);
            SetText(_currentLevelLabel, currentLevel != null ? $"LEVEL {currentLevel.Level}" : "-");

            ClearRows();
            if (_rowsRoot == null || _rowTemplate == null) return;

            foreach (UniversityInfoDTO level in levels)
            {
                GameObject row = Instantiate(_rowTemplate, _rowsRoot, false);
                row.name = _rowTemplate.name;
                BindRow(row.transform, level);
                row.SetActive(true);
                _runtimeRows.Add(row);
            }
        }

        private static void BindRow(Transform row, UniversityInfoDTO level)
        {
            TMP_Text levelText = FindComponent<TMP_Text>(row, "LevelText");
            TMP_Text researchPowerText = FindComponent<TMP_Text>(row, "ResearchPowerText");
            SetText(levelText, level.Level.ToString("N0"));
            SetText(researchPowerText, level.ResearchPower.ToString("F2", CultureInfo.InvariantCulture));

            SetBold(levelText, level.IsCurrentLevel);
            SetBold(researchPowerText, level.IsCurrentLevel);
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
