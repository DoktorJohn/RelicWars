using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.World
{

    public class WorldSelectionEntryLinker : MonoBehaviour
    {
        [Header("Tekst Referencer")]
        public TMP_Text worldNameLabel;
        public TMP_Text playerCountLabel;

        [Header("Interaktions Referencer")]
        public Button actionExecutionButton;
        public TMP_Text actionButtonLabel;
    }

}