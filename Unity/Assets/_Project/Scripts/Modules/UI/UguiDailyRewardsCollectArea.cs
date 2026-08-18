using System;
using Sunvale.Common.Sound;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class UguiDailyRewardsCollectArea : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private UISoundConfig hoverSound;
        [SerializeField] private UISoundConfig clickSound;

        private Action _onCollect;
        private bool _canCollect;
        private bool _requestInFlight;
        private Image _raycastGraphic;

        private void Awake()
        {
            _raycastGraphic = GetComponent<Image>();
            _raycastGraphic.color = Color.clear;
        }

        public void Configure(bool canCollect, Action onCollect)
        {
            _canCollect = canCollect;
            _requestInFlight = false;
            _onCollect = onCollect;
            if (_raycastGraphic == null) _raycastGraphic = GetComponent<Image>();
            _raycastGraphic.raycastTarget = canCollect;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_canCollect && !_requestInFlight) SimpleSoundManager.Play(hoverSound);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_canCollect || _requestInFlight || eventData.button != PointerEventData.InputButton.Left) return;
            _requestInFlight = true;
            _raycastGraphic.raycastTarget = false;
            SimpleSoundManager.Play(clickSound);
            _onCollect?.Invoke();
        }
    }
}
