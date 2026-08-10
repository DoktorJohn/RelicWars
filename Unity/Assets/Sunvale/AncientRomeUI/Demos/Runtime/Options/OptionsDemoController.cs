using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.Options
{
    public class OptionsDemoController : MonoBehaviour
    {
        public OptionsTabButton[] tabs;

        public GameObject[] tabContent;

        private OptionsTabButton currentTab;
        private GameObject currentActiveContent;

        public RectTransform tabFrameForSelectedTab;
        
        private bool wasInitialized;

        [Header("Sounds")] public UISoundConfig tabSwitchSoundConfig;
        

        public TMP_Dropdown languagesDropDown;
        public TMP_Dropdown resolutionsDropdown;
        public TMP_Dropdown qualitySettings;
        public TMP_Dropdown shadowsDropdown;
        public TMP_Dropdown unitsDropDown;
        
        private void Start()
        {
            Initialize();
            
            
        }

        private void PutSomeLanguagesInDropDown()
        {
            languagesDropDown.ClearOptions();

            List<string> languages = new List<string>
            {
                "English",
                "Spanish",
                "French",
                "German",
                "Italian",
                "Japanese",
                "Korean",
                "Chinese"
            };

            languagesDropDown.AddOptions(languages);

            // Default selected option
            languagesDropDown.SetValueWithoutNotify(0);
            languagesDropDown.RefreshShownValue();
        }

        private readonly Vector2Int[] demoResolutions =
        {
            new Vector2Int(1024, 768),
            new Vector2Int(1280, 720),
            new Vector2Int(1280, 800),
            new Vector2Int(1366, 768),
            new Vector2Int(1440, 900),
            new Vector2Int(1600, 900),
            new Vector2Int(1680, 1050),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };

        private void PutResolutionIntoDropDown()
        {
            resolutionsDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            int currentWidth = Screen.width;
            int currentHeight = Screen.height;

            for (int i = 0; i < demoResolutions.Length; i++)
            {
                Vector2Int resolution = demoResolutions[i];

                string option = resolution.x + " x " + resolution.y;
                options.Add(option);

                if (resolution.x == currentWidth && resolution.y == currentHeight)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionsDropdown.AddOptions(options);

            // Set initial value before listening, so it does not fire immediately.
            resolutionsDropdown.value = currentResolutionIndex;
            resolutionsDropdown.RefreshShownValue();
        }

        private void PutQualitySettingsIntoDropDown()
        {
            qualitySettings.ClearOptions();

            List<string> qualityOptions = new List<string>
            {
                "Low",
                "Medium",
                "High",
                "Ultra"
            };

            qualitySettings.AddOptions(qualityOptions);
            qualitySettings.SetValueWithoutNotify(2);
            qualitySettings.RefreshShownValue();
            
            shadowsDropdown.ClearOptions();
            shadowsDropdown.AddOptions(qualityOptions);
            shadowsDropdown.SetValueWithoutNotify(2);
            shadowsDropdown.RefreshShownValue();
            
            unitsDropDown.ClearOptions();
            unitsDropDown.AddOptions(qualityOptions);
            unitsDropDown.SetValueWithoutNotify(2);
            unitsDropDown.RefreshShownValue();
        }

        public void Initialize()
        {
            InnerInitialization();

            //first start
            if (currentTab == null)
            {
                currentTab = tabs[0];
                currentTab.SetAsSelectedAsPrime(false);
                currentActiveContent = tabContent[0];
                currentActiveContent.gameObject.SetActive(true);

                // 1. Force the entire Canvas to compute nested layouts right now because the outer frame will otherwise not allign
                Canvas.ForceUpdateCanvases();
                tabFrameForSelectedTab.anchoredPosition = currentTab.myRectTransform.anchoredPosition;
            }
            
            PutSomeLanguagesInDropDown();
            PutResolutionIntoDropDown();
            PutQualitySettingsIntoDropDown();
        }
        
        

        private void InnerInitialization()
        {
            if (wasInitialized)
            {
                return;
            }

            wasInitialized = true;

            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                tab.OnPointerDownEvent += TabWasPressed;
            }
        }


        

        
        private void TabWasPressed(OptionsTabButton tab)
        {
            if (currentTab == tab)
            {
                return;
            }

            if (currentTab != null)
            {
                currentTab.SetAsDeselected(true);
                currentActiveContent.gameObject.SetActive(false);
            }

            currentTab = tab;
            currentActiveContent = tabContent[GetTabIndex(tab)];
            currentActiveContent.gameObject.SetActive(true);
            currentTab.SetAsSelectedAsPrime(true);
            tabFrameForSelectedTab.anchoredPosition = currentTab.myRectTransform.anchoredPosition;
            
            SimpleSoundManager.Play(tabSwitchSoundConfig);
        }

        private int GetTabIndex(OptionsTabButton tabWeAreLookingFor)
        {
            for (var i = 0; i < tabs.Length; i++)
            {
                var t = tabs[i];
                if (t == tabWeAreLookingFor)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
