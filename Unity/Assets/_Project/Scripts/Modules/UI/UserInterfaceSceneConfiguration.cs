using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Project.Scripts.Modules.UI
{
    public class UserInterfaceSceneConfiguration : MonoBehaviour
    {
        [Header("HUD Kontrakt for denne scene")]
        public bool NeedTopBar = true;
        public bool NeedLeftSideBar = true;
        public bool NeedUnitStackIdeology = true;
        public bool NeedUnitDeploymentSideBar = true;
    }
}
