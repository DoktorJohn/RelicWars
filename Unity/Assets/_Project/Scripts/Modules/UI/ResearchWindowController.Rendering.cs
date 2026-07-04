using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.UI
{
    public partial class ResearchWindowController
    {
        private void UpdateResearchPointsDisplay(double currentPoints)
        {
            if (_researchPointsLabel != null) _researchPointsLabel.text = currentPoints.ToString("N0");
        }

        private void PopulateResearchTreeVisuals(List<ResearchNodeDTO> nodes)
        {
            if (_researchTreeContainer == null) return;
            HideLockedResearchTooltip();
            _researchTreeContainer.Clear();

            var filteredNodes = nodes.Where(node => node.ResearchType == _currentSelectedCategory).ToList();

            foreach (var node in filteredNodes)
            {
                AddResearchNodeToUI(node);
            }
        }

        private void AddResearchNodeToUI(ResearchNodeDTO nodeData)
        {
            VisualElement nodeCard = new VisualElement();
            nodeCard.AddToClassList("research-node");

            Label title = new Label(nodeData.Name);
            title.AddToClassList("node-title");
            nodeCard.Add(title);

            Label desc = new Label(nodeData.Description);
            desc.AddToClassList("node-description");
            nodeCard.Add(desc);

            VisualElement costRow = new VisualElement();
            costRow.AddToClassList("node-cost-row");

            Label costLabel = new Label($"{nodeData.ResearchPointCost:N0} RP");
            costLabel.AddToClassList("node-cost-label");
            costRow.Add(costLabel);

            if (nodeData.IsCompleted)
            {
                nodeCard.AddToClassList("node-completed");
                Label completedLabel = new Label("DONE");
                completedLabel.AddToClassList("node-status-text-done");
                costRow.Add(completedLabel);
            }
            else if (nodeData.IsLocked)
            {
                nodeCard.AddToClassList("node-locked");
                nodeCard.RegisterCallback<MouseEnterEvent>(evt => ShowLockedResearchTooltip(evt, nodeData));
                nodeCard.RegisterCallback<MouseLeaveEvent>(HideLockedResearchTooltip);
                nodeCard.RegisterCallback<MouseMoveEvent>(UpdateLockedResearchTooltipPosition);

                Button lockedBtn = new Button();
                lockedBtn.text = "LOCKED";
                lockedBtn.AddToClassList("btn-global-base");
                lockedBtn.AddToClassList("node-button-locked");
                lockedBtn.SetEnabled(false);
                lockedBtn.style.height = 24;
                lockedBtn.style.fontSize = 10;
                lockedBtn.style.marginTop = 0;
                costRow.Add(lockedBtn);
            }
            else if (nodeData.IsResearching)
            {
                nodeCard.AddToClassList("node-researching");

                Button researchingBtn = new Button();
                researchingBtn.text = "RESEARCHING";
                researchingBtn.AddToClassList("btn-global-base");
                researchingBtn.AddToClassList("node-button-researching");
                researchingBtn.SetEnabled(false);
                researchingBtn.style.height = 24;
                researchingBtn.style.fontSize = 10;
                researchingBtn.style.marginTop = 0;
                costRow.Add(researchingBtn);
            }
            else
            {
                Button researchBtn = new Button(() => RequestStartResearch(nodeData.Id));
                researchBtn.text = _activeResearchJob != null ? "BUSY" : "START";

                researchBtn.AddToClassList("btn-global-base");
                researchBtn.AddToClassList(_activeResearchJob != null ? "btn-imperial-primary" : "btn-imperial-success");

                researchBtn.style.height = 24;
                researchBtn.style.fontSize = 10;
                researchBtn.style.marginTop = 0;

                researchBtn.SetEnabled(nodeData.CanAfford && _activeResearchJob == null);
                costRow.Add(researchBtn);
            }

            nodeCard.Add(costRow);
            _researchTreeContainer.Add(nodeCard);
        }

        private void HandleActiveResearchJobDisplay(ActiveResearchJobDTO activeJob)
        {
            if (_activeJobPanel != null) _activeJobPanel.style.display = DisplayStyle.Flex;

            if (_activeTimerCoroutine != null) StopCoroutine(_activeTimerCoroutine);

            if (activeJob == null)
            {
                // Ingen aktiv forskning: Vis "IDLE" og disable knappen
                if (_activeResearchNameLabel != null) _activeResearchNameLabel.text = "IDLE";
                if (_activeResearchTimerLabel != null) _activeResearchTimerLabel.text = "--:--:--";
                _currentCancelResearchJobId = Guid.Empty;

                if (_cancelResearchButton != null)
                {
                    _cancelResearchButton.text = string.Empty;
                    _cancelResearchButton.SetEnabled(false);
                    _cancelResearchButton.style.display = DisplayStyle.None;
                }
            }
            else
            {
                // Aktiv forskning: Vis info og enable knappen
                var researchInfo = _cachedResearchNodes.FirstOrDefault(n => n.Id == activeJob.ResearchId);
                if (_activeResearchNameLabel != null)
                    _activeResearchNameLabel.text = researchInfo != null ? researchInfo.Name.ToUpper() : activeJob.ResearchId;

                _currentCancelResearchJobId = activeJob.JobId;
                if (_cancelResearchButton != null)
                {
                    _cancelResearchButton.text = "CANCEL RESEARCH";
                    _cancelResearchButton.SetEnabled(true);
                    _cancelResearchButton.style.display = DisplayStyle.Flex;
                }

                _activeTimerCoroutine = StartCoroutine(ExecuteActiveResearchCountdownTimer(activeJob.ExpectedCompletionTime));
            }
        }

        private IEnumerator ExecuteActiveResearchCountdownTimer(DateTime completionTime)
        {
            while (true)
            {
                TimeSpan remainingTime = completionTime - DateTime.UtcNow;

                if (remainingTime.TotalSeconds <= 0)
                {
                    if (_activeResearchTimerLabel != null) _activeResearchTimerLabel.text = "00:00:00";
                    RefreshResearchWindowState(_requestVersion);
                    yield break;
                }

                if (_activeResearchTimerLabel != null)
                {
                    _activeResearchTimerLabel.text = remainingTime.ToString(@"hh\:mm\:ss");
                }

                yield return new WaitForSeconds(1.0f);
            }
        }

        private void ShowLockedResearchTooltip(IMouseEvent mouseEvent, ResearchNodeDTO nodeData)
        {
            if (_lockedResearchTooltip == null)
            {
                return;
            }

            if (!nodeData.IsLocked)
            {
                HideLockedResearchTooltip();
                return;
            }

            if (_lockedResearchTooltipBodyLabel != null)
            {
                _lockedResearchTooltipBodyLabel.text = BuildLockedResearchTooltipText(nodeData);
            }

            _lockedResearchTooltip.BringToFront();
            _lockedResearchTooltip.style.display = DisplayStyle.Flex;
            UpdateLockedResearchTooltipPosition(mouseEvent);
        }

        private void HideLockedResearchTooltip(EventBase _ = null)
        {
            if (_lockedResearchTooltip != null)
            {
                _lockedResearchTooltip.style.display = DisplayStyle.None;
            }
        }

        private void UpdateLockedResearchTooltipPosition(IMouseEvent mouseEvent)
        {
            if (_lockedResearchTooltip == null || _lockedResearchTooltip.style.display == DisplayStyle.None || _lockedResearchTooltip.parent == null)
            {
                return;
            }

            Vector2 screenPosition = mouseEvent.mousePosition;
            Vector2 localPosition = _lockedResearchTooltip.parent.WorldToLocal(screenPosition);

            float availableWidth = Root != null ? Root.resolvedStyle.width : 0f;
            float availableHeight = Root != null ? Root.resolvedStyle.height : 0f;
            float tooltipWidth = GetResolvedDimension(_lockedResearchTooltip.resolvedStyle.width, 280f);
            float tooltipHeight = GetResolvedDimension(_lockedResearchTooltip.resolvedStyle.height, 100f);
            const float viewportMargin = 10f;
            const float cursorOffset = 18f;

            float preferredLeft = localPosition.x + cursorOffset;
            if (preferredLeft + tooltipWidth > availableWidth - viewportMargin)
            {
                preferredLeft = localPosition.x - tooltipWidth - cursorOffset;
            }

            float maxLeft = Mathf.Max(viewportMargin, availableWidth - tooltipWidth - viewportMargin);
            float maxTop = Mathf.Max(48f, availableHeight - tooltipHeight - viewportMargin);

            _lockedResearchTooltip.style.left = Mathf.Clamp(preferredLeft, viewportMargin, maxLeft);
            _lockedResearchTooltip.style.top = Mathf.Clamp(localPosition.y + cursorOffset, 48f, maxTop);
        }

        private static float GetResolvedDimension(float resolvedDimension, float fallback)
        {
            return float.IsNaN(resolvedDimension) || resolvedDimension <= 0f ? fallback : resolvedDimension;
        }

        private string BuildLockedResearchTooltipText(ResearchNodeDTO nodeData)
        {
            if (string.IsNullOrWhiteSpace(nodeData.ParentId))
            {
                return "No prerequisite listed.";
            }

            var parentNode = _cachedResearchNodes.FirstOrDefault(node => string.Equals(node.Id, nodeData.ParentId, StringComparison.OrdinalIgnoreCase));
            if (parentNode != null)
            {
                return parentNode.Name.ToUpperInvariant();
            }

            return nodeData.ParentId.ToUpperInvariant();
        }
    }
}
