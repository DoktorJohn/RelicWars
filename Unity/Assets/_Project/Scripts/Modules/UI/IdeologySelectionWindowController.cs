using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.Enums;

namespace Project.Modules.IdeologySelection
{
    [RequireComponent(typeof(UIDocument))]
    public class IdeologySelectionSceneController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;
        private VisualElement _ideologyCardsContainer;

        [Header("UI Skabeloner")]
        [SerializeField] private VisualTreeAsset _ideologyCardTemplate;

        [Header("Scene Konfiguration")]
        [SerializeField] private string _nextGameplaySceneName = "CityViewScene";

        private void OnEnable()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;

            InitializeUserInterfaceElements();
            StartIdeologySelectionListPopulation();
        }

        private void InitializeUserInterfaceElements()
        {
            _ideologyCardsContainer = _rootVisualElement.Q<VisualElement>("Container-Ideology-Cards");

            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[IdeologySelection] NetworkManager instance not found. Return to Bootstrap.");
            }
        }

        private void StartIdeologySelectionListPopulation()
        {
            if (_ideologyCardsContainer == null) return;

            _ideologyCardsContainer.Clear();

            foreach (IdeologyTypeEnum ideologyType in Enum.GetValues(typeof(IdeologyTypeEnum)))
            {
                if (ideologyType == IdeologyTypeEnum.None) continue;

                VisualElement ideologyCardInstance = _ideologyCardTemplate.CloneTree();

                Label titleLabel = ideologyCardInstance.Q<Label>("Label-Ideology-Title");
                Label descriptionLabel = ideologyCardInstance.Q<Label>("Label-Ideology-Description");
                Button selectButton = ideologyCardInstance.Q<Button>("Button-Select-Ideology");

                titleLabel.text = ideologyType.ToString();
                descriptionLabel.text = GetIdeologyVerboseDescription(ideologyType);

                selectButton.clicked += () => HandleIdeologySelectionRequest(ideologyType);

                _ideologyCardsContainer.Add(ideologyCardInstance);
            }
        }

        private void HandleIdeologySelectionRequest(IdeologyTypeEnum selectedIdeology)
        {
            if (NetworkManager.Instance == null) return;

            Debug.Log($"[IdeologySelection] Attempting to enact ideology: {selectedIdeology}");

            // Vi kalder nu direkte ind i NetworkManager wrapperen
            NetworkManager.Instance.SelectIdeology(selectedIdeology, (isSelectionSuccessful) =>
            {
                if (isSelectionSuccessful)
                {
                    Debug.Log("[IdeologySelection] Selection confirmed. Loading next scene.");
                    SceneManager.LoadScene(_nextGameplaySceneName);
                }
                else
                {
                    Debug.LogError("[IdeologySelection] Failed to enact ideology. Server rejected request.");
                }
            });
        }

        private string GetIdeologyVerboseDescription(IdeologyTypeEnum ideologyType)
        {
            return ideologyType switch
            {
                IdeologyTypeEnum.Feudalism => "A hierarchical sociopolitical system in which vassals and nobles held the land of a ruling king, in exchange for military and economical obligations." +
                "\n \n 4% increased tax rate, 5% less building cost",
                IdeologyTypeEnum.Monarchy => "An inherited form of government in which a single person, the monarch, serves as the head of state until death." +
                "\n \n 8% increased tax rate",
                IdeologyTypeEnum.Oligarchy => "A form of government in which the elite rules the land." +
                "\n \n 5% increased research rate, 10% increased population, 6% decreased tax rate",
                IdeologyTypeEnum.Democracy => "A form of government in which the rulers are elected by the people." +
                "\n \n 5% increased travel speed, 5% increased upkeep, 30% increased market silver, 2% increased tax rate",
                IdeologyTypeEnum.MilitaryJunta => "A form of government in which the land is ruled by the army and its leaders themselves." +
                "\n \n 10% decreased tax rate, 8% decreased upkeep, 5% increased recruitment speed",
                _ => "Choose your path to govern your new empire."
            };
        }
    }
}