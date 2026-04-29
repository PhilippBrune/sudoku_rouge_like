# Harmony Difficulty System

**Version:** 1.0 | **Date:** 2026-04-27 | **Status:** Implemented

---

## 1. Overview

The Harmony Difficulty System extends the **Garden Run** game mode with eleven selectable
difficulty tiers, numbered **H0 (Garden Path)** through **H10 (Void Garden)**. Each tier
increases puzzle complexity, economic pressure, and mechanical constraints, while rewarding
the player with a scaled XP multiplier and exclusive relics.

All scaling parameters are centralised in `HarmonyDifficultyService` (`Assets/Scripts/Run/`).
No other service contains per-level conditionals — every other service receives a pre-built
`HarmonyConfig` struct at run start.

---

## 2. Requirement Identifiers

| Prefix | Domain |
|---|---|
| `HARMONY-001..004` | Core run-state fields |
| `HARMONY-BOSS-001..002` | Boss modifier scaling |
| `HARMONY-ECON-001..003` | Economy penalties |
| `HARMONY-SURV-001..005` | Survivability constraints |
| `HARMONY-ITEM-001` | Item reward pressure |
| `HARMONY-RNG-001` | Positive-effect suppression |
| `HARMONY-FLAG-001..005` | Boolean mechanic flags |
| `HARMONY-XP-001` | XP multiplier |
| `HARMONY-UNLOCK-001` | Sequential unlock gate |
| `HARMONY-PERK-001..002` | Pre-run perks |
| `HARMONY-UI-001..002` | UI components |
| `HARMONY-ACHIEVE-001` | Steam achievements |
| `HARMONY-SAVE-001` | Save / sanitisation |
| `HARMONY-BADGE-001` | Per-level badge flags |
| `HARMONY-DISPLAY-001` | Display name / HUD label |
| `HARMONY-CONFIG-001` | Single-source config |

---

## 3. Level Names and Labels

| Level | Display Name | HUD Label |
|---|---|---|
| 0 | Garden Path | H0 |
| 1 | Harmony I | HI |
| 2 | Harmony II | HII |
| 3 | Harmony III | HIII |
| 4 | Harmony IV | HIV |
| 5 | Harmony V | HV |
| 6 | Harmony VI | HVI |
| 7 | Harmony VII | HVII |
| 8 | Harmony VIII | HVIII |
| 9 | Harmony IX | HIX |
| 10 | Void Garden | VG |

---

## 4. Scaling Tables

### 4.1 Economy

| Level | Gold Mult | Shop Surcharge | Reroll Penalty |
|---|---|---|---|
| H0 | ×1.00 | ×1.00 | 0 |
| H1 | ×0.96 | ×1.05 | 0 |
| H2 | ×0.92 | ×1.10 | 0 |
| H3 | ×0.88 | ×1.15 | 0 |
| H4 | ×0.84 | ×1.20 | 0 |
| H5 | ×0.80 | ×1.25 | −1 |
| H6 | ×0.76 | ×1.30 | −1 |
| H7 | ×0.72 | ×1.35 | −1 |
| H8 | ×0.68 | ×1.40 | −2 |
| H9 | ×0.64 | ×1.45 | −2 |
| H10 | ×0.60 | ×1.50 | −2 |

### 4.2 Survivability

| Level | HP Penalty | Mistake HP Cost | Combo Decay | Pencil Penalty | Item-Slot Penalty |
|---|---|---|---|---|---|
| H0 | 0 | 1 | ✗ | 0 | 0 |
| H1 | 0 | 1 | ✗ | 0 | 0 |
| H2 | 0 | 1 | ✗ | 0 | 0 |
| H3 | −1 | 1 | ✗ | 0 | 0 |
| H4 | −1 | 1 | ✗ | −1 | 0 |
| H5 | −1 | 2 | ✗ | −1 | 0 |
| H6 | −2 | 2 | ✗ | −1 | −1 |
| H7 | −2 | 2 | ✓ | −2 | −1 |
| H8 | −2 | 2 | ✓ | −2 | −2 |
| H9 | −3 | 3 | ✓ | −2 | −2 |
| H10 | −3 | 3 | ✓ | −3 | −2 |

Floor minimums: HP ≥ 3, Pencil ≥ 3, Item Slots ≥ 1.

### 4.3 Mechanic Flags

| Level | Hint Masking | T1-Only Start Relic | Mistakes Drain Pencil | Rest Heal Halved | Cursed Bonus Disabled |
|---|---|---|---|---|---|
| H0–H4 | ✗ | ✗ | ✗ | ✗ | ✗ |
| H5–H6 | ✓ | ✗ | ✗ | ✗ | ✗ |
| H7–H8 | ✓ | ✓ | ✓ | ✗ | ✗ |
| H9 | ✓ | ✓ | ✓ | ✗ | ✓ |
| H10 | ✓ | ✓ | ✓ | ✓ | ✓ |

### 4.4 Boss Modifiers

| Level | Floor Modifier Bonus (base) | Boss Options Bonus |
|---|---|---|
| H0–H2 | 0 | 0 |
| H3–H5 | +1 on floors 2–4 only | 0 (H3-H4), +1 (H5) |
| H6–H8 | +1 all floors | +1 (H6), +2 (H7-H8) |
| H9–H10 | +2 all floors | +3 |

