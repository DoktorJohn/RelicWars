using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;


namespace Sunvale.AncientRomeUI.Buttons
{
    [AddComponentMenu("Sunvale/AncientRomeUI/RowHighlightButton")]
    public class RowHighlightButton : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("References")] public Image background;

          
        [Header("Sounds")]
        [SerializeField] public UISoundConfig clickSoundConfig;
        [SerializeField] public UISoundConfig hoverSoundConfig;
       


        [Header("Colors")] public Color highlightColor;
        public Color clickColor;
        public Color normalColor;

        [Header("Unity Events")] public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public UnityEvent buttonActivatedClicked;

        private bool isHovered;
        private bool isPointerDown;

        public delegate void MyDelegateForButtonInteraction(RowHighlightButton theButton);

        public event MyDelegateForButtonInteraction OnPointerDownEvent;
        public event MyDelegateForButtonInteraction OnPointerUpEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerEnterEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerExitEvent;
        public event MyDelegateForButtonInteraction OnButtonActivatedClicked;

        private void OnEnable()
        {
            isHovered = false;
            isPointerDown = false;
            SetColor(normalColor);
        }

        private void SetColor(Color color)
        {
            if (background != null)
            {
                background.color = color;
            }
        }

        public void GoToHoverState()
        {
            SetColor(highlightColor);
        }

        public void GoToPointerExitState()
        {
            SetColor(normalColor);
        }

        public void GoToClickDownState()
        {
            SetColor(clickColor);

           SimpleSoundManager.Play(clickSoundConfig);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;

            OnPointerDownEvent?.Invoke(this);
            onUnityPointerDown?.Invoke();

            GoToClickDownState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;

            OnPointerUpEvent?.Invoke(this);
            onUnityPointerUp?.Invoke();

            if (isHovered)
            {
                buttonActivatedClicked?.Invoke();
                OnButtonActivatedClicked?.Invoke(this);

                GoToHoverState();
            }
            else
            {
                GoToPointerExitState();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            OnButtonPointerEnterEvent?.Invoke(this);
            onUnityPointerEnter?.Invoke();

           SimpleSoundManager.Play(hoverSoundConfig);

            if (!isPointerDown)
            {
                GoToHoverState();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            OnButtonPointerExitEvent?.Invoke(this);
            onUnityPointerExit?.Invoke();

            if (!isPointerDown)
            {
                GoToPointerExitState();
            }
        }
    }
}
