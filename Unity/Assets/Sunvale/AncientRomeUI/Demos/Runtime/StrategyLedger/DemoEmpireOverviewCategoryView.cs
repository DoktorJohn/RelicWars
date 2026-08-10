
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoEmpireOverviewCategoryView : MonoBehaviour
    {
            public StrategyLedgerDemoController myManagerStrategyLedger;
            public DemoGovernmentSectionController governmentSection;
            public DemoDemographicsOverviewSectionController demographicsSection;
            public DemoMilitaryOverviewSectionController militarySection;
            public DemoProductionOverviewSectionController productionSection;
            public DemoTreasurySectionController treasurySection;
            
            public void Initialize()
            {
                    governmentSection.InitializeForGovernment(myManagerStrategyLedger.governmentData);
                    demographicsSection.Initialize(myManagerStrategyLedger.empireStatsData);
                    militarySection.Initialize(myManagerStrategyLedger.militaryStatsData);
                    productionSection.Initialize(myManagerStrategyLedger.empireStatsData);
                    treasurySection.Initialize(myManagerStrategyLedger.empireStatsData);
            }

           
    }
}
