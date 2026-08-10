using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.PieCharts;
using Sunvale.AncientRomeUI.PieCharts.Donut;


namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoGovernmentSectionController : MonoBehaviour
    {
        [Header("Portraits")]
        public DemoCharacterPortraitTagView consulPortraitOne;
        public DemoCharacterPortraitTagView consulPortraitTwo;
        public DemoCharacterPortraitTagView praetorPortrait;
        public DemoCharacterPortraitTagView senateLeaderPortrait;

        [Header("Senate")]
        public List<PieChartCategoryLabel> senateFactionsLabels;
        public EllipticalHalfDonutChart ellipticalSenatePieChartController;

        [Header("Label Options")]
        public bool showSeatCounter = false;

        public void InitializeForGovernment(RomeGovernmentData governmentData)
        {
            ApplyCharacter(consulPortraitOne, governmentData.consulA, "Consul");
            ApplyCharacter(consulPortraitTwo, governmentData.consulB, "Consul");
            ApplyCharacter(praetorPortrait, governmentData.praetor, "Praetor");
            ApplyCharacter(senateLeaderPortrait, governmentData.senateLeader, "Senate Leader");

            ApplySenateFactions(governmentData);
        }

        private void ApplyCharacter(
            DemoCharacterPortraitTagView portrait,
            RomeCharacterData character,
            string titleTag)
        {
            if (portrait == null || character == null)
                return;

            portrait.SetIconSprite(character.portraitSprite);
            portrait.SetNameLabelString(FormatRomanNameShort(character.name));
            portrait.SetBottomExtraLabel(titleTag);
        }

        private void ApplySenateFactions(RomeGovernmentData governmentData)
        {
            if (senateFactionsLabels == null)
                return;

            List<RomeSenateFactionData> factions = governmentData.factions;

            if (ellipticalSenatePieChartController != null)
                ellipticalSenatePieChartController.categories.Clear();

            int totalSeats = 0;

            if (factions != null)
            {
                for (int i = 0; i < factions.Count; i++)
                {
                    if (factions[i] != null)
                        totalSeats += Mathf.Max(0, factions[i].seats);
                }
            }

            int factionCount = factions != null ? factions.Count : 0;
            int visibleCount = Mathf.Min(factionCount, senateFactionsLabels.Count);

            for (int i = 0; i < senateFactionsLabels.Count; i++)
            {
                PieChartCategoryLabel label = senateFactionsLabels[i];

                if (label == null)
                    continue;

                bool hasFaction = i < visibleCount;
                label.gameObject.SetActive(hasFaction);

                if (!hasFaction)
                    continue;

                RomeSenateFactionData faction = factions[i];

                Color factionColor = GetColorFromLabel(label);
                Material factionMaterial = label.myMaterial;

                int seats = Mathf.Max(0, faction.seats);
                string percentageString = FormatPercentage(seats, totalSeats);

                SetFactionLabel(
                    label,
                    faction.name,
                    showSeatCounter ? seats.ToString() : "",
                    percentageString
                );

                if (ellipticalSenatePieChartController != null)
                {
                    ellipticalSenatePieChartController.categories.Add(
                        new EllipticalHalfDonutChart.DonutChartCategory
                        {
                            name = faction.name,
                            value = seats,
                            color = factionColor,
                            material = factionMaterial
                        }
                    );
                }
            }

            if (ellipticalSenatePieChartController != null)
                ellipticalSenatePieChartController.GenerateChart();
        }

        private static void SetFactionLabel(
            PieChartCategoryLabel label,
            string factionName,
            string counterString,
            string percentageString)
        {
           
                label.labelName.SetText(factionName + " " + percentageString);
                // label.thingCounter.SetText(counterString);
                // label.thingPercentages.SetText(percentageString);
        }

        private static string FormatPercentage(int seats, int totalSeats)
        {
            if (totalSeats <= 0)
                return "0%";

            int percentage = Mathf.RoundToInt((seats / (float)totalSeats) * 100f);
            return $"{percentage}%";
        }

        private static Color GetColorFromLabel(PieChartCategoryLabel label)
        {
            // Main intended source: the color you assign on the label row.
            if (label.myVertexColorTint.a > 0.001f)
                return label.myVertexColorTint;

            // Fallback: if the label uses a material color swatch.
            if (label.myMaterial != null && label.myMaterial.HasProperty("_Color"))
                return label.myMaterial.color;

            return Color.white;
        }

        private static string FormatRomanNameShort(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "";

            string[] parts = fullName.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (parts.Length == 1)
                return parts[0];

            if (parts.Length == 2)
                return $"{GetInitial(parts[0])}. {parts[1]}";

            string firstInitial = GetInitial(parts[0]).ToString();
            string secondInitial = GetInitial(parts[1]).ToString();
            string lastName = parts[parts.Length - 1];

            return $"{firstInitial}. {secondInitial}. {lastName}";
        }

        private static char GetInitial(string namePart)
        {
            if (string.IsNullOrWhiteSpace(namePart))
                return '?';

            return char.ToUpperInvariant(namePart.Trim()[0]);
        }
    }
}
