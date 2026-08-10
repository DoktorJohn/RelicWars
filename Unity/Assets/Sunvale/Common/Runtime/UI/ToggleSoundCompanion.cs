using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;


namespace Sunvale.Common.UI
{
    [AddComponentMenu("Sunvale/Common/ToggleSoundCompanion")]
    [RequireComponent(typeof(Toggle))]
    public class ToggleSoundCompanion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
            public Toggle toggle;

            [Header("Sounds")]
            public UISoundConfig buttonHoverConfig;
            public UISoundConfig buttonClickConfig;

            public void PlayHoverSound()
            {
                    SimpleSoundManager.Play(buttonHoverConfig);
            }

            public void PlayClickSound()
            {
                SimpleSoundManager.Play(buttonClickConfig);
            }
            


            private void Reset()
            {
                    toggle = GetComponent<Toggle>();
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                   PlayHoverSound();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                    
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                    PlayClickSound();
            }
    }

}
