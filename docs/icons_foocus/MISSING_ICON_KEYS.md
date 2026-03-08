# Missing Icon Keys (Code/UI Unresolved)

This file lists unresolved icon keys required by code and UI flows, plus design-driven icon keys that are still missing canonical requirements.

Theme baseline for all keys:
- Pixel art, 64x64
- Calm greens, stone neutrals, lantern-gold highlights
- Kyoto zen garden visual language

## 1) Required for MVP

All keys in this file are treated as MVP-required.

## 2) Rarity Variant Policy

Rarity variants are required for:
- Relics
- Rarity frames
- Gameplay items with Normal/Rare/Epic behavior (for example Solver, Finder, and applicable consumables)

## 3) Modifier and Curse Policy

- Modifier icons must use color encoding by difficulty tier.
- Curse icons must use fixed accent color constraints for readability.

## 4) Unresolved Keys from Code/UI Mapping

These keys are actively referenced in code (`Resources.Load("GeneratedIcons/<key>")` or icon mapping functions) and need canonical requirement entries in icon specs.

- icon_bud
- icon_scroll_graph
- icon_bamboo_scroll
- icon_golden_bloom
- icon_garden_lantern
- icon_triple_chest
- icon_stone_gear
- icon_language_scroll
- icon_torii_lock
- icon_spin_coin
- icon_enlightenment_tree
- icon_golden_koi
- icon_moss_stone
- icon_infinite_lotus
- icon_temple_seal
- icon_ink_save
- icon_relic_pedestal
- icon_compass_of_order
- icon_stone_altar
- icon_wind_bell
- icon_tea_cup
- icon_sakura_coin
- icon_jade_amulet
- icon_pebble
- icon_coin_sakura
- icon_fog_stone
- icon_broken_mask
- icon_sacred_bell
- icon_rice_bowl
- main_menue
- main_menu

## 5) Unresolved Class Icon Keys (Design-Driven)

These class identities exist in `docs/GameDesignSpec.md` but are not yet defined as canonical class icon keys in the Fooocus requirement docs.

- icon_class_number_freak
- icon_class_zen_master
- icon_class_garden_monk
- icon_class_shrine_archivist
- icon_class_koi_gambler
- icon_class_lantern_seer
- icon_class_stone_gardener

## 6) Unresolved Item Icon Keys (Design-Driven)

These gameplay items are defined in design docs and need explicit canonical icon keys/requirements.

- icon_item_solver
- icon_item_finder
- icon_item_ink_well
- icon_item_meditation_stone
- icon_item_wind_chime
- icon_item_pattern_scroll
- icon_item_koi_reflection
- icon_item_lantern_of_clarity
- icon_item_tea_of_focus
- icon_item_cherry_blossom_pact
- icon_item_fortune_envelope
- icon_item_stone_shift
- icon_item_harmony_charm
- icon_item_compass_of_order

## 7) Unresolved Route/Mode/Event/Boss Keys

These systems are in the design scope but not yet fully specified as canonical icon keys in the current Fooocus requirement set.

- icon_route_bamboo_path
- icon_route_lantern_path
- icon_route_koi_pond_path
- icon_route_stone_garden_path
- icon_route_blossom_path
- icon_mode_garden_run
- icon_mode_endless_zen
- icon_mode_spirit_trials
- icon_event_sacrifice
- icon_event_risk
- icon_event_resource_trade
- icon_boss_garden_spirit
- icon_boss_phase_mist_veil
- icon_boss_phase_whisper_roots
- icon_boss_phase_spirit_core

## 8) Unresolved Modifier Keys

Modifier families listed in design docs need canonical icon keys.

- icon_modifier_fog_of_war
- icon_modifier_arrow_sums
- icon_modifier_german_whispers
- icon_modifier_dutch_whispers
- icon_modifier_parity_lines
- icon_modifier_renban_lines
- icon_modifier_killer_cages
- icon_modifier_difference_kropki
- icon_modifier_ratio_kropki

## 9) Tracking Fields (Required)

For each unresolved key, add these fields before generation:
- key
- category
- rarity_policy
- prompt_owner
- review_status (`draft`, `review`, `approved`)
- output_file
- source_doc

## 10) Next Step

Create `icon_registry.csv` and add every key above with status and ownership so generation can be executed without ambiguity.
