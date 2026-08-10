using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.HealthBars;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public class DemoRPGCharacterStatSheetView : MonoBehaviour
    {
        [Header("Character Info")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text ageText;
        [SerializeField] private TMP_Text cultureText;
        [SerializeField] private TMP_Text restedText;
        [SerializeField] private TMP_Text allegianceText;

        [Header("Core Stats")]
        [SerializeField] private TMP_Text strengthText;
        [SerializeField] private TMP_Text vitalityText;
        [SerializeField] private TMP_Text agilityText;
        [SerializeField] private TMP_Text valorText;
        [SerializeField] private TMP_Text vigorText;

        [Header("Resources")]
        [SerializeField] private AnimatedHealthBarFill hpBar;
        

        [SerializeField] private AnimatedHealthBarFill staminaBar;
      

        [Header("Experience")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider experienceSlider;

        [Header("Combat Stats - Left")]
        [SerializeField] private TMP_Text mainHandDamageText;
        [SerializeField] private TMP_Text criticalChanceText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text fumbleChanceText;
        [SerializeField] private TMP_Text dodgeChanceText;
        [SerializeField] private TMP_Text moveSpeedText;

        [Header("Combat Stats - Right")]
        [SerializeField] private TMP_Text offHandDamageText;
        [SerializeField] private TMP_Text criticalDamageText;
        [SerializeField] private TMP_Text attackSpeedText;
        [SerializeField] private TMP_Text armorPenetrationText;
        [SerializeField] private TMP_Text blockChanceText;
        [SerializeField] private TMP_Text parryChanceText;

        [Header("Defense Stats")]
        [SerializeField] private TMP_Text armorText;
        [SerializeField] private TMP_Text statusEffectResistanceText;
        [SerializeField] private TMP_Text slashingResistanceText;
        [SerializeField] private TMP_Text piercingResistanceText;
        [SerializeField] private TMP_Text crushingResistanceText;
        [SerializeField] private TMP_Text fireResistanceText;

        [NonSerialized]private RPGCharacterData currentCharacter;

        public Image strengthMainStatFrame;
        public Image vitalityMainStatFrame;
        public Image agilityMainStatFrame;
        public Image valorMainStatFrame;
        public Image vigorMainStatFrame;

        public List<Image> allFramesMainStats;

        public void InitializeForCharacter(RPGCharacterData character, bool animateBars = false)
        {
            currentCharacter = character;
            Refresh(animateBars);
        }

        public void Refresh(bool animateBars = true)
        {
            if (currentCharacter == null)
            {
                Clear();
                return;
            }

            SetCharacterInfo();
            SetCoreStats();
            SetResources(animateBars);
            SetExperience();
            SetCombatStats();
            SetDefenseStats();
            
            for (var i = 0; i < allFramesMainStats.Count; i++)
            {
                var frame = allFramesMainStats[i];
                frame.enabled = false;
            }

            switch (currentCharacter.mainAttribute)
            {
                case RPGStatType.Strength:
                    strengthMainStatFrame.enabled = true;
                    break;
                case RPGStatType.Vitality:
                    vitalityMainStatFrame.enabled = true;
                    break;
                case RPGStatType.Agility:
                    agilityMainStatFrame.enabled = true;
                    break;
                case RPGStatType.Valor:
                    valorMainStatFrame.enabled = true;
                    break;
                case RPGStatType.Vigor:
                    vigorMainStatFrame.enabled = true;
                    break;
            }
        }

        private void SetCharacterInfo()
        {
            SetText(characterNameText, currentCharacter.CharacterName);
            SetText(ageText, $"Age: {currentCharacter.Age}");
            SetText(cultureText, currentCharacter.Culture.ToString());
            SetText(restedText, currentCharacter.IsRested ? "Rested" : "Tired");
            SetText(allegianceText, currentCharacter.Allegiance.ToString());
        }

        private void SetCoreStats()
        {
            SetStatInt(strengthText, RPGStatType.Strength);
            SetStatInt(vitalityText, RPGStatType.Vitality);
            SetStatInt(agilityText, RPGStatType.Agility);
            SetStatInt(valorText, RPGStatType.Valor);
            SetStatInt(vigorText, RPGStatType.Vigor);
        }

        private void SetResources(bool animateBars)
        {
            float hp = Stat(RPGStatType.HP);
            float maxHp = Stat(RPGStatType.MaxHP);

            float stamina = Stat(RPGStatType.Stamina);
            float maxStamina = Stat(RPGStatType.MaxStamina);
            if (hpBar != null)
            {
                if (animateBars)
                    hpBar.AnimateToHealth(hp, maxHp);
                else
                    hpBar.SetHealthInstant(hp, maxHp);
            }

            if (staminaBar != null)
            {
                if (animateBars)
                    staminaBar.AnimateToHealth(stamina, maxStamina);
                else
                    staminaBar.SetHealthInstant(stamina, maxStamina);
            }
        }

        private void SetExperience()
        {
            float level = Stat(RPGStatType.Level);
            float experience = Stat(RPGStatType.Experience);
            float experienceToNextLevel = Stat(RPGStatType.ExperienceToNextLevel);

            SetText(levelText, $"Level {Round(level)}");
            if (experienceSlider != null)
            {
                experienceSlider.minValue = 0f;
                experienceSlider.maxValue = Mathf.Max(1f, experienceToNextLevel);
                experienceSlider.value = Mathf.Clamp(experience, 0f, experienceSlider.maxValue);
            }
        }

        private void SetCombatStats()
        {
            SetStatInt(mainHandDamageText, RPGStatType.MainHandDamage);
            SetStatPercent(criticalChanceText, RPGStatType.CriticalChance);
            SetStatPercent(accuracyText, RPGStatType.Accuracy);
            SetStatPercent(fumbleChanceText, RPGStatType.FumbleChance);
            SetStatPercent(dodgeChanceText, RPGStatType.DodgeChance);
            SetStatInt(moveSpeedText, RPGStatType.MoveSpeed);

            SetStatInt(offHandDamageText, RPGStatType.OffHandDamage);
            SetStatMultiplier(criticalDamageText, RPGStatType.CriticalDamage);
            SetStatMultiplier(attackSpeedText, RPGStatType.AttackSpeed);
            SetStatMultiplier(armorPenetrationText, RPGStatType.ArmorPenetration);
            SetStatPercent(blockChanceText, RPGStatType.BlockChance);
            SetStatPercent(parryChanceText, RPGStatType.ParryChance);
        }

        private void SetDefenseStats()
        {
            SetStatInt(armorText, RPGStatType.Armor);
            SetStatPercent(statusEffectResistanceText, RPGStatType.StatusEffectResistance);
            SetStatPercent(slashingResistanceText, RPGStatType.SlashingResistance);
            SetStatPercent(piercingResistanceText, RPGStatType.PiercingResistance);
            SetStatPercent(crushingResistanceText, RPGStatType.CrushingResistance);
            SetStatPercent(fireResistanceText, RPGStatType.FireResistance);
        }

        public void Clear()
        {
            SetText(characterNameText, string.Empty);
            SetText(ageText, string.Empty);
            SetText(cultureText, string.Empty);
            SetText(restedText, string.Empty);
            SetText(allegianceText, string.Empty);

            SetText(strengthText, string.Empty);
            SetText(vitalityText, string.Empty);
            SetText(agilityText, string.Empty);
            SetText(valorText, string.Empty);
            SetText(vigorText, string.Empty);

            SetText(levelText, string.Empty);

            SetText(mainHandDamageText, string.Empty);
            SetText(criticalChanceText, string.Empty);
            SetText(accuracyText, string.Empty);
            SetText(fumbleChanceText, string.Empty);
            SetText(dodgeChanceText, string.Empty);
            SetText(moveSpeedText, string.Empty);

            SetText(offHandDamageText, string.Empty);
            SetText(criticalDamageText, string.Empty);
            SetText(attackSpeedText, string.Empty);
            SetText(armorPenetrationText, string.Empty);
            SetText(blockChanceText, string.Empty);
            SetText(parryChanceText, string.Empty);

            SetText(armorText, string.Empty);
            SetText(statusEffectResistanceText, string.Empty);
            SetText(slashingResistanceText, string.Empty);
            SetText(piercingResistanceText, string.Empty);
            SetText(crushingResistanceText, string.Empty);
            SetText(fireResistanceText, string.Empty);

            if (hpBar != null)
                hpBar.SetHealthInstant(0f, 1f);

            if (staminaBar != null)
                staminaBar.SetHealthInstant(0f, 1f);

            if (experienceSlider != null)
            {
                experienceSlider.minValue = 0f;
                experienceSlider.maxValue = 1f;
                experienceSlider.value = 0f;
            }
        }

        private float Stat(RPGStatType type)
        {
            return currentCharacter != null ? currentCharacter.GetStat(type) : 0f;
        }

        private void SetStatInt(TMP_Text target, RPGStatType type)
        {
            SetText(target, Round(Stat(type)).ToString());
        }

        private void SetStatPercent(TMP_Text target, RPGStatType type)
        {
            SetText(target, $"{Round(Stat(type))}%");
        }

        private void SetStatMultiplier(TMP_Text target, RPGStatType type)
        {
            SetText(target, $"{FormatOneOptionalDecimal(Stat(type))}x");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static int Round(float value)
        {
            return Mathf.RoundToInt(value);
        }

        private static string FormatOneOptionalDecimal(float value)
        {
            if (Mathf.Approximately(value, Mathf.Round(value)))
                return Mathf.RoundToInt(value).ToString();

            return value.ToString("0.0");
        }
    }
}
