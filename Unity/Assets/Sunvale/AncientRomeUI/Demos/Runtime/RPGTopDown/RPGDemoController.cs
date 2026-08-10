using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.UI;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public class RPGDemoController : MonoBehaviour
    {
        
        public DemoRPGCharacterDetailsWindowController characterDetailsWindow;
        public RPGItemLibrarySO itemLibSO;

        [NonSerialized] public List<RPGCharacterData> party;
        [NonSerialized] public RPGSharedInventory globalInventory;
        public DemoSpriteCollection spriteCollection;

        public List<DemoRPGCharacterSideCardView> sideCards;

        public SimpleButton healButton;
        public SimpleButton damageButton;
        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            globalInventory = new RPGSharedInventory(itemLibSO.allItems);
            CreateThreeCharacters();
            characterDetailsWindow.InitializeForCharacter(party[0]);

            healButton.OnButtonActivatedClicked += HealButtonPressed;
            damageButton.OnButtonActivatedClicked += DamageButtonpPessed;

            for (var i = 0; i < sideCards.Count; i++)
            {
                var sideCard = sideCards[i];
                sideCard.BindToCharacter(party[i]);
                sideCard.OnCardActivatedClicked -= CharPanelClicked;
                sideCard.OnCardActivatedClicked += CharPanelClicked;
            }
        }

        private void CreateThreeCharacters()
        {
            party = RPGCharacterData.CreateDemoParty();

            if (spriteCollection != null && spriteCollection.rpgPortraits != null && spriteCollection.rpgPortraits.Count > 0)
            {
                party[0].portraitSprite = spriteCollection.rpgPortraits[party[0].portraitIndex % spriteCollection.rpgPortraits.Count];
                party[1].portraitSprite = spriteCollection.rpgPortraits[party[1].portraitIndex % spriteCollection.rpgPortraits.Count];
                party[2].portraitSprite = spriteCollection.rpgPortraits[party[2].portraitIndex % spriteCollection.rpgPortraits.Count];
            }

            party[0].skillTreeVersion = 0;
            party[1].skillTreeVersion = 1;
            party[2].skillTreeVersion = 2;

            party[0].skillTreePoints = 4;
            party[1].skillTreePoints = 4;
            party[2].skillTreePoints = 5;
        }

        public void CharPanelClicked(DemoRPGCharacterSideCardView card)
        {
            int index = sideCards.IndexOf(card);
            characterDetailsWindow.InitializeForCharacter(party[index]);
        }

        public void PublishGlobalBuff(RPGSkillButton sourceButton, float duration)
        {
            if (sourceButton == null)
                return;

            if (sideCards == null)
                return;

            float safeDuration = Mathf.Max(0.01f, duration);

            for (int i = 0; i < sideCards.Count; i++)
            {
                DemoRPGCharacterSideCardView sideCard = sideCards[i];

                if (sideCard == null)
                    continue;

                sideCard.PublishGlobalBuff(sourceButton, safeDuration);
            }
        }

        public void DamageParty()
        {
            if (party == null)
                return;

            for (var i = 0; i < party.Count; i++)
            {
                RPGCharacterData character = party[i];

                if (character == null)
                    continue;

                int hpDamage = UnityEngine.Random.Range(15, 31);
                int staminaDamage = UnityEngine.Random.Range(15, 31);

                character.AddToStat(RPGStatType.HP, -hpDamage);
                character.AddToStat(RPGStatType.Stamina, -staminaDamage);

                character.ClampResourceStats();
            }
        }

        public void HealParty()
        {
            if (party == null)
                return;

            for (var i = 0; i < party.Count; i++)
            {
                RPGCharacterData character = party[i];

                if (character == null)
                    continue;

                character.AddToStat(RPGStatType.HP, 20);
                character.AddToStat(RPGStatType.Stamina, 20);

                character.ClampResourceStats();
            }
        }

        private void HealButtonPressed(SimpleButton theButton)
        {
            HealParty();
        }

        private void DamageButtonpPessed(SimpleButton theButton)
        {
            DamageParty();
        }
    }
}
