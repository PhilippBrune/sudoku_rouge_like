using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;

namespace SudokuRoguelike.Run
{
    public static class RunArchetypeService
    {
        public static RunState CreateRunState(ClassId classId, int seed, bool allowIrregular = true)
        {
            var def = ClassCatalog.GetDefinition(classId);

            return new RunState
            {
                ClassId = classId,
                Mode = GameMode.GardenRun,
                Seed = seed,
                RunNumber = 1,
                Depth = 0,
                CurrentFloor = 0,
                TotalFloors = 5,
                CurrentHP = def.BaseHP,
                MaxHP = def.BaseHP,
                CurrentPencil = def.BasePencil,
                MaxPencil = def.BasePencil,
                CurrentGold = 0,
                ItemSlots = def.BaseItemSlots,
                AllowIrregularPuzzles = allowIrregular
            };
        }
    }
}
