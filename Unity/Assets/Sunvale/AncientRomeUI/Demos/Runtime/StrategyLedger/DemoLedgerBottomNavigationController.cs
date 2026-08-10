using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoLedgerBottomNavigationController : MonoBehaviour
    {
            [SerializeField] public List<CircularIconTabButton> buttons;


            private CircularIconTabButton currentlySelected;


            private void Start()
            {
                    currentlySelected = buttons[0];
                    currentlySelected.SetAsSelectedAsPrime(false);

                    for (var i = 0; i < buttons.Count; i++)
                    {
                            var btn = buttons[i];
                            btn.OnButtonActivatedClicked += ButtonClicked;
                    }
            }


            private void ButtonClicked(CircularIconTabButton theTab)
            {
                    if (currentlySelected == theTab)
                    {
                            return;
                    }
                    
                    if (currentlySelected != null)
                    {
                            currentlySelected.SetAsDeselected(true);
                    }

                    theTab.SetAsSelectedAsPrime(true);
                    currentlySelected = theTab;
            }
    }
}
