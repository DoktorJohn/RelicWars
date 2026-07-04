using Project.Modules.City;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.UI
{
    public partial class TownHallWindowController
    {
        private readonly List<QueueTimerBinding> _queueTimerBindings = new List<QueueTimerBinding>();

        private void PopulateConstructionQueue(List<BuildingDTO> constructionJobs)
        {
            constructionJobs ??= new List<BuildingDTO>();
            _constructionQueueContainer.Clear();
            _currentQueueCount = constructionJobs.Count;

            if (_queueTimerCoroutine != null)
            {
                StopCoroutine(_queueTimerCoroutine);
                _queueTimerCoroutine = null;
            }
            _queueTimerBindings.Clear();

            if (_queueHeaderLabel != null)
                _queueHeaderLabel.text = $"CONSTRUCTION QUEUE ({_currentQueueCount}/5)";

            RefreshBuildingGridStates();

            if (_currentQueueCount == 0)
            {
                var emptyQueueLabel = new Label("NO ACTIVE CONSTRUCTIONS");
                emptyQueueLabel.AddToClassList("queue-empty-label");
                _constructionQueueContainer.Add(emptyQueueLabel);
                return;
            }

            foreach (var job in constructionJobs)
            {
                var queueItemElement = new VisualElement();
                queueItemElement.AddToClassList("queue-item-card");

                var infoContainer = new VisualElement();
                infoContainer.AddToClassList("queue-item-info-container");

                var jobTitleLabel = new Label(job.Type.ToString().ToUpperInvariant());
                jobTitleLabel.AddToClassList("queue-item-title");

                var levelContainer = new VisualElement();
                levelContainer.AddToClassList("queue-item-level");
                levelContainer.Add(new Label($"LVL {job.Level - 1}"));

                var arrowLabel = new Label("->");
                arrowLabel.AddToClassList("queue-level-arrow");
                levelContainer.Add(arrowLabel);

                var newLevelLabel = new Label(job.Level.ToString());
                newLevelLabel.AddToClassList("queue-level-new");
                levelContainer.Add(newLevelLabel);

                infoContainer.Add(jobTitleLabel);
                infoContainer.Add(levelContainer);

                var footerContainer = new VisualElement();
                footerContainer.AddToClassList("queue-item-footer");
                var timerDisplayLabel = new Label("--:--:--");
                timerDisplayLabel.AddToClassList("queue-item-time");
                footerContainer.Add(timerDisplayLabel);

                queueItemElement.Add(infoContainer);
                queueItemElement.Add(footerContainer);
                _constructionQueueContainer.Add(queueItemElement);

                if (job.UpgradeFinished.HasValue)
                    _queueTimerBindings.Add(new QueueTimerBinding(timerDisplayLabel, job.UpgradeFinished.Value));
            }

            if (_queueTimerBindings.Count > 0)
                _queueTimerCoroutine = StartCoroutine(UpdateConstructionQueueTimers());
        }

        private IEnumerator UpdateConstructionQueueTimers()
        {
            var waitInstruction = new WaitForSeconds(1f);
            while (isActiveAndEnabled)
            {
                bool hasFinishedJob = false;
                foreach (var binding in _queueTimerBindings)
                {
                    TimeSpan remaining = binding.FinishTimestamp - DateTime.UtcNow;
                    if (remaining.TotalSeconds <= 0)
                    {
                        binding.Label.text = "FINISHED";
                        hasFinishedJob = true;
                    }
                    else
                    {
                        binding.Label.text = remaining.ToString(@"hh\:mm\:ss");
                    }
                }

                if (hasFinishedJob)
                {
                    _queueTimerCoroutine = null;
                    CityStateManager.Instance?.InitiateResourceRefresh(_activeCityId);
                    yield break;
                }

                yield return waitInstruction;
            }

            _queueTimerCoroutine = null;
        }

        private sealed class QueueTimerBinding
        {
            public QueueTimerBinding(Label label, DateTime finishTimestamp)
            {
                Label = label;
                FinishTimestamp = finishTimestamp;
            }

            public Label Label { get; }
            public DateTime FinishTimestamp { get; }
        }
    }
}
