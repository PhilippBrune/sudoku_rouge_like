---
name: spec-analysis
description: Analyze the game specifications in docs/ — extract requirements and IDs, check spec status/version, find contradictions, and assess implementation status against code and tests. Use before implementing or reviewing any feature, when asked "what does the spec say", for gap analysis, traceability audits, or when editing a spec file.
---

# Specification Analysis

`docs/` is the authoritative game specification. Code that disagrees with a current spec is a defect or a documented spec issue — never silently "fix" the spec to match the code.

## Where requirements live

- System specs: `docs/*.md` (PascalCase files such as `BossMechanicsSystem.md`, `PressureMechanicsSpec.md`). Lowercase kebab-case docs (`ci-validation.md`, `performance-*.md`) are process and readiness docs, not gameplay specs.
- Index of all requirement IDs → file → method → status: `docs/REQUIREMENT_MAP.md`.
- In code: `// [REQ: <ID>]` comments at the implementation site. Find them with `grep -rn "\[REQ: BOSS-" Assets/Scripts`.
- Annotation coverage report: `docs/AnnotationGapReport.md`. It covers traceability only and does not prove runtime behavior.
- `tickets/` and `testcases/` folders do not exist in this repo (as of 2026-10). The local AI pipeline in `ai/` (gitignored) used `REQ-<CAT>-<NNN>`/`TKT-`/`TC-` IDs, but the tracked specs use `<SYSTEM>-<CATEGORY>-<NNN>` (e.g. `PRESSURE-TICK-005`).

## Requirement IDs

- Format used by tracked specs: `<PREFIX>-<CATEGORY>-<NNN>`. Prefixes are listed in the index table of `REQUIREMENT_MAP.md` (PRESSURE, DEBUFF, CURSE, META, ZENMODE, TRIAL, TUTO-BASICS, GEN, MAP, BOSS, XP, ITEM, SAVE, INPUT, ACCESS, ECON, SEASON, DAILY, CLASS).
- IDs are stable. Never rename, renumber, or reuse an ID. A retired requirement keeps its ID and is marked deprecated/outdated.
- New requirement: use the next free number in that prefix+category. Grep both the spec and `REQUIREMENT_MAP.md` first so you don't create a duplicate.
- If a requirement has no ID, propose one in the same format and record it in both the spec and `REQUIREMENT_MAP.md`.

## Spec header conventions

Every system spec starts with:

```markdown
**Version:** X.Y | **Date:** YYYY-MM-DD | **Status:** <status>
```

followed by a `### Change History` table (`| Version | Date | Status | Changes |`), newest row first.

Spec lifecycle status: `new` → `under review` → `review` → `implemented` → `deprecate` (also seen: `superseded`, `Deferred`).

When editing a spec:
1. Bump the version (minor for clarifications, major for behavior changes).
2. Add a Change History row with today's date and a concrete description.
3. Update the header status.
4. Mirror any per-requirement status change in `REQUIREMENT_MAP.md`.

## Per-requirement status

Markers used in specs and the map: ✅ Implemented | ⚠️ Partial | ❌ Not Implemented | 🆕 New (spec only).

A ✅ requires runtime evidence (a passing Unity EditMode/PlayMode test, or an observed E2E run) where the behavior is observable. A matching method name alone is not enough.

For review and gap-analysis work, separate **code status** from **runtime status** and assign exactly one final status:
`IMPLEMENTED` · `PARTIALLY_IMPLEMENTED` · `BROKEN` · `MISSING` · `CONTRADICTED` · `OUTDATED` · `UNTESTABLE`.

## Analysis workflow

1. Read the spec header: version, date, status. Older dates or `superseded` status mean it may not be current.
2. Read the "How to Use This Spec" / AI instructions section if present. Several specs include one.
3. List the requirement IDs touched by the task and look each one up in `REQUIREMENT_MAP.md`.
4. Open the referenced file and method and confirm the `// [REQ: ID]` tag and the actual logic match the spec text (values, thresholds, ordering).
5. Find the tests: `grep -rn "<ID>\|<MethodName>" Assets/Scripts/Tests`.
6. Cross-check other specs for the same concept. Common overlaps are star density (`SudokuGenerationPipeline.md`, `GameDesignSpec.md`, `TutorialModeSystem.md`), XP (`ClassXpProgressionSystem.md`, `GameDesignSpec.md`), and boss flow (`BossMechanicsSystem.md`, `PathSystem_GardenOverview.md`, `BossDebuffEffects.md`).

## Contradictions

Never pick a side silently. Report each one as:

```text
CONFLICT: <ID-A> (<doc A> vX.Y, date) vs <ID-B> (<doc B> vX.Y, date)
A says: ...
B says: ...
Code does: ... (file:line)
Likely current: <doc> because <newer date / status / explicit supersession>
Recommendation: define one authoritative rule in <doc>; update the other.
```

Ask the user before changing gameplay behavior to resolve a conflict.
