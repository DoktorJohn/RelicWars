using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Domain.State;
using Assets._Project.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.WorldPlayer;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets._Project.Scripts.Modules.UI
{
    public partial class IdeologyWindowController
    {
        private void RenderOverviewSection(IdeologyDTO ideologyDto)
        {
            if (_labelIdeologyName != null)
                _labelIdeologyName.text = ideologyDto.Name.ToUpper();

            if (_labelIdeologyDescription != null)
                _labelIdeologyDescription.text = ideologyDto.Description;
        }

        private void PopulateFocusGrid(List<IdeologyFocusDTO> focuses)
        {
            if (_focusGridContainer == null || _focusCardTemplate == null) return;
            _focusGridContainer.Clear();

            foreach (var focus in focuses)
            {
                VisualElement cardInstance = _focusCardTemplate.Instantiate();
                VisualElement actualCard = cardInstance.Q<VisualElement>(null, "focus-card");

                var nameLbl = actualCard.Q<Label>("Card-Name");
                var descLbl = actualCard.Q<Label>("Card-Description");
                var costInfoLbl = actualCard.Q<Label>("Card-CostInfo");

                if (nameLbl != null) nameLbl.text = focus.Name.ToFriendlyName().ToUpper();
                if (descLbl != null) descLbl.text = focus.Description;

                var enactBtn = actualCard.Q<Button>("Btn-Enact");
                var statusLbl = actualCard.Q<Label>("Lbl-Status");

                enactBtn.userData = new FocusButtonState(focus.IdeologyFocusPointCost, focus.IsAvailable);

                if (focus.AlreadyEnacted)
                {
                    enactBtn.style.display = DisplayStyle.None;
                    statusLbl.style.display = DisplayStyle.Flex;

                    if (costInfoLbl != null)
                        costInfoLbl.text = $"{focus.IdeologyFocusPointCost} PTS";

                    if (focus.ActiveTime.HasValue)
                    {
                        var timerCoroutine = StartCoroutine(CountdownTimerRoutine(statusLbl, focus.ExpirationTime));
                        _activeTimers.Add(timerCoroutine);
                    }
                    else
                    {
                        statusLbl.text = "ENACTED";
                    }
                }
                else
                {
                    statusLbl.style.display = DisplayStyle.None;
                    enactBtn.style.display = DisplayStyle.Flex;

                    if (focus.ActiveTime.HasValue)
                    {
                        string timeString = "";
                        if (focus.ActiveTime.Value.TotalHours >= 1) timeString += $"{(int)focus.ActiveTime.Value.TotalHours}H ";
                        if (focus.ActiveTime.Value.Minutes > 0) timeString += $"{focus.ActiveTime.Value.Minutes}M";
                        timeString = timeString.Trim();

                        if (costInfoLbl != null)
                            costInfoLbl.text = $"{focus.IdeologyFocusPointCost} PTS | TIME: {timeString}";
                    }
                    else
                    {
                        if (costInfoLbl != null)
                            costInfoLbl.text = $"{focus.IdeologyFocusPointCost} PTS | INSTANT";
                    }

                    enactBtn.SetEnabled(focus.IsAvailable && _currentAvailablePoints >= focus.IdeologyFocusPointCost);
                    if (!focus.IsAvailable && costInfoLbl != null)
                        costInfoLbl.text = focus.UnavailableReason;
                    enactBtn.clicked += () => ExecuteEnactFocus(focus.Name, enactBtn);
                }

                _focusGridContainer.Add(actualCard);
            }
        }

        private IEnumerator CountdownTimerRoutine(Label targetLabel, DateTime expirationTime)
        {
            while (true)
            {
                TimeSpan remaining = expirationTime - DateTime.UtcNow;

                if (remaining.TotalSeconds <= 0)
                {
                    targetLabel.text = "EXPIRED";
                    RequestAndRenderIdeologyData(_requestVersion);
                    yield break;
                }

                targetLabel.text = string.Format("ACTIVE\n{0:D2}:{1:D2}:{2:D2}",
                    (int)remaining.TotalHours, remaining.Minutes, remaining.Seconds);

                yield return new WaitForSeconds(1f);
            }
        }

        private void StopAllActiveTimers()
        {
            foreach (var timer in _activeTimers)
            {
                if (timer != null) StopCoroutine(timer);
            }

            _activeTimers.Clear();
        }

        private IEnumerator ShowEffectResultAndRefresh(string summary)
        {
            if (_labelIdeologyDescription != null) _labelIdeologyDescription.text = summary;
            yield return new WaitForSeconds(3f);
            RequestAndRenderIdeologyData(_requestVersion);
        }

        private sealed class FocusButtonState
        {
            public double Cost { get; }
            public bool IsAvailable { get; }

            public FocusButtonState(double cost, bool isAvailable)
            {
                Cost = cost;
                IsAvailable = isAvailable;
            }
        }
    }
}
