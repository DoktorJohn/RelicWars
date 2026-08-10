using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.StrategyTopDown
{
    public class DemoConstructionPanelController : MonoBehaviour
    {
            public DemoProvincePanelController myManger;
            public List<LargeBuildingButton> activeButtonsList;
            public CarvedPressButton closeButton;

            public TextMeshProUGUI numberOfSlotsLabel;

            private bool wasInitialized;

            private void InnerInitialization()
            {
                    if (wasInitialized)
                    {
                            return;
                    }

                    wasInitialized = true;

                    closeButton.OnButtonActivatedClicked += CloseButtonPressed;

                    for (var i = 0; i < activeButtonsList.Count; i++)
                    {
                            var btn = activeButtonsList[i];
                            btn.OnButtonActivatedClicked += BuildingButtonClicked;
                    }
            }

            private void OnDestroy()
            {
                    if (!wasInitialized)
                            return;

                    closeButton.OnButtonActivatedClicked -= CloseButtonPressed;

                    for (var i = 0; i < activeButtonsList.Count; i++)
                    {
                            var btn = activeButtonsList[i];

                            if (btn != null)
                                    btn.OnButtonActivatedClicked -= BuildingButtonClicked;
                    }
            }

            private void BuildingButtonClicked(LargeBuildingButton thebutton)
            {
                    int index = activeButtonsList.IndexOf(thebutton);

                    if (index < 0)
                            return;

                    DemoProvinceBuildingType buildingType = GetBuildingTypeFromButtonIndex(index);

                    if (buildingType == DemoProvinceBuildingType.noneExistingNull)
                            return;

                    bool constructionWasQueued = myManger.NewBuildingConstruction(buildingType);

                    if (constructionWasQueued)
                    {
                            CloseTheWindow();
                    }
                    else
                    {
                            Refresh();
                    }
            }

            private DemoProvinceBuildingType GetBuildingTypeFromButtonIndex(int index)
            {
                    // Button list starts at brickwork.
                    // Enum starts with noneExistingNull, so offset by +1.
                    int enumValue = index + 1;

                    if (!Enum.IsDefined(typeof(DemoProvinceBuildingType), enumValue))
                            return DemoProvinceBuildingType.noneExistingNull;

                    return (DemoProvinceBuildingType)enumValue;
            }

            public void Initialize()
            {
                    gameObject.SetActive(true);
                    InnerInitialization();
                    Refresh();
            }

            public void Refresh()
            {
                    int slotsLeft = 0;

                    if (myManger != null)
                    {
                            slotsLeft = myManger.GetRemainingBuildingSlots();
                    }

                    numberOfSlotsLabel.SetText(slotsLeft.ToString());
            }

            private void CloseButtonPressed(CarvedPressButton button)
            {
                    CloseTheWindow();
            }

            public void CloseTheWindow()
            {
                    gameObject.SetActive(false);
            }
    }
}
