using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Data
{
    [CreateAssetMenu(menuName = "SudokuRoguelike/Class Definition")]
    public sealed class ClassDefinition : ScriptableObject
    {
        public ClassId Id;
        public string DisplayName;
        public bool Playable;
        public int BaseHP;
        public int BasePencil;
        public int BaseItemSlots;
        public int BaseRerollTokens;
        public bool CanBuyPencilMidLevel = true;
        public string PassiveDescription;
    }

    [CreateAssetMenu(menuName = "SudokuRoguelike/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string Id;
        public ItemType Type;
        public ItemRarity Rarity;
        public int Charges = 1;
        public bool IsConsumable = true;
        public int ValueA;
        public int ValueB;
    }

    [CreateAssetMenu(menuName = "SudokuRoguelike/Modifier Definition")]
    public sealed class ModifierDefinition : ScriptableObject
    {
        public BossModifierId Id;
        public BossModifierTier Tier;
        [Range(0f, 2f)] public float DifficultyImpact;
        public bool IsEpicEligible;
        public int MinRun;
    }

    [CreateAssetMenu(menuName = "SudokuRoguelike/Progression Table")]
    public sealed class ProgressionTable : ScriptableObject
    {
        public List<StarEntry> Stars = new();

        [Serializable]
        public sealed class StarEntry
        {
            [Range(1, 12)] public int Star;
            [Range(0.01f, 0.95f)] public float MissingPercent;
        }

        public float GetMissingPercent(int star)
        {
            foreach (var entry in Stars)
            {
                if (entry.Star == star)
                {
                    return entry.MissingPercent;
                }
            }

            return StarDensityService.MissingPercentForStars(star);
        }
    }
}
