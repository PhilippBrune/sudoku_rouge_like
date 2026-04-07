using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Classes
{
    public sealed class ClassSelectService
    {
        public List<ClassId> GetSelectableClasses(MetaProgressionState meta)
        {
            var result = new List<ClassId>();
            if (meta?.UnlockedClasses == null) return result;

            for (var i = 0; i < meta.UnlockedClasses.Count; i++)
                result.Add(meta.UnlockedClasses[i]);

            return result;
        }

        public int GetClassLevel(ClassId classId, MetaProgressionState meta)
        {
            if (meta?.GardenProgression?.ClassEntries == null) return 1;

            for (var i = 0; i < meta.GardenProgression.ClassEntries.Count; i++)
            {
                var entry = meta.GardenProgression.ClassEntries[i];
                if (entry.ClassId == classId)
                    return Economy.XpTable.DeriveLevel(entry.TotalXp);
            }

            return 1;
        }
    }
}
