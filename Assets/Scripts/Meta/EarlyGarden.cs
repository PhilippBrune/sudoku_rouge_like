using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Meta
{
    /// <summary>
    /// [REQ: RUN-EARLY-001] [REQ: RUN-EARLY-002] Something new in each of the first ten runs (RM-118, owner's pick
    /// "behutsam öffnen", 2026-11-03): a new profile's Garden Runs hold back four relics, three events and three boss
    /// rules, and every finished run opens the next one in this order. Profiles with ten or more runs have everything. The
    /// Daily Walk is the same for every player that day and holds nothing back.
    /// </summary>
    public static class EarlyGarden
    {
        public enum Kind { Relic, Event, Rule }

        public readonly struct Entry
        {
            public readonly Kind Kind;
            public readonly string Id;   // the RelicId or BossModifierId name, or the event's id
            public Entry(Kind kind, string id) { Kind = kind; Id = id; }
        }

        /// <summary>The order things open in: run 1 opens the first.</summary>
        public static readonly IReadOnlyList<Entry> Entries = new[]
        {
            new Entry(Kind.Relic, nameof(RelicId.PhoenixFeather)),
            new Entry(Kind.Event, "wishing_fountain"),
            new Entry(Kind.Rule, nameof(BossModifierId.Skyscrapers)),
            new Entry(Kind.Relic, nameof(RelicId.ShiftingGarden)),
            new Entry(Kind.Event, "rare_flower_patch"),
            new Entry(Kind.Rule, nameof(BossModifierId.Sandwich)),
            new Entry(Kind.Relic, nameof(RelicId.EternalLotus)),
            new Entry(Kind.Event, "tempting_shrine"),
            new Entry(Kind.Rule, nameof(BossModifierId.LittleKiller)),
            new Entry(Kind.Relic, nameof(RelicId.DragonsEye)),
        };

        public static int Count => Entries.Count;

        /// <summary>How many have opened after this many finished runs.</summary>
        public static int Opened(int finishedRuns) => Math.Clamp(finishedRuns, 0, Count);

        /// <summary>True when the run holds this thing back: a Garden Run (not the Daily Walk) that has not opened it.</summary>
        public static bool IsHeld(RunState state, Kind kind, string id)
        {
            if (state == null || state.Mode != GameMode.GardenRun || state.DailyWalk || state.GardenOpened >= Count) return false;
            for (var i = state.GardenOpened; i < Count; i++)
                if (Entries[i].Kind == kind && Entries[i].Id == id) return true;
            return false;
        }

        /// <summary>The entries that opened since the player last saw the run end's "New" column: from
        /// <paramref name="shown"/> up to <paramref name="opened"/>.</summary>
        public static List<Entry> NewSince(int shown, int opened)
        {
            var list = new List<Entry>();
            for (var i = Math.Max(0, shown); i < Math.Min(opened, Count); i++) list.Add(Entries[i]);
            return list;
        }

        public static string Name(Entry entry) => entry.Kind switch
        {
            Kind.Relic => RelicService.GetRelicName(Enum.Parse<RelicId>(entry.Id)),
            Kind.Rule => BossService.GetModifierName(Enum.Parse<BossModifierId>(entry.Id)),
            _ => LocalizationService.T($"RunEvent.{entry.Id}.Title", entry.Id)
        };

        public static string IconPath(Entry entry)
        {
            switch (entry.Kind)
            {
                case Kind.Relic:
                    var relic = Enum.Parse<RelicId>(entry.Id);
                    return RelicService.GetIconFolder(relic) + "/icon_" + RelicService.GetIconName(relic);
                case Kind.Rule:
                    return "modifier/icon_" + BossService.GetIconName(Enum.Parse<BossModifierId>(entry.Id));
                default:
                    return "node/icon_event_scroll";
            }
        }

        /// <summary>The plaque's caption: what kind of thing joined the garden.</summary>
        public static string Caption(Entry entry) => entry.Kind switch
        {
            Kind.Relic => LocalizationService.T("EarlyGarden.Relic", "A relic joins the garden"),
            Kind.Rule => LocalizationService.T("EarlyGarden.Rule", "A rule for the bosses"),
            _ => LocalizationService.T("EarlyGarden.Event", "An event on the path")
        };
    }
}