### 4.5 Grid Size Weights (5×5 … 9×9)

| Level | 5×5 | 6×6 | 7×7 | 8×8 | 9×9 |
|---|---|---|---|---|---|
| H0 | 30 | 25 | 20 | 15 | 10 |
| H1 | 25 | 25 | 22 | 17 | 11 |
| H2 | 20 | 22 | 23 | 20 | 15 |
| H3 | 15 | 20 | 22 | 23 | 20 |
| H4 | 10 | 18 | 22 | 25 | 25 |
| H5 | 5 | 15 | 20 | 27 | 33 |
| H6 | 3 | 12 | 18 | 27 | 40 |
| H7 | 0 | 10 | 17 | 28 | 45 |
| H8 | 0 | 5 | 15 | 27 | 53 |
| H9 | 0 | 0 | 12 | 28 | 60 |
| H10 | 0 | 0 | 0 | 25 | 75 |

### 4.6 Star Difficulty Weights (1★ … 6★)

| Level | 1★ | 2★ | 3★ | 4★ | 5★ | 6★ |
|---|---|---|---|---|---|---|
| H0 | 40 | 30 | 20 | 10 | 0 | 0 |
| H1 | 35 | 30 | 23 | 12 | 0 | 0 |
| H2 | 30 | 30 | 25 | 15 | 0 | 0 |
| H3 | 25 | 28 | 25 | 17 | 5 | 0 |
| H4 | 20 | 25 | 25 | 20 | 10 | 0 |
| H5 | 15 | 22 | 25 | 23 | 12 | 3 |
| H6 | 10 | 20 | 25 | 25 | 15 | 5 |
| H7 | 8 | 18 | 22 | 25 | 20 | 7 |
| H8 | 5 | 15 | 20 | 25 | 25 | 10 |
| H9 | 3 | 10 | 18 | 25 | 30 | 14 |
| H10 | 0 | 5 | 15 | 25 | 35 | 20 |

### 4.7 Reward Pressure

| Level | Nothing Chance Bonus | Positive Effect Mult | XP Multiplier |
|---|---|---|---|
| H0 | +0.0% | ×1.00 | ×1.0 |
| H1 | +1.5% | ×0.93 | ×1.1 |
| H2 | +3.0% | ×0.86 | ×1.2 |
| H3 | +4.5% | ×0.79 | ×1.3 |
| H4 | +6.0% | ×0.72 | ×1.4 |
| H5 | +7.5% | ×0.65 | ×1.5 |
| H6 | +9.0% | ×0.58 | ×1.6 |
| H7 | +10.5% | ×0.51 | ×1.7 |
| H8 | +12.0% | ×0.44 | ×1.8 |
| H9 | +13.5% | ×0.37 | ×1.9 |
| H10 | +15.0% | ×0.30 | ×2.0 |

---

## 5. Harmony-Gated Relics

New relics are added to the appropriate tier pool only when `harmonyLevel >= gate`.

| RelicId | Tier | Gate | Effect |
|---|---|---|---|
| `CrackedStone` | T1 | H2+ | Each mistake after the first in a puzzle costs 0 HP (stone absorbs). Passive. |
| `FrostedMirror` | T2 | H5+ | Once per mistake: spend 3 pencil to absorb 1 HP of damage. Auto-triggers. |
| `VoidPetal` | T3 | H7+ | Once per run: survive a lethal hit at 1 HP. Grants 1 free Normal Solver on activation. |
| `LanternOfVoid` | Legendary | H9+ | Boss modifier labels permanently hidden. +1 gold per active floor modifier on puzzle complete. |

These relics are **excluded** from the `AllRelicsDiscovered` completion tracker (which uses a
plain boolean flag rather than recalculating from individual discoveries).

---

## 6. Harmony-Gated Item

| ItemType | Gate | Effect |
|---|---|---|
| `BambooCompass` | H3+ | Highlights 2 cells where the next placement would violate a constraint. |

---

## 7. Pre-Run Perks

Perks are optional modifiers chosen before a run, each unlocked at a specific Harmony level.
Only one perk may be active per run.

| HarmonyPerkId | Gate | Effect |
|---|---|---|
| `MoonshadeOffering` | H4+ | +20 starting gold, +2 starting pencil. First puzzle's reward slot 1 forced to Nothing. |
| `ScholarsBurden` | H6+ | Max item slots −1 (beyond the H6 penalty), +5 starting pencil, gain 1 free Rare InkWell. |
| `VoidWard` | H8+ | All positive floor effects disabled. Start with 1 mistake-shield charge. |
| `EmptyCanvas` | H10 | All starting items stripped. Item reward slots guaranteed Rare+. |

---

## 8. Unlock Logic

`[HARMONY-UNLOCK-001]`

- Harmony levels are unlocked **sequentially** — no skipping.
- Unlocking H(N+1) requires: **Victory** at H(N) in Garden Run **with ≥ 1 boss defeated**.
- `MaxUnlockedHarmonyLevel` is stored in `MetaProgressionState` and **never decremented**.
- `LastSelectedHarmonyLevel` is clamped to `MaxUnlockedHarmonyLevel` on save load.
- Unlocks are **global** (not per-class).

