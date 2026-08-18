using Assets.Scripts.Domain.Enums;
using Domain.StaticData.Generators;
using Project.Modules.City;
using Project.Modules.UI.Windows.Implementations;
using Project.Network.Manager;
using Project.Scripts.Modules.Map;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public static class WindowNavigationHelper
    {
        public static Button CreateLinkButton(string text, Action onClick, string extraClass = null)
        {
            var button = new Button { text = text ?? string.Empty };
            button.AddToClassList("btn-entity-link");
            if (!string.IsNullOrWhiteSpace(extraClass))
            {
                button.AddToClassList(extraClass);
            }

            if (onClick != null)
            {
                button.clicked += onClick;
            }

            return button;
        }

        public static void OpenProfile(Guid worldPlayerId) => OpenWindow(WindowTypeEnum.Profile, worldPlayerId);

        public static void OpenAlliance(Guid allianceId) => OpenWindow(WindowTypeEnum.Alliance, allianceId);

        public static void OpenCityInspection(Guid cityId, int x, int y)
        {
            int seed = WorldMapStateManager.Instance?.CurrentWorldSeed ?? 0;
            var coordinates = new Vector2Int(x, y);
            var biome = WorldGenerationService.CalculateWorldMapBiomeVariant(
                checked((short)x),
                checked((short)y),
                seed);

            WorldMapInteractionHandler.Instance?.OpenCityInspection(new CityInspectionPayload
            {
                CityId = cityId,
                Coordinates = coordinates,
                TerrainName = biome.ToString()
            });
        }

        public static void OpenMessageToPlayer(Guid worldPlayerId) => OpenWindow(WindowTypeEnum.Message, worldPlayerId);

        public static void OpenCombatSimulator(object payload = null) => OpenWindow(WindowTypeEnum.CombatSimulator, payload);

        public static void OpenWindow(WindowTypeEnum windowType, object payload = null)
        {
            if (GlobalWindowManager.Instance == null)
            {
                return;
            }

            GlobalWindowManager.Instance.OpenWindow(windowType, payload);
        }
    }
}
