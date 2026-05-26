# Annotation Gap Report

**Generated:** 2026-05-09 (refreshed — full automated audit)
**Scope:** IDs that exist in `REQUIREMENT_MAP.md` and are marked ✅ implemented, but are **not yet annotated** in the corresponding source file with a `// [REQ: ID]` comment.

**Coverage:** 258 of 258 IDs annotated (100%) — 🎉 All gaps resolved**

---

## Traceability-only warning

This report proves only that implemented requirement IDs are annotated near source locations. It does not prove runtime behavior, does not prove UI wiring, does not prove save compatibility, and does not prove localization completeness.

Release readiness still requires Unity batchmode EditMode and PlayMode tests, smoke traversal, save durability validation, localization catalog validation, asset reference validation, legal review, and manual QA where noted in the release checklist.

---

## How to Use This Report

Each row names the ID, the file where the annotation belongs, and the exact method or field to annotate. Treat this as traceability evidence, not behavioral proof.
Priority = how many dependents break if this is wrong (H=high, M=medium, L=low).

---

## Completed Systems (100%) — no action needed

| System | IDs | Status |
|--------|:---:|--------|
| ACCESS | 5/5 | ✅ 100% |
| CLASS | 4/4 | ✅ 100% |
| DAILY | 5/5 | ✅ 100% |
| DEBUFF | 36/36 | ✅ 100% |
| ECON | 3/3 | ✅ 100% |
| GEN | 11/11 | ✅ 100% |
| INPUT | 6/6 | ✅ 100% |
| ITEM | 3/3 | ✅ 100% |
| SAVE | 10/10 | ✅ 100% |
| SEASON | 4/4 annotated (2 gaps in sub-IDs — see below) | |
| SHOP | 1/1 | ✅ 100% |
| TUTO | 10/10 | ✅ 100% |
| TRIAL | 13/13 | ✅ 100% |
| XP | 8/8 | ✅ 100% |
| ZENMODE | 9/9 | ✅ 100% |
| MAP | 14/14 | ✅ 100% |
| PRESSURE | 45/45 | ✅ 100% |
| META | 19/19 | ✅ 100% |
| BOSS | 8/8 | ✅ 100% |
| CURSE | 38/38 | ✅ 100% |
| RELIC | 4/4 | ✅ 100% |
| SEASON (full) | 6/6 | ✅ 100% |

---

## ✅ All gaps resolved — no remaining action items

All 258 IDs have been annotated. The sections below are retained for historical reference only.

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| CURSE-INT-006 | `Assets/Scripts/Run/RunDirector.cs` | Board setup / `StartLevel` — inkblot curse marks 3 cells as fogged | M |
| CURSE-INT-007 | `Assets/Scripts/Items/ItemService.cs` | `UseItem` — 4 curse-bearing items call `ApplyCurse` | M |

> Note: `CURSE-INT-006` maps to the inkblot fog injection. This is handled by `ModifierGeometryGenerator.GenerateFog` (called from the overlay pipeline), not a direct `StartLevel` call. The annotation belongs on `ModifierGeometryGenerator.GenerateFog` and/or the `BossModifierId.FogOfWar` case in the overlay generator.

---

## BOSS — Boss Mechanics (4 gaps)

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| BOSS-MOD-001 | `Assets/Scripts/Core/GameEnums.cs` | `BossModifierId` enum — 15 modifier IDs 0–14 | H |
| BOSS-MOD-002 | `Assets/Scripts/Boss/BossService.cs` | `ModifierData` — tier/impact/name/description per modifier | H |
| BOSS-MOD-003 | `Assets/Scripts/Sudoku/ModifierGeometryGenerator.cs` | `Generate` — produces overlay for each modifier | H |
| BOSS-FLOOR-002 | `Assets/Scripts/Boss/BossService.cs` | `BuildEligiblePool` — boss choice pool excludes active floor modifiers | M |

---

