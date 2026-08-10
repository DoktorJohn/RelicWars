using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Sunvale.Common.UI;

namespace Sunvale.AncientRomeUI.Demos.DemoHub
{
    [DefaultExecutionOrder(-10000)]
    public class DemoHubController : MonoBehaviour
    {
        public TextMeshProUGUI sceneNameTMP;

        public GameObject optionsUI;
        public GameObject strategyUI;
        public GameObject rpgUI;
        public GameObject strategy2;
        public GameObject mainMenu;
        public GameObject tooltipExamples;

        private int index;

        public SimpleButton leftArrow;
        public SimpleButton rightArrow;

        public bool demoIsON;

        [Header("Input Compatibility")]
        [Tooltip("Assign the single EventSystem used by this demo scene.")]
        public EventSystem eventSystem;

        [Tooltip("Automatically fixes the assigned EventSystem module depending on the project's active input handling.")]
        public bool autoFixEventSystem = true;

     

        public void Awake()
        {
            FixAssignedEventSystemIfNeeded();
            if (leftArrow != null)
                leftArrow.OnButtonActivatedClicked += LeftArrowPressed;

            if (rightArrow != null)
                rightArrow.OnButtonActivatedClicked += RightArrowPressed;
        }

        private void OnDestroy()
        {
            if (leftArrow != null)
                leftArrow.OnButtonActivatedClicked -= LeftArrowPressed;

            if (rightArrow != null)
                rightArrow.OnButtonActivatedClicked -= RightArrowPressed;
        }

        private void Start()
        {
            if (!demoIsON)
                return;

            SetScenery();
        }

        public void LeftArrowPressed(SimpleButton theButton)
        {
            index--;

            if (index < 0)
                index = 5;

            SetScenery();
        }

        public void RightArrowPressed(SimpleButton theButton)
        {
            index++;

            if (index > 5)
                index = 0;

            SetScenery();
        }

        private void SetScenery()
        {
            optionsUI.gameObject.SetActive(false);
            strategyUI.gameObject.SetActive(false);
            rpgUI.gameObject.SetActive(false);
            mainMenu.gameObject.SetActive(false);
            strategy2.gameObject.SetActive(false);
            tooltipExamples.gameObject.SetActive(false);

            switch (index)
            {
                case 0:
                    optionsUI.gameObject.SetActive(true);
                    sceneNameTMP.SetText("Options demo");
                    break;

                case 1:
                    strategyUI.gameObject.SetActive(true);
                    sceneNameTMP.SetText("Strategy demo");
                    break;

                case 2:
                    rpgUI.gameObject.SetActive(true);
                    sceneNameTMP.SetText("Rpg demo");
                    break;

                case 3:
                    strategy2.gameObject.SetActive(true);
                    sceneNameTMP.SetText("Strategy2 Demo");
                    break;

                case 4:
                    mainMenu.gameObject.SetActive(true);
                    sceneNameTMP.SetText("Main menu Demo");
                    break;

                case 5:
                    tooltipExamples.SetActive(true);
                    sceneNameTMP.SetText("Tooltips Demo");
                    break;
            }

            Canvas.ForceUpdateCanvases();
        }

        private void FixAssignedEventSystemIfNeeded()
        {
            if (!autoFixEventSystem)
                return;

            if (eventSystem == null)
            {
                Debug.LogWarning(
                    "Sunvale Ancient Rome UI Demo: No EventSystem assigned to DemoHubController. " +
                    "Please assign the scene's single EventSystem in the Inspector."
                );

                return;
            }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // Project is set to New Input System only.
            // StandaloneInputModule uses legacy UnityEngine.Input and can spam errors in this mode.
            StandaloneInputModule[] legacyModules = eventSystem.GetComponents<StandaloneInputModule>();

            for (int i = 0; i < legacyModules.Length; i++)
            {
                if (legacyModules[i] != null)
                    Destroy(legacyModules[i]);
            }

            // Reflection avoids a direct compile dependency on Unity.InputSystem.
            Type inputSystemModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
            );

            if (inputSystemModuleType == null)
            {
                Debug.LogWarning(
                    "Sunvale Ancient Rome UI Demo: Project uses the new Input System, " +
                    "but InputSystemUIInputModule was not found. Make sure Unity's Input System package is installed."
                );

                return;
            }

            if (eventSystem.GetComponent(inputSystemModuleType) == null)
                eventSystem.gameObject.AddComponent(inputSystemModuleType);

#else
            // Old Input Manager or Both mode.
            // StandaloneInputModule is safe here and keeps the demo compatible with older projects.
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}