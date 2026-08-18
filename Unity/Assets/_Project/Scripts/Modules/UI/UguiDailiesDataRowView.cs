using System;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Graphics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    [ExecuteAlways]
    public sealed class UguiDailiesDataRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text objectiveLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private SimpleFillBar progressFillBar;
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private UguiDailyRewardItemView rewardItemTemplate;
        [SerializeField] private UguiDailyRewardsCollectArea rewardsCollectArea;
        [SerializeField] private Sprite coinsIcon;
        [SerializeField] private Sprite woodIcon;
        [SerializeField] private Sprite stoneIcon;
        [SerializeField] private Sprite metalIcon;
        [SerializeField, Range(0f, 1f)] private float collectedAlpha = 0.5f;
        [SerializeField, Min(0f)] private float twoRewardSpacing = 10f;

        private CanvasGroup _rowCanvasGroup;
        private HorizontalLayoutGroup _rewardsLayout;
        private float _defaultRowAlpha;

        private void Awake()
        {
            if (rewardsContainer != null)
                _rewardsLayout = rewardsContainer.GetComponent<HorizontalLayoutGroup>();

            if (!Application.isPlaying)
            {
                ApplyEditorPreviewLayout();
                return;
            }

            _rowCanvasGroup = GetComponent<CanvasGroup>();
            if (_rowCanvasGroup == null) _rowCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            _defaultRowAlpha = _rowCanvasGroup.alpha;
            if (rewardItemTemplate != null) rewardItemTemplate.gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            ApplyEditorPreviewLayout();
        }

        private void OnTransformChildrenChanged()
        {
            if (!Application.isPlaying) ApplyEditorPreviewLayout();
        }

        public void Bind(DailyObjectiveRowDTO objective, Action<int> onCollect)
        {
            if (objective == null) return;
            objectiveLabel.text = string.IsNullOrWhiteSpace(objective.CompletionInfo)
                ? objective.Name
                : objective.CompletionInfo;

            bool comingSoon = objective.State == DailyObjectiveState.ComingSoon;
            bool completed = objective.IsCompleted || objective.State == DailyObjectiveState.Complete;
            bool isCollectable = completed && !objective.IsCollected && !comingSoon;
            _rowCanvasGroup.alpha = objective.IsCollected ? collectedAlpha : _defaultRowAlpha;
            RenderRewards(objective, isCollectable);
            rewardsCollectArea?.Configure(isCollectable,
                () => onCollect?.Invoke(objective.DefinitionId));

            if (comingSoon)
            {
                progressLabel.text = "COMING SOON";
                progressFillBar?.SetNormalizedValue(0f);
                return;
            }

            float normalizedProgress = objective.Target > 0d
                ? Mathf.Clamp01((float)(objective.Progress / objective.Target))
                : 0f;
            progressFillBar?.SetNormalizedValue(normalizedProgress);
            progressLabel.text = objective.IsCollected
                ? "Collected"
                : $"{Math.Floor(objective.Progress):N0}/{Math.Floor(objective.Target):N0}";
        }

        private void RenderRewards(DailyObjectiveRowDTO objective, bool isCollectable)
        {
            if (rewardsContainer == null || rewardItemTemplate == null) return;
            for (int index = rewardsContainer.childCount - 1; index >= 0; index--)
            {
                Transform child = rewardsContainer.GetChild(index);
                if (child != rewardItemTemplate.transform) Destroy(child.gameObject);
            }

            int rewardCount = objective.Rewards?.Count ?? 0;
            ConfigureRewardsLayout(rewardCount);
            if (objective.Rewards == null) return;
            foreach (DailyObjectiveRewardDTO reward in objective.Rewards)
            {
                UguiDailyRewardItemView item = Instantiate(rewardItemTemplate, rewardsContainer, false);
                item.gameObject.SetActive(true);
                item.Bind(reward, GetIcon(reward.Type), isCollectable);
            }
        }

        private void ConfigureRewardsLayout(int rewardCount)
        {
            if (_rewardsLayout == null) return;

            _rewardsLayout.childForceExpandWidth = rewardCount >= 3;
            _rewardsLayout.spacing = rewardCount == 2 ? twoRewardSpacing : 0f;
            _rewardsLayout.childAlignment = rewardCount == 1
                ? TextAnchor.MiddleCenter
                : TextAnchor.MiddleLeft;
        }

        private void ApplyEditorPreviewLayout()
        {
            if (Application.isPlaying || rewardsContainer == null) return;
            _rewardsLayout = rewardsContainer.GetComponent<HorizontalLayoutGroup>();
            ConfigureRewardsLayout(rewardsContainer.childCount);
        }

        private Sprite GetIcon(DailyObjectiveRewardType type) => type switch
        {
            DailyObjectiveRewardType.Coins => coinsIcon,
            DailyObjectiveRewardType.Wood => woodIcon,
            DailyObjectiveRewardType.Stone => stoneIcon,
            DailyObjectiveRewardType.Metal => metalIcon,
            _ => null
        };
    }
}
