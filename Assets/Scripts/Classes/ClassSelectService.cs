using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Classes
{
    public sealed class ClassSelectCard
    {
        public ClassId ClassId;
        public string Name;
        public int HP;
        public int Pencil;
        public int ItemSlots;
        public bool IsUnlocked;
        public bool IsGreyedOut;
        public string UnlockRequirement;
        public string PassiveDisplay;
        public int Tier;
        public ClassComplexity Complexity;
        public PlayerSkillBand SkillBand;
    }

    public sealed class ClassSelectService
    {
        private readonly ClassUnlockService _unlockService = new();

        public List<ClassSelectCard> BuildCards(MetaProgressionState meta)
        {
            var order = new[]
            {
                ClassId.NumberFreak,
                ClassId.GardenMonk,
                ClassId.ShrineArchivist,
                ClassId.KoiGambler,
                ClassId.StoneGardener,
                ClassId.LanternSeer,
                ClassId.ZenMaster
            };

            var cards = new List<ClassSelectCard>(order.Length);
            for (var i = 0; i < order.Length; i++)
            {
                var snapshot = ClassCatalog.Build(order[i]);
                var unlocked = meta.UnlockedClasses.Contains(order[i]);
                var metaInfo = ClassCatalog.GetMeta(order[i]);

                cards.Add(new ClassSelectCard
                {
                    ClassId = order[i],
                    Name = order[i].ToString(),
                    HP = snapshot.HP,
                    Pencil = snapshot.Pencil,
                    ItemSlots = snapshot.ItemSlots,
                    IsUnlocked = unlocked,
                    IsGreyedOut = !unlocked,
                    UnlockRequirement = unlocked ? string.Empty : _unlockService.GetUnlockRequirementText(order[i]),
                    PassiveDisplay = unlocked ? metaInfo.PassiveDescription : "???",
                    Tier = metaInfo.Tier,
                    Complexity = metaInfo.Complexity,
                    SkillBand = metaInfo.SkillBand
                });
            }

            return cards;
        }
    }
}
