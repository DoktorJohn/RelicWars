using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.State
{
    [Serializable]
    public struct CityActivityState
    {
        public bool TownHallIsBusy;
        public string ActiveBuildingName;
        public int BuildingsInQueue;
        public bool BarracksIsBusy;
        public string ActiveUnitName;
        public int UnitsInQueue;
    }
}
