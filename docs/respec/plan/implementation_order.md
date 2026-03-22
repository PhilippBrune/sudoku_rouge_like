# Implementation Plan — Remaining Specs

## Priority Order

| # | Spec | Gaps | Why This Order |
|:---:|------|:---:|----------------|
| 1 | GoldEconomySystem | 3 | Currently WRONG: relics sold in shops (spec says no), Nothing slot gives gold (spec says no), shop size is fixed (spec says floor-scaled). Bugs affect every run. |
| 2 | MetaProgressionSystem | 4 | CompletionService uses proxy checks instead of real conditions. 43/52 achievements missing. Codex-unlock integration incomplete. Foundational for all progression. |
| 3 | SpiritTrialsMode | 6 | Scoring formula is wrong (multiplicative vs additive). No tier system. No personal bests. Full archive+rewrite. Depends on Gold fix and Meta fix. |
| 4 | BossMechanicsSystem | 3 | Legend panel with tap-to-isolate missing. Geometry spatial separation missing. UX features for multi-modifier readability. |
| 5 | SudokuGenerationPipeline | 2 | Uniqueness checker (OPEN-A) high priority for Spirit Trials. Template expansion (OPEN-B) low priority. |
| 6 | TutorialModeSystem | 1 | 7-star support gap is minor (spec self-contradicts on modifier requirement). |
| 7 | SaveLoadArchitecture | 1 | Steam Cloud sync — platform integration, not gameplay. Can ship without it. |
| 8 | GameDesignSpec | 3 | Master overview doc. Multi-select controls, Endless Zen modifier pool, pencil consumption. Updated last. |

---

## Step 1 — GoldEconomySystem (3 fixes)

**Archive & rewrite:** `ShopService.cs`

| Task | Change |
|------|--------|
| Remove relic sales from shops | Delete relic offer logic from `ShopService.BuildOffers()` |
| Floor-based shop inventory size | Floor 1-2 = 2 items, Floor 3-4 = 3 items, Floor 5 = 4 items |
| Nothing slot grants zero gold | Remove `NothingGoldBonus` from `ItemService` and gold grant from `RunDirector.PickRolledSlot()` |

**Files:** `ShopService.cs`, `ItemService.cs`, `RunDirector.cs`

---

## Step 2 — MetaProgressionSystem (4 fixes)

**Archive & rewrite:** `CompletionService.cs`, `SteamAchievementService.cs`

| Task | Change |
|------|--------|
| Fix CompletionService checks | Replace proxy conditions with real validation: 30 board-star combos tracked, 8 classes at L30+, all relics discovered |
| Add 43 missing achievements | Implement full 52-achievement table with correct IDs, names, and evaluation conditions |
| Wire codex to Reed Duelist unlock | Evaluate actual codex completion count, not bool flag |
| Verify Quiet Cartographer flag | Ensure `ClearedStageNoPencilNoHpLoss` is SET in `RunDirector` when conditions are met |

**Files:** `CompletionService.cs`, `SteamAchievementService.cs`, `ClassUnlockService.cs`, `RuntimeModels.cs`, `ProfileService.cs`

---

## Step 3 — SpiritTrialsMode (6 fixes — full rewrite)

**Archive & rewrite:** `SpiritTrialsService.cs`

| Task | Change |
|------|--------|
| Add `SpiritTrialsTier` enum | Apprentice, Adept, Master, Grandmaster in `GameEnums.cs` |
| Per-tier config table | HP, Pencil, Items, Stars, ModCount, ParTime, PointsPerCell, PenaltyPerMistake |
| Correct scoring formula | `floor((Base * SpeedMult) + ConstraintBonus + PencilBonus - MistakePenalty)` |
| Correct SpeedMultiplier | `max(0.5, 2.0 - elapsed / parTime)` per-tier par times |
| Personal best tracking | Add per-tier fields to `ProfileStats` |
| Spirit Trials UI | Tier selection in Game Modes menu, results screen with score breakdown |

**Files:** `SpiritTrialsService.cs`, `GameEnums.cs`, `RuntimeModels.cs`, `ProfileService.cs`, `MainMenuController.cs`, `MainMenuBlueprintBuilder.cs`, `PrototypeRunScreenController.cs`, `GameBootstrap.cs`

---

## Step 4 — BossMechanicsSystem (3 features)

**Extend existing files** (no full archive needed):

| Task | Change |
|------|--------|
| Modifier Legend Panel | `BuildModifierLegend()` in `PrototypeRunScreenController.cs` — collapsible panel, colour swatches, tap-to-isolate with 3s coroutine |
| Overlay grouping | Group overlay GameObjects by `BossModifierId` in `Dictionary<BossModifierId, List<GameObject>>` for isolation targeting |
| Geometry spatial separation | Soft avoidance in `ModifierGeometryGenerator` — bias line starts away from cells already used by other modifiers |

**Files:** `PrototypeRunScreenController.cs`, `ModifierGeometryGenerator.cs`

---

## Step 5 — SudokuGenerationPipeline (1 feature)

**Extend existing:** `SudokuGenerator.cs`

| Task | Change |
|------|--------|
| Post-removal uniqueness checker | Run backtracking solver on puzzle grid with all active constraint rules. If >1 solution found, remove additional cells or regenerate. Gate behind Spirit Trials (always) and optionally Garden Run. |

**Files:** `SudokuGenerator.cs`, `SudokuConstraintEngine.cs`

*(OPEN-B template expansion deferred — low priority)*

---

## Step 6 — TutorialModeSystem (1 fix)

| Task | Change |
|------|--------|
| Clarify 7-star behaviour | Spec contradicts itself. Decide: require modifier or not. Update `TutorialModeService.GetStars()` if adding 7-star. |

**Files:** `TutorialModeService.cs`

---

## Step 7 — SaveLoadArchitecture (1 feature)

| Task | Change |
|------|--------|
| Steam Cloud sync | New `SteamCloudSyncService.cs` — upload/download via `SteamRemoteStorage`, `sync_metadata.json`, conflict dialog UI. Deferred to near-release. |

**Files:** New `SteamCloudSyncService.cs`, `MainMenuController.cs`

---

## Step 8 — GameDesignSpec (wrap-up)

| Task | Change |
|------|--------|
| Multi-select controls | CTRL+click, CTRL+A, CTRL+I in `PrototypeRunScreenController` |
| Endless Zen modifier pool | Expand from 8 to 14 modifiers (all except German Whispers), remove depth>=20 cap |
| Update cross-references | Fix all `_implemented` links in the spec |
| Rename | `GameDesignSpec_implemented.md` |

**Files:** `PrototypeRunScreenController.cs`, `EndlessZenService.cs`, `GameDesignSpec.md`

---

## Gap Summary

| Step | Archive+Rewrite | Extend | New Files | Gap Count |
|:---:|:---:|:---:|:---:|:---:|
| 1 Gold | 1 | 2 | 0 | 3 |
| 2 Meta | 2 | 3 | 0 | 4 |
| 3 Spirit | 1 | 6 | 0 | 6 |
| 4 Boss | 0 | 2 | 0 | 3 |
| 5 Sudoku | 0 | 2 | 0 | 1 |
| 6 Tutorial | 0 | 1 | 0 | 1 |
| 7 Save | 0 | 1 | 1 | 1 |
| 8 GDS | 0 | 3 | 0 | 3 |
| **Total** | **4** | **20** | **1** | **22** |
