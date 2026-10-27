# Documentation index: Run of the Nine

**Updated:** 2026-10-27

`docs/` is the game's specification (CLAUDE.md: it wins over code and generic rules). Every spec carries a version, a
status (new → under review → review → implemented → deprecate) and a change history; requirement IDs are listed in
[REQUIREMENT_MAP.md](REQUIREMENT_MAP.md). Bugs go to [review/GAME_GAPS.md](review/GAME_GAPS.md) (`GAP-NNN`), features to
[ROADMAP_BACKLOG.md](ROADMAP_BACKLOG.md) (`RM-NNN`), contradictions between specs to
[review/SPECIFICATION_ISSUES.md](review/SPECIFICATION_ISSUES.md) (`SPEC-CONF-*`, decided only by the owner).

## Game specifications

| Area | Document |
|---|---|
| The whole game | [GameDesignSpec.md](GameDesignSpec.md) |
| Garden path and floors | [PathSystem_GardenOverview.md](PathSystem_GardenOverview.md) |
| Bosses, trials and modifiers | [BossMechanicsSystem.md](BossMechanicsSystem.md), [BossDebuffEffects.md](BossDebuffEffects.md), [CurseSystem.md](CurseSystem.md), [PressureMechanicsSpec.md](PressureMechanicsSpec.md) |
| Items, relics, gold | [ItemsAndRelicsSystem.md](ItemsAndRelicsSystem.md), [GoldEconomySystem.md](GoldEconomySystem.md) |
| Classes and progression | [ClassXpProgressionSystem.md](ClassXpProgressionSystem.md), [ClassProgressionUnlocks.md](ClassProgressionUnlocks.md), [MetaProgressionSystem.md](MetaProgressionSystem.md), [HarmonyDifficultySystem.md](HarmonyDifficultySystem.md), [Achievements.md](Achievements.md) |
| Other modes | [EndlessZenMode.md](EndlessZenMode.md), [SpiritTrialsMode.md](SpiritTrialsMode.md), [DailyGoalSystem.md](DailyGoalSystem.md), [SeasonalChallengeSpec.md](SeasonalChallengeSpec.md), [TutorialModeSystem.md](TutorialModeSystem.md), [FirstTimePlayTutorial.md](FirstTimePlayTutorial.md) |
| Puzzles | [SudokuGenerationPipeline.md](SudokuGenerationPipeline.md), [puzzle-generation-readiness.md](puzzle-generation-readiness.md) |
| Saves, input, accessibility | [SaveLoadArchitecture.md](SaveLoadArchitecture.md), [InputAndProfilesSpec.md](InputAndProfilesSpec.md), [AccessibilitySpec.md](AccessibilitySpec.md), [accessibility-validation.md](accessibility-validation.md) |
| Look, sound, story | [AudioVisualDirection.md](AudioVisualDirection.md), [narrative-direction.md](narrative-direction.md), [audio-asset-pipeline.md](audio-asset-pipeline.md), [audio-8bit-prompts.md](audio-8bit-prompts.md), [font-asset-pipeline.md](font-asset-pipeline.md), [background-asset-budget.md](background-asset-budget.md), [reserved-art-manifest.md](reserved-art-manifest.md) |
| Languages | [localization-catalog.md](localization-catalog.md), [localization-glossary.md](localization-glossary.md) |
| Engine and code | [UnityImplementationSpec.md](UnityImplementationSpec.md), [ui-wiring-registry.md](ui-wiring-registry.md), [platform-services-foundation.md](platform-services-foundation.md), [steam-platform-integration.md](steam-platform-integration.md), [platform-ports-macos-linux.md](platform-ports-macos-linux.md) |

## Design proposals and concepts (owner picks)

| Document | State |
|---|---|
| [design/layout-proposals.md](design/layout-proposals.md) | Ink-and-paper screens: everything built except the main menu proposal (UI-STYLE-023) |
| [GameDesignSpec.md](GameDesignSpec.md) §9c and §14 | Run score grades (built, with the reasons for the proposals not chosen); the one-more-run concept: first slice built (RM-110, 111, 112, 115), RM-113, RM-116 and RM-118 to 121 open |
| [DailyGoalSystem.md](DailyGoalSystem.md), "The Daily Walk as a run" | Daily Walk (RM-122, built): the why, the reward table and the owner's answers |
| [IncenseWalkMode.md](IncenseWalkMode.md) | Incense Walk (RM-123, built): the why, fairness, the owner's answers; open: the midnight edge of the week's board |
| [ItemsAndRelicsSystem.md](ItemsAndRelicsSystem.md), "Class relics: why and the owner's answers" | Class relics (RM-124): the signature relics built, the class relic screens open |
| [GoldEconomySystem.md](GoldEconomySystem.md), "Why and the levers" | Gold and items: levers L1, L2, L3, L5 and L6 built; L4 not picked; the balance check waits for a playtest |
| [BossMechanicsSystem.md](BossMechanicsSystem.md), "Modifier catalog" | Modifier catalog (RM-001): batches B1 to B6 built; Phase 0 steps 3 and 5 open |
| [narrative-direction.md](narrative-direction.md), "Proposal: the story of the Nine" | The story of the Nine (RM-125): a proposal for the owner's pick, nothing built |

## Review, tests and quality

[review/GAME_GAPS.md](review/GAME_GAPS.md) (fix backlog) · [review/OPEN_POINTS.md](review/OPEN_POINTS.md) (what to playtest,
what waits for the owner) · [review/SPECIFICATION_ISSUES.md](review/SPECIFICATION_ISSUES.md) ·
[review/E2E_TEST_REPORT.md](review/E2E_TEST_REPORT.md) · [review/REQUIREMENT_TRACEABILITY.md](review/REQUIREMENT_TRACEABILITY.md) ·
[technical-debt-register.md](technical-debt-register.md) · [development.md](development.md) (workflow, test runner) ·
[ci-validation.md](ci-validation.md) · [smoke-test-readiness.md](smoke-test-readiness.md) ·
[runtime-resource-validation.md](runtime-resource-validation.md) · run-length report (`tools/playtest/run_length_report.py`, GameDesignSpec §14) · performance budgets
([generation](performance-generation-budget.md), [runtime](performance-runtime-budget.md),
[profiler results](performance-profiler-results.md), [UI helpers](performance-ui-update-components.md))

## Release, legal, community

- Release: [release/release-readiness-status.md](release/release-readiness-status.md), [release/platform-release-manifest.md](release/platform-release-manifest.md); the 0.1.5 checklists ([platform readiness](release/platform-readiness-checklist.md), [store assets](release/store-platform-asset-checklist.md), [known issues](release/known-issues-plan.md)) are records of that release, read by the readiness tests.
- Legal: [legal/legal-check-2026-10.md](legal/legal-check-2026-10.md) (October 2026 check and the owner's actions), [Credits.md](Credits.md), [legal/third-party-notices.md](legal/third-party-notices.md), [legal/asset-provenance-register.md](legal/asset-provenance-register.md), [legal/privacy-analytics-notice.md](legal/privacy-analytics-notice.md).
- Community: [community/discord-server-setup.md](community/discord-server-setup.md), [community/beta-test-keys.md](community/beta-test-keys.md).

## Records (kept for history; read by tests, not maintained as specs)

[AnnotationGapReport.md](AnnotationGapReport.md) (May 2026 annotation audit) ·
[controller-extraction-plan.md](controller-extraction-plan.md) (line budgets the contract tests enforce) ·
[repository-working-tree-triage.md](repository-working-tree-triage.md) · [future-scope-decision-register.md](future-scope-decision-register.md) ·
[archive/](archive/) (retired drafts).
