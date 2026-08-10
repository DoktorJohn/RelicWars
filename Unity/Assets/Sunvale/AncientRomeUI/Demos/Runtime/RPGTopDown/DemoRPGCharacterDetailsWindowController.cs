using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public class DemoRPGCharacterDetailsWindowController : MonoBehaviour
    {
        public RPGDemoController myManager;

        public CarvedPressButton closeButton;

        public DemoRPGInventorySectionController inventorySection;

        public DemoRPGSkillsTraitsSectionController skillsTraitsSection;

        public CarvedPressButton cancelButton;

        public TextMeshProUGUI nameLabel;

        public List<TextColorTabButton> myTabs;
        public List<GameObject> contentSections;

        private TextColorTabButton currentTab;
        private GameObject currentContentSection;

        private RPGCharacterData currentCharacter;

        public enum CharacterDetailsTab
        {
            inventory,
            skills,
            items,
            traits
        }

        private CharacterDetailsTab currMode;
        private bool wasInitialized;

        private void InnerInitialization()
        {
            if (wasInitialized)
            {
                return;
            }

            wasInitialized = true;

            closeButton.OnButtonActivatedClicked += CloseButtonClicked;
            cancelButton.OnButtonActivatedClicked += CancelButtonClicked;

            for (var i = 0; i < myTabs.Count; i++)
            {
                var tab = myTabs[i];
                tab.OnButtonActivatedClicked += TabClicked;
            }
        }


        private void TabClicked(TextColorTabButton theTab)
        {
            if (theTab == currentTab)
            {
                return;
            }

            int index = myTabs.IndexOf(theTab);

            InitializeContentAtIndex(index);
        }

        private void InitializeContentAtIndex(int idx)
        {
            var theTab = myTabs[idx];
            if (currentTab != null)
            {
                currentTab.SetAsDeselected(true);
            }

            currentTab = theTab;
            currentTab.SetSelected(true, true);

            if (currentContentSection != null)
            {
                currentContentSection.gameObject.SetActive(false);
            }

            currentContentSection = contentSections[idx];
            currentContentSection.gameObject.SetActive(true);

            currMode = (CharacterDetailsTab)idx;
            switch (currMode)
            {
                case CharacterDetailsTab.inventory:
                    inventorySection.InitializeForCharacter(currentCharacter, myManager, false);
                    break;
                case CharacterDetailsTab.items:
                    inventorySection.InitializeForCharacter(currentCharacter, myManager, false);
                    break;
                case CharacterDetailsTab.traits:
                    skillsTraitsSection.InitializeForCharacter(currentCharacter, myManager);
                    break;
                case CharacterDetailsTab.skills:
                    skillsTraitsSection.InitializeForCharacter(currentCharacter, myManager);
                    break;
            }
        }

        private void CancelButtonClicked(CarvedPressButton thebutton)
        {
            CloseTheWindow();
        }


        public void InitializeForCharacter(RPGCharacterData character)
        {
            InnerInitialization();
            gameObject.SetActive(true);
            character.OnCharacterDirty -= CharacterDirtyUpdateEverything;
            character.OnCharacterDirty += CharacterDirtyUpdateEverything;

            nameLabel.SetText(character.CharacterName);

            currentCharacter = character;

            int currentIndex = 0;
            if (currentTab != null)
            {
                currentIndex = myTabs.IndexOf(currentTab);
            }

            InitializeContentAtIndex(currentIndex);
        }


        private void CharacterDirtyUpdateEverything(RPGCharacterData character)
        {
            switch (currMode)
            {
                case CharacterDetailsTab.inventory:
                    inventorySection.InitializeForCharacter(character, myManager, true);
                    break;
                case CharacterDetailsTab.items:
                    inventorySection.InitializeForCharacter(character, myManager, true);
                    break;
            }
        }


        private void CloseButtonClicked(CarvedPressButton btn)
        {
            CloseTheWindow();
        }

        private void CloseTheWindow()
        {
            gameObject.SetActive(false);
        }

        public static string GetShortenedName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            string[] parts = fullName
                .Trim()
                .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return parts[0];

            string lastName = parts[parts.Length - 1];

            List<string> initials = new List<string>();

            for (int i = 0; i < parts.Length - 1; i++)
            {
                initials.Add(parts[i][0] + ". ");
            }

            return string.Join("", initials) + lastName;
        }
    }
}
