using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.TooltipScene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TooltipWindow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;

        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = GetComponent<RectTransform>();

                return rectTransform;
            }
        }

        public bool IsVisible { get; private set; }
        public bool IsPointerInside { get; private set; }
        public TooltipTrigger Owner { get; private set; }

        private void Reset()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show(TooltipTrigger owner)
        {
            Owner = owner;
            IsVisible = true;
            IsPointerInside = false;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = false;
            }
        }

        public void Hide()
        {
            Owner = null;
            IsVisible = false;
            IsPointerInside = false;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        public bool WantsToStayOpen(TooltipTrigger source)
        {
            if (IsPointerInside)
                return true;

            if (source != null && source.IsPointerInside)
                return true;

            return false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsPointerInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsPointerInside = false;
        }

        private void OnDisable()
        {
            IsPointerInside = false;
            IsVisible = false;
            Owner = null;
        }
    }
}
