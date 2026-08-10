using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public enum RPGStatType
    {
        // Core stats
        Strength,
        Vitality,
        Agility,
        Valor,
        Vigor,

        // Resources
        HP,
        MaxHP,
        Stamina,
        MaxStamina,

        // Progression
        Level,
        Experience,
        ExperienceToNextLevel,

        // Combat
        MainHandDamage,
        OffHandDamage,
        CriticalChance,
        CriticalDamage,
        Accuracy,
        AttackSpeed,
        FumbleChance,
        ArmorPenetration,
        DodgeChance,
        BlockChance,
        MoveSpeed,
        ParryChance,

        // Defense
        Armor,
        StatusEffectResistance,
        SlashingResistance,
        PiercingResistance,
        CrushingResistance,
        FireResistance
    }

    public enum RPGCharacterCulture
    {
        Roman,
        Greek,
        Gaul,
        Egyptian,
        Carthaginian
    }

    public enum RPGCharacterAllegiance
    {
        Devoted,
        Loyal,
        Neutral,
        Questionable,
        Rebel
    }

    [Serializable]
    public class RPGStatValue
    {
        public RPGStatType Type;
        public float Value;

        public RPGStatValue(RPGStatType type, float value)
        {
            Type = type;
            Value = value;
        }
    }

    [Serializable]
    public class RPGCharacterData
    {
        public string CharacterName;
        public int Age;
        public RPGCharacterCulture Culture;
        public RPGCharacterAllegiance Allegiance;
        public int skillTreeVersion;
        public int skillTreePoints;
        
        public int darkSilhuetteIndex;
        public bool IsRested;

        public Sprite portraitSprite;
        public int portraitIndex;

        public RPGStatType mainAttribute;

        private readonly List<RPGStatValue> stats = new();

        private readonly Dictionary<RPGItemDefinitionSO, List<RPGItemBuff>> itemBuffsByItem = new();

        public RPGCharacterInventory myInventory;
        public IReadOnlyList<RPGStatValue> Stats => stats;

        public IReadOnlyDictionary<RPGItemDefinitionSO, List<RPGItemBuff>> ItemBuffsByItem => itemBuffsByItem;

        public delegate void MyDelegateForCharacterDirty(RPGCharacterData character);

        public event MyDelegateForCharacterDirty OnCharacterDirty;

        public RPGCharacterData(string characterName)
        {
            CharacterName = characterName;
        }

        public void BindWithInventory(RPGCharacterInventory inventory)
        {
            myInventory = inventory;
        }

        public float GetBaseStat(RPGStatType type)
        {
            RPGStatValue stat = stats.FirstOrDefault(s => s.Type == type);
            return stat != null ? stat.Value : 0f;
        }

        public float GetStat(RPGStatType type)
        {
            float baseValue = GetBaseStat(type);

            GetItemBuffTotals(type, out float flatBonus, out float percentBonus);

            float finalValue = baseValue + flatBonus;
            finalValue *= 1f + percentBonus / 100f;

            return finalValue;
        }

        private void GetItemBuffTotals(RPGStatType type, out float flatBonus, out float percentBonus)
        {
            flatBonus = 0f;
            percentBonus = 0f;

            foreach (KeyValuePair<RPGItemDefinitionSO, List<RPGItemBuff>> pair in itemBuffsByItem)
            {
                List<RPGItemBuff> buffs = pair.Value;

                for (int i = 0; i < buffs.Count; i++)
                {
                    RPGItemBuff buff = buffs[i];

                    if (buff == null)
                        continue;

                    if (buff.statType != type)
                        continue;

                    switch (buff.modifierType)
                    {
                        case RPGStatModifierType.flat:
                            flatBonus += buff.value;
                            break;

                        case RPGStatModifierType.percent:
                            percentBonus += buff.value;
                            break;
                    }
                }
            }
        }

        public void SetStat(RPGStatType type, float value)
        {
            RPGStatValue stat = stats.FirstOrDefault(s => s.Type == type);

            if (stat == null)
            {
                stats.Add(new RPGStatValue(type, value));
            }
            else
            {
                stat.Value = value;
            }

            MarkDirty();
        }

        public void AddToStat(RPGStatType type, float amount)
        {
            SetStat(type, GetBaseStat(type) + amount);
        }

        public void MultiplyStat(RPGStatType type, float multiplier)
        {
            SetStat(type, GetBaseStat(type) * multiplier);
        }

        public void ApplyBuff(RPGStatType type, float flatAmount)
        {
            AddToStat(type, flatAmount);
        }

        public void ApplyPercentBuff(RPGStatType type, float percent)
        {
            float multiplier = 1f + percent / 100f;
            MultiplyStat(type, multiplier);
        }

        public void ApplyItemBuffs(RPGItemDefinitionSO item, RPGEquipmentSlot equippedSlot)
        {
            if (item == null)
                return;

            RemoveItemBuffs(item);

            if (item.itemBuffs == null || item.itemBuffs.Count == 0)
            {
                MarkDirty();
                return;
            }

            List<RPGItemBuff> appliedBuffs = CreateSlotAdjustedItemBuffs(item, equippedSlot);

            itemBuffsByItem.Add(item, appliedBuffs);

            ClampResourceStats();
            MarkDirty();
        }

        public void RemoveItemBuffs(RPGItemDefinitionSO item)
        {
            if (item == null)
                return;

            bool removed = itemBuffsByItem.Remove(item);

            if (!removed)
                return;

            ClampResourceStats();
            MarkDirty();
        }


        private static List<RPGItemBuff> CreateSlotAdjustedItemBuffs(RPGItemDefinitionSO item, RPGEquipmentSlot equippedSlot)
        {
            List<RPGItemBuff> adjustedBuffs = new();

            for (int i = 0; i < item.itemBuffs.Count; i++)
            {
                RPGItemBuff originalBuff = item.itemBuffs[i];

                if (originalBuff == null)
                    continue;

                RPGStatType adjustedStatType = GetSlotAdjustedStatType(
                    originalBuff.statType,
                    equippedSlot
                );

                adjustedBuffs.Add(new RPGItemBuff(
                    adjustedStatType,
                    originalBuff.modifierType,
                    originalBuff.value
                ));
            }

            return adjustedBuffs;
        }

        private static RPGStatType GetSlotAdjustedStatType(RPGStatType statType, RPGEquipmentSlot equippedSlot)
        {
            if (equippedSlot == RPGEquipmentSlot.offHand && statType == RPGStatType.MainHandDamage)
                return RPGStatType.OffHandDamage;

            if (equippedSlot == RPGEquipmentSlot.mainHand && statType == RPGStatType.OffHandDamage)
                return RPGStatType.MainHandDamage;

            return statType;
        }

        public IReadOnlyList<RPGItemBuff> GetItemBuffs(RPGItemDefinitionSO item)
        {
            if (item == null)
                return Array.Empty<RPGItemBuff>();

            if (itemBuffsByItem.TryGetValue(item, out List<RPGItemBuff> buffs))
                return buffs;

            return Array.Empty<RPGItemBuff>();
        }

        public List<RPGItemBuff> GetAllActiveItemBuffs()
        {
            List<RPGItemBuff> allBuffs = new();

            foreach (KeyValuePair<RPGItemDefinitionSO, List<RPGItemBuff>> pair in itemBuffsByItem)
                allBuffs.AddRange(pair.Value);

            return allBuffs;
        }

        public bool HasItemBuffsFrom(RPGItemDefinitionSO item)
        {
            if (item == null)
                return false;

            return itemBuffsByItem.ContainsKey(item);
        }

        public void ClampResourceStats()
        {
            float hp = GetBaseStat(RPGStatType.HP);
            float maxHp = GetStat(RPGStatType.MaxHP);

            if (hp > maxHp)
                SetStat(RPGStatType.HP, maxHp);

            if (hp < 0)
                SetStat(RPGStatType.HP, 0);

            float stamina = GetBaseStat(RPGStatType.Stamina);
            float maxStamina = GetStat(RPGStatType.MaxStamina);

            if (stamina > maxStamina)
                SetStat(RPGStatType.Stamina, maxStamina);

            if (stamina < 0)
                SetStat(RPGStatType.Stamina, 0);
        }

        private void MarkDirty()
        {
            OnCharacterDirty?.Invoke(this);
        }

        public static RPGCharacterData InitializeAureliusStats()
        {
            RPGCharacterData character = new RPGCharacterData("Marcus Aurelius Decimus");

            character.Age = 42;
            character.Culture = RPGCharacterCulture.Roman;
            character.Allegiance = RPGCharacterAllegiance.Devoted;
            character.IsRested = true;
            character.darkSilhuetteIndex = 0;
            character.portraitIndex = 0;
            character.skillTreeVersion = 0;

            // Core stats, 5-25 range
            character.SetStat(RPGStatType.Strength, 16);
            character.SetStat(RPGStatType.Vitality, 12);
            character.SetStat(RPGStatType.Agility, 10);
            character.SetStat(RPGStatType.Valor, 14);
            character.SetStat(RPGStatType.Vigor, 10);
            character.mainAttribute = RPGStatType.Vitality;


            // Resources
            character.SetStat(RPGStatType.HP, 90);
            character.SetStat(RPGStatType.MaxHP, 90);
            character.SetStat(RPGStatType.Stamina, 140);
            character.SetStat(RPGStatType.MaxStamina, 140);

            // Progression
            character.SetStat(RPGStatType.Level, 15);
            character.SetStat(RPGStatType.Experience, 650);
            character.SetStat(RPGStatType.ExperienceToNextLevel, 1000);

            // Combat
            character.SetStat(RPGStatType.MainHandDamage, 28);
            character.SetStat(RPGStatType.OffHandDamage, 16);
            character.SetStat(RPGStatType.CriticalChance, 12);
            character.SetStat(RPGStatType.CriticalDamage, 3.5f);
            character.SetStat(RPGStatType.Accuracy, 78);
            character.SetStat(RPGStatType.AttackSpeed, 1.1f);
            character.SetStat(RPGStatType.FumbleChance, 9);
            character.SetStat(RPGStatType.ArmorPenetration, 1.1f);
            character.SetStat(RPGStatType.DodgeChance, 12);
            character.SetStat(RPGStatType.BlockChance, 0);
            character.SetStat(RPGStatType.MoveSpeed, 14);
            character.SetStat(RPGStatType.ParryChance, 7);

            // Defense
            character.SetStat(RPGStatType.Armor, 36);
            character.SetStat(RPGStatType.StatusEffectResistance, 15);
            character.SetStat(RPGStatType.SlashingResistance, 12);
            character.SetStat(RPGStatType.PiercingResistance, 33);
            character.SetStat(RPGStatType.CrushingResistance, 7);
            character.SetStat(RPGStatType.FireResistance, 16);

            return character;
        }

        public static RPGCharacterData InitializeRandomRomanCharacter(string name, int seed)
        {
            Random random = new Random(seed);

            RPGCharacterData character = new RPGCharacterData(name);

            character.Age = random.Next(22, 56);
            character.Culture = RPGCharacterCulture.Roman;
            character.Allegiance = RPGCharacterAllegiance.Loyal;
            character.IsRested = random.NextDouble() > 0.35;
            character.darkSilhuetteIndex = random.Next(1, 3);
            character.portraitIndex = random.Next(1, 15);
           

            int strength = random.Next(5, 26);
            int vitality = random.Next(5, 26);
            int agility = random.Next(5, 26);
            int valor = random.Next(5, 26);
            int vigor = random.Next(5, 26);

            int r = random.Next(0, 2);
            if (r == 0)
            {
                character.mainAttribute = RPGStatType.Strength;
            }
            else
            {
                character.mainAttribute = RPGStatType.Valor;
            }

            character.SetStat(RPGStatType.Strength, strength);
            character.SetStat(RPGStatType.Vitality, vitality);
            character.SetStat(RPGStatType.Agility, agility);
            character.SetStat(RPGStatType.Valor, valor);
            character.SetStat(RPGStatType.Vigor, vigor);

            int level = random.Next(3, 18);

            float maxHp = 50 + vitality * 4 + level * 2;
            float maxStamina = 50 + vigor * 4 + agility;

            character.SetStat(RPGStatType.HP, random.Next((int) (maxHp * 0.55f), (int) maxHp + 1));
            character.SetStat(RPGStatType.MaxHP, maxHp);
            character.SetStat(RPGStatType.Stamina, random.Next((int) (maxStamina * 0.55f), (int) maxStamina + 1));
            character.SetStat(RPGStatType.MaxStamina, maxStamina);

            character.SetStat(RPGStatType.Level, level);
            character.SetStat(RPGStatType.Experience, random.Next(0, 900));
            character.SetStat(RPGStatType.ExperienceToNextLevel, 1000);

            character.SetStat(RPGStatType.MainHandDamage, 8 + strength * 1.2f + level);
            character.SetStat(RPGStatType.OffHandDamage, 4 + strength * 0.65f);
            character.SetStat(RPGStatType.CriticalChance, 3 + agility * 0.6f);
            character.SetStat(RPGStatType.CriticalDamage, 1.5f + valor * 0.08f);
            character.SetStat(RPGStatType.Accuracy, 45 + agility * 1.4f + valor * 0.5f);
            character.SetStat(RPGStatType.AttackSpeed, 0.75f + agility * 0.025f);
            character.SetStat(RPGStatType.FumbleChance, Math.Max(1, 16 - agility * 0.45f));
            character.SetStat(RPGStatType.ArmorPenetration, 0.7f + strength * 0.025f);
            character.SetStat(RPGStatType.DodgeChance, 2 + agility * 0.55f);
            character.SetStat(RPGStatType.BlockChance, random.Next(0, 11));
            character.SetStat(RPGStatType.MoveSpeed, 8 + agility * 0.35f);
            character.SetStat(RPGStatType.ParryChance, 2 + valor * 0.35f);

            character.SetStat(RPGStatType.Armor, 10 + vitality + strength * 0.6f);
            character.SetStat(RPGStatType.StatusEffectResistance, 3 + vigor * 0.8f);
            character.SetStat(RPGStatType.SlashingResistance, random.Next(5, 21));
            character.SetStat(RPGStatType.PiercingResistance, random.Next(5, 26));
            character.SetStat(RPGStatType.CrushingResistance, random.Next(3, 18));
            character.SetStat(RPGStatType.FireResistance, random.Next(3, 20));

            character.ClampResourceStats();

            return character;
        }

        public static List<RPGCharacterData> CreateDemoParty()
        {
            List<RPGCharacterData> list = new List<RPGCharacterData>(3);
            Random random = new Random();
            list.Add(InitializeAureliusStats());
            list.Add(InitializeRandomRomanCharacter(GetDemoRomanName(random.Next(1,5)), random.Next(0,100)));
            list.Add(InitializeRandomRomanCharacter(GetDemoRomanName(random.Next(5,9)), random.Next(200,300)));
            
            

            return list;
        }
        
        
        private static readonly string[] DemoRomanNames =
        {
            "Marcus Aurelius Decimus",
            "Lucius Varro Castus",
            "Titus Flavius Corvus",
            "Gaius Aelius Marcellus",
            "Publius Cornelius Drusus",
            "Quintus Valerius Maximus",
            "Aulus Cassius Severus",
            "Sextus Julius Falco",
            "Decimus Antonius Rufus",
            "Tiberius Claudius Varian"
        };

        private static string GetDemoRomanName(int index)
        {
            if (DemoRomanNames == null || DemoRomanNames.Length == 0)
                return "Unnamed Character";

            return DemoRomanNames[index % DemoRomanNames.Length];
        }
    }
}