## META — Meta Progression (11 gaps)

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| META-XP-002 | `Assets/Scripts/Economy/XpTable.cs` | `DeriveLevel` — two-phase curve L1–15 + L16–40 | H |
| META-XP-003 | `Assets/Scripts/Economy/XpTable.cs` | Total to L40: 16,860 XP constant | M |
| META-XP-004 | `Assets/Scripts/Save/ProfileService.cs` | `RecordRunAndGetNewUnlocks` — XP committed at run end only | H |
| META-PRESTIGE-004 | `Assets/Scripts/Save/ProfileService.cs` | `TryPrestige` — on prestige: TotalXp=0, PrestigeTier++ | M |
| META-ASCEND-001 | `Assets/Scripts/Core/SaveModels.cs` | `MetaProgressionState.AscensionLevel` field | M |
| META-ASCEND-002 | `Assets/Scripts/Meta/AscensionService.cs` | `ApplyAscension` — increments AscensionLevel, +1 MaxStarCap, unlocks seasonal | H |
| META-ASCEND-003 | `Assets/Scripts/Meta/AscensionService.cs` | `TryPrestigeReset` — resets AscensionLevel=0, MaxStarCap=5 | M |
| META-ASCEND-004 | `Assets/Scripts/Meta/AscensionService.cs` | `BuildMonthlySeed` — monthly seed: `(year × 100) + month` | M |
| META-COMPLETE-001 | `Assets/Scripts/Meta/CompletionService.cs` | `Recalculate` — 4 checks × 25% completion score | M |
| META-CODEX-001 | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery`, `RecordRelicDiscovery` — item & relic codex | M |
| META-CODEX-002 | `Assets/Scripts/Save/ProfileService.cs` | `RecordItemDiscovery` — entry marked Discovered on first acquisition only | L |

---

## RELIC — Items & Relics (1 gap)

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| RELIC-SLOT-001 | `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.HasRelic + HeldRelic` — single relic slot | M |

---

## SEASON — Seasonal Challenge (2 gaps)

| ID | File | Method / Location | Priority |
|----|------|-------------------|----------|
| SEASON-UNLOCK-001 | `Assets/Scripts/Meta/AscensionService.cs` | `ApplyAscension` — `SeasonalChallengeUnlocked = true` after first ascension | M |
| SEASON-SEED-001 | `Assets/Scripts/Save/SaveFileService.cs` | `HasActiveSeasonalRun` — monthly seed: `year * 100 + month` | M |

---

## Summary by System

| System | Total IDs | Annotated | Gaps | % Done |
|--------|:---------:|:---------:|:----:|:------:|
| ACCESS | 5 | 5 | 0 | ✅ 100% |
| BOSS | 8 | 8 | 0 | ✅ 100% |
| CLASS | 4 | 4 | 0 | ✅ 100% |
| CURSE | 38 | 38 | 0 | ✅ 100% |
| DAILY | 5 | 5 | 0 | ✅ 100% |
| DEBUFF | 36 | 36 | 0 | ✅ 100% |
| ECON | 3 | 3 | 0 | ✅ 100% |
| GEN | 11 | 11 | 0 | ✅ 100% |
| INPUT | 6 | 6 | 0 | ✅ 100% |
| ITEM | 3 | 3 | 0 | ✅ 100% |
| MAP | 14 | 14 | 0 | ✅ 100% |
| META | 19 | 19 | 0 | ✅ 100% |
| PRESSURE | 45 | 45 | 0 | ✅ 100% |
| RELIC | 4 | 4 | 0 | ✅ 100% |
| SAVE | 10 | 10 | 0 | ✅ 100% |
| SEASON | 6 | 6 | 0 | ✅ 100% |
| SHOP | 1 | 1 | 0 | ✅ 100% |
| TRIAL | 13 | 13 | 0 | ✅ 100% |
| TUTO | 10 | 10 | 0 | ✅ 100% |
| XP | 8 | 8 | 0 | ✅ 100% |
| ZENMODE | 9 | 9 | 0 | ✅ 100% |
| **TOTAL** | **258** | **258** | **0** | **✅ 100%** |

### Status: Complete — no further action needed

All 258 requirement IDs have inline `// [REQ: ID]` annotations in their source files.