---

## 9. Achievements

| ID | Steam Key | Name | Condition | Tier |
|---|---|---|---|---|
| 53 | `harmony_1_win` | First Harmony | Win GardenRun at H1+ | Intermediate |
| 54 | `harmony_3_win` | Deep Roots | Win GardenRun at H3+ | Intermediate |
| 55 | `harmony_5_win` | Halfway to Void | Win GardenRun at H5+ | Advanced |
| 56 | `harmony_7_win` | Beyond the Garden | Win GardenRun at H7+ | Advanced |
| 57 | `harmony_9_win` | Borderless Garden | Win GardenRun at H9+ | Expert |
| 58 | `harmony_10_win` | Void Garden | Win GardenRun at H10 | Expert |
| 59 | `harmony_10_perfect` | Void Perfection | Win H10 with 0 mistakes | Hidden |
| 60 | `harmony_all_classes` | Master of Harmony | Win H5+ with all 8 classes | Hidden |

Tracking storage: `MetaProgressionState.HarmonyV5PlusWins` (`List<ClassId>`) records each class
that has won at H5+. The `harmony_all_classes` check fires when the list reaches 8 entries.

---

## 10. Save Model Changes

**`MetaProgressionState`** (in `SaveModels.cs`):

```csharp
public int MaxUnlockedHarmonyLevel;       // 0–10, never decremented
public int LastSelectedHarmonyLevel;      // clamped to MaxUnlocked on load
public List<int> HarmonyBadgeFlags;       // bit0=perfect, bit1=speed per level
public List<ClassId> HarmonyV5PlusWins;  // for harmony_all_classes achievement
```

**Sanitisation** (in `SaveFileService.SanitizeEnvelope`):
```csharp
meta.MaxUnlockedHarmonyLevel  = Math.Clamp(meta.MaxUnlockedHarmonyLevel, 0, 10);
meta.LastSelectedHarmonyLevel = Math.Clamp(meta.LastSelectedHarmonyLevel, 0, meta.MaxUnlockedHarmonyLevel);
if (meta.HarmonyBadgeFlags == null) meta.HarmonyBadgeFlags = new List<int>();
```

---

## 11. UI Specification

### 11.1 Game Modes Panel

`GameModesPanelController` exposes:
- `SelectedHarmonyLevel` (int, 0–MaxUnlocked)
- `SelectedHarmonyPerk` (HarmonyPerkId)
- `SetHarmonyLevel(int level, int maxUnlocked)`
- `SetHarmonyPerk(int perkIndex)`

### 11.2 Harmony Node Row

`HarmonyNodeRowController` is a `MonoBehaviour` attached to a horizontal row of 11 nodes.

- `BindMeta(MetaProgressionState)` — initialise from save data
- `SetSelectedLevel(int)` — select without firing event
- `Refresh()` — rebuild all node states
- `event Action<int> OnNodeSelected` — fired on player tap

Node states: **Locked** (level > MaxUnlocked), **Unlocked**, **Selected**.

The summary card beneath the row shows:
- Display name, HUD label, XP multiplier from `HarmonyDifficultyService`.

---

## 12. Implementation File Map

| File | Change |
|---|---|
| `Assets/Scripts/Core/GameEnums.cs` | Added `BambooCompass`, `CrackedStone`, `FrostedMirror`, `VoidPetal`, `LanternOfVoid`, `HarmonyPerkId` enum |
| `Assets/Scripts/Core/SaveModels.cs` | Added 4 fields to `MetaProgressionState` |
| `Assets/Scripts/Core/RuntimeModels.cs` | Added harmony fields to `LaunchRequest`, `RunState`, `RunResult`; perk flags |
| `Assets/Scripts/Run/HarmonyDifficultyService.cs` | **NEW** — central difficulty authority |
| `Assets/Scripts/Run/RunArchetypeService.cs` | Apply `HarmonyConfig` to `CreateRunState()`; perk effects |
| `Assets/Scripts/Boss/BossService.cs` | `harmonyBonus` params on modifier count / roll methods |
| `Assets/Scripts/Economy/ShopService.cs` | `harmonySurcharge` param on `BuildOffers()` |
| `Assets/Scripts/Economy/RelicService.cs` | 4 new relics, harmony pool gating, `LanternOfVoid` in `OnPuzzleComplete`, relic helpers |
| `Assets/Scripts/Save/ProfileService.cs` | Harmony unlock step, XP multiplier, achievement call |
| `Assets/Scripts/Save/SaveFileService.cs` | Sanitisation clamps |
| `Assets/Scripts/Meta/SteamAchievementService.cs` | **NEW** — 8 harmony achievements |
| `Assets/Scripts/UI/GameModesPanelController.cs` | Harmony selection properties + helpers |
| `Assets/Scripts/UI/HarmonyNodeRowController.cs` | **NEW** — node chain UI |
| `docs/HarmonyDifficultySystem.md` | **THIS FILE** |
