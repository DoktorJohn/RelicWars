using Sunvale.AncientRomeUI.Buttons;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiAllianceWindowNavigationController : MonoBehaviour
    {
        [SerializeField] private FramedSpriteTabButton overviewTab;
        [SerializeField] private FramedSpriteTabButton membersTab;
        [SerializeField] private FramedSpriteTabButton politicsTab;
        [SerializeField] private FramedSpriteTabButton congressTab;
        [SerializeField] private GameObject overviewContent;
        [SerializeField] private GameObject membersContent;
        [SerializeField] private GameObject politicsContent;
        [SerializeField] private GameObject congressContent;

        private void OnEnable()
        {
            overviewTab.OnButtonActivatedClicked += ShowOverview;
            membersTab.OnButtonActivatedClicked += ShowMembers;
            politicsTab.OnButtonActivatedClicked += ShowPolitics;
            congressTab.OnButtonActivatedClicked += ShowCongress;
            ShowMembers(membersTab);
        }

        private void OnDisable()
        {
            overviewTab.OnButtonActivatedClicked -= ShowOverview;
            membersTab.OnButtonActivatedClicked -= ShowMembers;
            politicsTab.OnButtonActivatedClicked -= ShowPolitics;
            congressTab.OnButtonActivatedClicked -= ShowCongress;
        }

        private void ShowOverview(FramedSpriteTabButton _) => Select(overviewTab, overviewContent);
        private void ShowMembers(FramedSpriteTabButton _) => Select(membersTab, membersContent);
        private void ShowPolitics(FramedSpriteTabButton _) => Select(politicsTab, politicsContent);
        private void ShowCongress(FramedSpriteTabButton _) => Select(congressTab, congressContent);

        private void Select(FramedSpriteTabButton selectedTab, GameObject selectedContent)
        {
            overviewContent.SetActive(selectedContent == overviewContent);
            membersContent.SetActive(selectedContent == membersContent);
            politicsContent.SetActive(selectedContent == politicsContent);
            congressContent.SetActive(selectedContent == congressContent);
            overviewTab.SetSelected(selectedTab == overviewTab, false);
            membersTab.SetSelected(selectedTab == membersTab, false);
            politicsTab.SetSelected(selectedTab == politicsTab, false);
            congressTab.SetSelected(selectedTab == congressTab, false);
        }
    }
}
