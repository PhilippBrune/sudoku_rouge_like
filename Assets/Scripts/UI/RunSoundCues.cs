using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: AUDIO-SFX-001] The run's sounds whose event has many causes (GAP-073 round 2, owner 2026-11-04): compared once a
    /// frame against the run state, so every item, relic, mistake or gold source sounds without a call in each place.
    /// The first look at a run (a new run or a loaded save) only records it.
    /// </summary>
    public sealed class RunSoundCues
    {
        public enum Cue { ItemUse, RelicPickup, PencilDepleted, HpCritical, Combo, ComboBreak, GoldGain, FogReveal, FloorTransition }

        public struct Snapshot
        {
            public RunState State;
            public ModifierOverlayData Overlay;
            public int ItemsUsed, Relics, Pencil, Hp, MaxHp, Combo, Gold, Floor, Fog;

            public static Snapshot Of(RunState s, ModifierOverlayData overlay) => new Snapshot
            {
                State = s, Overlay = overlay, ItemsUsed = s.ItemsUsedThisRun, Relics = s.HeldRelics?.Count ?? 0,
                Pencil = s.CurrentPencil, Hp = s.CurrentHP, MaxHp = s.MaxHP, Combo = s.ComboStreak, Gold = s.CurrentGold,
                Floor = s.CurrentFloor, Fog = overlay?.FogCells?.Count ?? 0
            };
        }

        private Snapshot? _last;
        private readonly List<Cue> _cues = new List<Cue>();   // reused: Tick runs every frame

        /// <summary>The cues since the last call (valid until the next call); empty on the first look at a run.</summary>
        public List<Cue> Tick(RunState state, ModifierOverlayData overlay)
        {
            _cues.Clear();
            if (state == null) { _last = null; return _cues; }
            var now = Snapshot.Of(state, overlay);
            if (_last.HasValue && _last.Value.State == state) Compare(_last.Value, now, _cues);
            _last = now;
            return _cues;
        }

        public static List<Cue> Compare(Snapshot before, Snapshot after, List<Cue> cues = null)
        {
            cues ??= new List<Cue>();
            if (after.ItemsUsed > before.ItemsUsed) cues.Add(Cue.ItemUse);
            if (after.Relics > before.Relics) cues.Add(Cue.RelicPickup);
            if (before.Pencil > 0 && after.Pencil <= 0) cues.Add(Cue.PencilDepleted);
            // A quarter of the HP or less, crossed downwards; the end of the run has its own sound.
            if (after.Hp > 0 && after.Hp * 4 <= after.MaxHp && before.Hp * 4 > before.MaxHp) cues.Add(Cue.HpCritical);
            if (after.Combo > before.Combo && after.Combo % 5 == 0) cues.Add(Cue.Combo);
            if (before.Combo >= 5 && after.Combo == 0) cues.Add(Cue.ComboBreak);
            if (after.Gold > before.Gold) cues.Add(Cue.GoldGain);
            if (after.Overlay == before.Overlay && after.Fog < before.Fog) cues.Add(Cue.FogReveal);
            if (after.Floor > before.Floor) cues.Add(Cue.FloorTransition);
            return cues;
        }

        public static void Play(RunAudioController audio, IReadOnlyList<Cue> cues, int combo)
        {
            if (audio == null || cues.Count == 0) return;
            for (var i = 0; i < cues.Count; i++)
            {
                var cue = cues[i];
                // A mistake already sounds (wrong digit, HP loss): one more cue at most, the critical HP first.
                if (cue == Cue.ComboBreak && Contains(cues, Cue.HpCritical)) continue;
                switch (cue)
                {
                    case Cue.ItemUse: audio.PlayItemUse(); break;
                    case Cue.RelicPickup: audio.PlayRelicPickup(); break;
                    case Cue.PencilDepleted: audio.PlayPencilDepleted(); break;
                    case Cue.HpCritical: audio.PlayHpCritical(); break;
                    case Cue.Combo: audio.PlayComboMilestone(combo); break;
                    case Cue.ComboBreak: audio.PlayComboBreak(); break;
                    case Cue.GoldGain: audio.PlayGoldGain(); break;
                    case Cue.FogReveal: audio.PlayFogReveal(); break;
                    default: audio.PlayFloorTransition(); break;
                }
            }
        }

        private static bool Contains(IReadOnlyList<Cue> cues, Cue cue)
        {
            for (var i = 0; i < cues.Count; i++) if (cues[i] == cue) return true;
            return false;
        }
    }
}
