# Achievements

**Version:** 1.7 | **Date:** 2026-11-08 | **Status:** implemented (63 achievements saved in Steamworks in five languages; the 34 newest not published yet)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.7 | 2026-11-08 | implemented | floor_5: the English description is the player text only ("Reach the last floor of a Garden Run."); the floors are in the condition column. English for floor_5 and the eight Swift Path challenges goes to Steamworks as a localization file (`achievement_loc_vdf.py --english`). |
| 1.6 | 2026-11-03 | implemented (63 in Steamworks, the 34 newest not published) | The 24 class challenge achievements are created in Steamworks with both icons and entered in German, French, Spanish and Brazilian Portuguese: the page shows 63 achievements, every language fully localized. They continue the token series `NEW_ACHIEVEMENT_5_7` to `_30`. Saved, not published. |
| 1.5 | 2026-11-03 | implemented (game side; the 24 challenge achievements are not in Steamworks yet) | Owner (2026-11-03): RM-116 with 24 achievements, one per class challenge (CLASS-CHALLENGE-002): three per class, the class's own, a win in under 40 minutes and a win at Harmony VI. Icons by `tools/steam/achievement_icons.py`. |
| 1.4 | 2026-11-03 | implemented (39 in Steamworks, the story ones not published) | Owner (2026-11-03): "füge die hier hinzu ... ich erlaube dir das". The ten story achievements are created in Steamworks (App 4858470) with name, description and both icons, and entered in German, French, Spanish and Brazilian Portuguese: the page shows 39 achievements, every language fully localized. Saved, not published (Publish waits for the owner). Steamworks starts a new token series after a reload of the achievements page, so the last seven story achievements have the tokens `NEW_ACHIEVEMENT_5_0` to `_6`; `achievement_loc_vdf.py` keeps each achievement's real token. |
| 1.3 | 2026-11-02 | implemented (game side; the 10 story achievements are not in Steamworks yet) | Owner (0.3.1 playtest): "Story-Erfolge ... füge den hinzu", answer "Zehn". Ten story achievements (ACHIEVE-STORY-001): a second chapter of any keeper, all five chapters of each keeper (8), the ninth keeper's chapter. Icons by `tools/steam/achievement_icons.py`. They still need entering in Steamworks (§4), then publishing with the owner's OK. |
| 1.2 | 2026-10-27 | implemented | §5 has the eight Harmony achievements too. All 29 achievements are entered in German, French, Spanish (Spain) and Portuguese (Brazil) on the Achievement Localization page (files by `tools/steam/achievement_loc_vdf.py`); saved, not published. |
| 1.1 | 2026-10-27 | implemented | Owner: "Lade die neuen Achievements ... hoch". The 21 new achievements are created in Steamworks (App 4858470) with name, description and both icons, saved, not published (Publish waits for the owner's OK). The other four languages (§5) are not entered yet. The Daily Walk streak is checked at the end of a Daily Walk too (it is a Garden Run of two floors since DailyGoalSystem 0.4). |
| 1.0 | 2026-10-26 | implemented | Owner request: "Review the current achievements and add more to the game." Review of the eight Harmony achievements; 21 new achievements for the first hours, the classes, the other modes and the collection (ACHIEVE-RUN-001, ACHIEVE-CLASS-001, ACHIEVE-MODE-001, ACHIEVE-COLLECT-001). |

## 1. Review of the achievements so far

The game had eight Steam achievements, all for winning Garden Runs on higher Harmony levels (HarmonyDifficultySystem §9, HARMONY-ACHIEVE-001).

- **Nothing in the first hours.** The first achievement needs a win at Harmony I, which needs a win at Harmony 0 first. A new player can play for many hours without one. Steam players look at the achievement list to judge a game's depth, and an empty list after an evening reads as "nothing to do".
- **Only one way to play counts.** Classes, Endless Zen, Spirit Trials, the Monthly Walk, the Daily Walk and the Codex give nothing.
- **Many recorded facts are unused.** The game already counts puzzles, bosses, class levels, unlocks, Codex finds, streaks and depths.

The eight stay as they are (names and API keys are live). The new ones fill the gaps. Every one of them is earned from data the game records at a run's end; none needs a new screen.

## 2. All achievements

API names are the Steamworks keys (`SteamAchievementService.AllAchievementIds`); `ProgressAchievementTests` checks that every key the game uses is listed here.

### Harmony (existing, HARMONY-ACHIEVE-001)

| API name | Name | Description |
|---|---|---|
| `harmony_1_win` | First Awakening | Complete a Garden Run on Harmony Level 1 or higher. |
| `harmony_3_win` | Moss and Gravel | Complete a Garden Run on Harmony Level 3 or higher. |
| `harmony_5_win` | Water Reflection | Complete a Garden Run on Harmony Level 5 or higher. |
| `harmony_7_win` | Lantern Glow | Complete a Garden Run on Harmony Level 7 or higher. |
| `harmony_9_win` | Torii Gateway | Complete a Garden Run on Harmony Level 9 or higher. |
| `harmony_10_win` | Summit Ascent | Complete a Garden Run on Harmony Level 10. |
| `harmony_10_perfect` | Tranquil Perfection | Complete a Garden Run on Harmony Level 10 with zero mistakes. (hidden) |
| `harmony_all_classes` | Master of the Garden | Complete a Garden Run on Harmony Level 5 or higher with all 8 classes. |

### The run (ACHIEVE-RUN-001)

Counted for Garden Runs only (not tutorials, custom puzzles or the other modes). "First Stone" and "The Gate Opens" read the profile's totals, so an existing profile earns them at its next run end.

| API name | Name | Description | Condition in the game |
|---|---|---|---|
| `first_puzzle` | First Stone | Solve your first puzzle. | `ProfileStats.TotalPuzzlesSolved` ≥ 1 |
| `first_boss` | The Gate Opens | Defeat your first boss. | `ProfileStats.TotalBossesDefeated` ≥ 1 |
| `floor_5` | Summit in Sight | Reach the last floor of a Garden Run. | `FloorReached` ≥ 3 on a three-floor run, else ≥ 5 (RUN-FLOORS-001) |
| `first_win` | Through the Garden | Win a Garden Run. | a won Garden Run, any Harmony |
| `perfect_run` | Clean Ink | Win a Garden Run without a single mistake. | won, `MistakesMade` = 0 |
| `no_items_win` | Empty Hands | Win a Garden Run without using an item. | won, `ItemsUsedThisRun` = 0 |
| `one_hp_win` | By a Thread | Win a Garden Run with 1 HP left. | won, HP at the end = 1 |
| `cursed_three` | Wearing the Curse | Finish three cursed puzzles in one run. | `CursedPuzzlesAccepted` (accepted and finished) ≥ 3 |
| `nine_by_nine` | The Full Grid | Solve a 9×9 puzzle. | a solved puzzle with board size 9 |
| `five_stars` | Five Stars | Solve a five-star puzzle. | a solved puzzle with 5 stars |

### The classes (ACHIEVE-CLASS-001)

| API name | Name | Description | Condition in the game |
|---|---|---|---|
| `class_second` | A Second Walker | Unlock a second class. | 2 classes unlocked |
| `class_all` | Eight Walkers | Unlock all eight classes. | 8 classes unlocked |
| `class_level_10` | Steady Steps | Raise a class to Level 10. | highest class level ≥ 10 |
| `class_level_40` | Master of a Path | Raise a class to Level 40. | highest class level ≥ 40 |
| `win_every_class` | Eight Ways Through | Win a Garden Run with every class. | `MetaProgressionState.ClassesWithAWin` holds all 8 (counted from 0.2.2 on) |

### The other modes (ACHIEVE-MODE-001)

| API name | Name | Description | Condition in the game |
|---|---|---|---|
| `endless_25` | The Deep Garden | Reach depth 25 in Endless Zen. | the session's depth ≥ 25 |
| `trials_grandmaster` | The Spirits Bow | Pass a Spirit Trial at Grandmaster. | a passed trial at the Grandmaster tier |
| `monthly_walk` | Under the Moon | Finish a Monthly Walk. | a finished Monthly Walk |

### The collection (ACHIEVE-COLLECT-001)

| API name | Name | Description | Condition in the game |
|---|---|---|---|
| `daily_streak_7` | Seven Dawns | Keep a Daily Walk streak for 7 days. | `DailyGoalState.CurrentStreak` ≥ 7 at a Garden Run's or a Daily Walk's end |
| `codex_half` | Half the Collection | Find half of everything in the Codex. | Items, Relics, Rules and Curses together: found ≥ half |
| `codex_all` | The Complete Codex | Find everything in the Codex. | every Codex page complete |

### The story (ACHIEVE-STORY-001, since 0.3.3)

| API name | Name | Description | Condition in the game |
|---|---|---|---|
| `story_new_page` | A New Page | Open a second chapter of any keeper's tale. | `StoryService.ChapterOf` ≥ 2 for any keeper |
| `story_tale_number_freak` | The Number Freak's Tale | Open all five chapters of the Number Freak's tale. | `ChapterOf(NumberFreak)` = 5 |
| `story_tale_garden_monk` | The Garden Monk's Tale | Open all five chapters of the Garden Monk's tale. | `ChapterOf(GardenMonk)` = 5 |
| `story_tale_shrine_archivist` | The Shrine Archivist's Tale | Open all five chapters of the Shrine Archivist's tale. | `ChapterOf(ShrineArchivist)` = 5 |
| `story_tale_koi_gambler` | The Koi Gambler's Tale | Open all five chapters of the Koi Gambler's tale. | `ChapterOf(KoiGambler)` = 5 |
| `story_tale_stone_gardener` | The Stone Gardener's Tale | Open all five chapters of the Stone Gardener's tale. | `ChapterOf(StoneGardener)` = 5 |
| `story_tale_lantern_seer` | The Lantern Seer's Tale | Open all five chapters of the Lantern Seer's tale. | `ChapterOf(LanternSeer)` = 5 |
| `story_tale_reed_duelist` | The Reed Duelist's Tale | Open all five chapters of the Reed Duelist's tale. | `ChapterOf(ReedDuelist)` = 5 |
| `story_tale_quiet_cartographer` | The Quiet Cartographer's Tale | Open all five chapters of the Quiet Cartographer's tale. | `ChapterOf(QuietCartographer)` = 5 |
| `story_ninth_keeper` | The Ninth Keeper | Open the last chapter of the story of the Nine. | `StoryNinthOpened` |

### Class challenges (CLASS-CHALLENGE-002, since 0.4.1)

A Garden Run with the class (not the Daily Walk); each is also a seal on the class's plaque in the garden of classes.

| API name | Name | Description | Condition in the game |
|---|---|---|---|
| `challenge_number_freak_1` | Ten Clean Counts (Number Freak) | Win a Garden Run with 10 perfect puzzles. | won, `PerfectPuzzleCount` ≥ 10 |
| `challenge_number_freak_2` | The Swift Path (Number Freak) | Win a Garden Run in under 25 minutes. | won, the run open under 25 minutes (`ActiveSeconds`; 40 for a run saved before 0.4.3) |
| `challenge_number_freak_3` | The High Path (Number Freak) | Win a Garden Run on Harmony VI or higher. | won at Harmony ≥ 6 |
| `challenge_garden_monk_1` | Empty Hands, Full Heart (Garden Monk) | Win a Garden Run without using an item. | won, `ItemsUsedThisRun` = 0 |
| `challenge_garden_monk_2` | The Swift Path (Garden Monk) | Win a Garden Run in under 25 minutes. | won, the run open under 25 minutes (`ActiveSeconds`; 40 for a run saved before 0.4.3) |
| `challenge_garden_monk_3` | The High Path (Garden Monk) | Win a Garden Run on Harmony VI or higher. | won at Harmony ≥ 6 |
| `challenge_shrine_archivist_1` | The Archive of Four (Shrine Archivist) | End a Garden Run holding 4 relics. | `RelicsCollectedThisRun` ≥ 4 at the end |
| `challenge_shrine_archivist_2` | The Swift Path (Shrine Archivist) | Win a Garden Run in under 25 minutes. | won, the run open under 25 minutes (`ActiveSeconds`; 40 for a run saved before 0.4.3) |
| `challenge_shrine_archivist_3` | The High Path (Shrine Archivist) | Win a Garden Run on Harmony VI or higher. | won at Harmony ≥ 6 |
| `challenge_koi_gambler_1` | Three Curses Bet (Koi Gambler) | Win a Garden Run with 3 cursed puzzles finished. | won, `CursedPuzzlesAccepted` ≥ 3 |
| `challenge_koi_gambler_2` | The Swift Path (Koi Gambler) | Win a Garden Run in under 25 minutes. | won, the run open under 25 minutes (`ActiveSeconds`; 40 for a run saved before 0.4.3) |
| `challenge_koi_gambler_3` | The High Path (Koi Gambler) | Win a Garden Run on Harmony VI or higher. | won at Harmony ≥ 6 |
| `challenge_stone_gardener_1` | The Busy Rake (Stone Gardener) | Win a Garden Run using 8 items. | won, `ItemsUsedThisRun` ≥ 8 |
| `challenge_stone_gardener_2` | The Swift Path (Stone Gardener) | Win a Garden Run in under 25 minutes. | won, the run open under 25 minutes (`ActiveSeconds`; 40 for a run saved before 0.4.3) |
| `challenge_stone_gardener_3` | The High Path (Stone Gardener) | Win a Garden Run on Harmony VI or higher. | won at Harmony ≥ 6 |
| `challenge_lantern_seer_1` | Three Lights at Once (Lantern Seer) | Defeat a boss that brings 3 rules at once. | `RunState.MostBossRulesCleared` ≥ 3 |
| `challenge_lantern_seer_2` | The Swift Path (Lantern Seer) | Win a Garden Run in under 25 minutes. | won, the run open under 25 minutes (`ActiveSeconds`; 40 for a run saved before 0.4.3) |
| `challenge_lantern_seer_3` | The High Path (Lantern Seer) | Win a Garden Run on Harmony VI or higher. | won at Harmony ≥ 6 |
| `challenge_reed_duelist_1` | A Quiet Floor (Reed Duelist) | Clear a floor without a pencil mark and without losing HP. | `RunState.CleanFloors` ≥ 1 |
| `challenge_reed_duelist_2` | The Swift Path (Reed Duelist) | Win a Garden Run in under 25 minutes. | won, the run open under 25 minutes (`ActiveSeconds`; 40 for a run saved before 0.4.3) |
| `challenge_reed_duelist_3` | The High Path (Reed Duelist) | Win a Garden Run on Harmony VI or higher. | won at Harmony ≥ 6 |
| `challenge_quiet_cartographer_1` | Six Steps of Risk (Quiet Cartographer) | Win a Garden Run taking 6 tiles of the risk path. | won, `RunState.RiskTilesTaken` ≥ 6 |
| `challenge_quiet_cartographer_2` | The Swift Path (Quiet Cartographer) | Win a Garden Run in under 25 minutes. | won, the run open under 25 minutes (`ActiveSeconds`; 40 for a run saved before 0.4.3) |
| `challenge_quiet_cartographer_3` | The High Path (Quiet Cartographer) | Win a Garden Run on Harmony VI or higher. | won at Harmony ≥ 6 |

## 3. Requirements

| ID | Requirement |
|---|---|
| ACHIEVE-RUN-001 | At every Garden Run's end (`ProfileService.RecordRunAndGetNewUnlocks`, after XP, unlocks and Codex finds are counted) the game grants the run achievements of §2 whose condition holds. Tutorials, custom puzzles and the other modes grant none of them. |
| ACHIEVE-CLASS-001 | At the same point the class achievements of §2 are checked; a won Garden Run adds its class to `ClassesWithAWin` (old saves start empty). |
| ACHIEVE-MODE-001 | The results of Endless Zen, Spirit Trials and the Monthly Walk check their achievement before they save (`EndScreenViewController`); a new achievement is saved even when the result is not a new best. |
| ACHIEVE-COLLECT-001 | The Daily Walk streak and the Codex achievements are checked at every Garden Run's end, the Daily Walk's included. |
| ACHIEVE-STORY-001 | The story achievements are checked at the same point, after `StoryService.Record` has opened this run's chapters; an old save that already holds chapters gets them at its next Garden Run's end. |

Every grant is kept in the profile (`MasteryAchievementState.UnlockedAchievements`) and sent to Steam when Steam runs; unlocks made without Steam are sent at the next start with Steam (`SyncUnlockedAchievements`). An API name Steamworks does not know yet fails quietly (a warning in the log) and is sent again later.

## 4. Steamworks (created 2026-10-27, the story and class challenge achievements 2026-11-03; publishing waits for the owner)

Steamworks → App 4858470 → Stats & Achievements → Achievements: one new achievement per row of §2 with the API name, name, description, hidden = no, and the two icons (`steam_assets/achievements/achieved_<api name>.jpg` and `unachieved_<api name>.jpg`, 256×256). Then publish (stop and check before the Publish button, as always). The texts in the other four languages (§5) go in on Stats & Achievements → Achievement Localization: `tools/steam/achievement_loc_vdf.py` writes one Key/Values file per language from §5 into `steam_assets/achievements/localization/`, and each file is uploaded there (entered 2026-10-27 for all 29, 2026-11-03 for all 39). The tokens follow the order the achievements were created in, and a reload of the achievements page starts a new series (`NEW_ACHIEVEMENT_5_<n>` for the last seven story achievements): download the English localization file first when adding more.

For entering them: `steam_assets/achievements/steamworks_entry.html` lists the 21 new achievements with both icons and every text in five languages (a click copies a field), and `new_achievements_overview.jpg` shows all icons at once (both made by `tools/steam/achievement_sheet.py` from this page).

## 5. Names and descriptions in the other languages

Name and description, separated by the colon; the mode and screen names are the game's own (Gartenrunde, Parcours du jardin, Recorrido del jardín, Percurso do jardim; Etage, Étage, Piso, Andar; LP and PV).

| API name | Deutsch | Français | Español | Português (Brasil) |
|---|---|---|---|---|
| `harmony_1_win` | Erstes Erwachen: Schließe eine Gartenrunde auf Harmonie 1 oder höher ab. | Premier éveil : terminez un parcours du jardin en Harmonie 1 ou plus. | Primer despertar: completa un recorrido del jardín en Armonía 1 o superior. | Primeiro despertar: conclua um percurso do jardim em Harmonia 1 ou superior. |
| `harmony_3_win` | Moos und Kies: Schließe eine Gartenrunde auf Harmonie 3 oder höher ab. | Mousse et gravier : terminez un parcours du jardin en Harmonie 3 ou plus. | Musgo y grava: completa un recorrido del jardín en Armonía 3 o superior. | Musgo e cascalho: conclua um percurso do jardim em Harmonia 3 ou superior. |
| `harmony_5_win` | Wasserspiegelung: Schließe eine Gartenrunde auf Harmonie 5 oder höher ab. | Reflet dans l'eau : terminez un parcours du jardin en Harmonie 5 ou plus. | Reflejo en el agua: completa un recorrido del jardín en Armonía 5 o superior. | Reflexo na água: conclua um percurso do jardim em Harmonia 5 ou superior. |
| `harmony_7_win` | Laternenschein: Schließe eine Gartenrunde auf Harmonie 7 oder höher ab. | Lueur de lanterne : terminez un parcours du jardin en Harmonie 7 ou plus. | Brillo de farol: completa un recorrido del jardín en Armonía 7 o superior. | Brilho da lanterna: conclua um percurso do jardim em Harmonia 7 ou superior. |
| `harmony_9_win` | Das Torii-Tor: Schließe eine Gartenrunde auf Harmonie 9 oder höher ab. | Le portail torii : terminez un parcours du jardin en Harmonie 9 ou plus. | El portal torii: completa un recorrido del jardín en Armonía 9 o superior. | O portal torii: conclua um percurso do jardim em Harmonia 9 ou superior. |
| `harmony_10_win` | Aufstieg zum Gipfel: Schließe eine Gartenrunde auf Harmonie 10 ab. | Ascension du sommet : terminez un parcours du jardin en Harmonie 10. | Ascenso a la cima: completa un recorrido del jardín en Armonía 10. | Subida ao cume: conclua um percurso do jardim em Harmonia 10. |
| `harmony_10_perfect` | Stille Vollkommenheit: Schließe eine Gartenrunde auf Harmonie 10 ohne einen einzigen Fehler ab. | Perfection tranquille : terminez un parcours du jardin en Harmonie 10 sans aucune erreur. | Perfección serena: completa un recorrido del jardín en Armonía 10 sin ningún error. | Perfeição tranquila: conclua um percurso do jardim em Harmonia 10 sem nenhum erro. |
| `harmony_all_classes` | Meister des Gartens: Schließe mit allen 8 Klassen eine Gartenrunde auf Harmonie 5 oder höher ab. | Maître du jardin : terminez un parcours du jardin en Harmonie 5 ou plus avec les 8 classes. | Maestro del jardín: completa un recorrido del jardín en Armonía 5 o superior con las 8 clases. | Mestre do jardim: conclua um percurso do jardim em Harmonia 5 ou superior com as 8 classes. |
| `first_puzzle` | Der erste Stein: Löse dein erstes Rätsel. | La première pierre : résolvez votre premier puzzle. | La primera piedra: resuelve tu primer puzle. | A primeira pedra: resolva seu primeiro quebra-cabeça. |
| `first_boss` | Das Tor öffnet sich: Besiege deinen ersten Boss. | La porte s'ouvre : battez votre premier boss. | La puerta se abre: derrota a tu primer jefe. | O portão se abre: derrote seu primeiro chefe. |
| `floor_5` | Der Gipfel in Sicht: Erreiche die letzte Etage einer Gartenrunde. | Le sommet en vue : atteignez le dernier étage d'un parcours du jardin. | La cima a la vista: llega al último piso de un recorrido del jardín. | O cume à vista: chegue ao último andar de um percurso do jardim. |
| `first_win` | Durch den Garten: Gewinne eine Gartenrunde. | À travers le jardin : remportez un parcours du jardin. | A través del jardín: gana un recorrido del jardín. | Pelo jardim: vença um percurso do jardim. |
| `perfect_run` | Reine Tinte: Gewinne eine Gartenrunde ohne einen einzigen Fehler. | Encre pure : remportez un parcours du jardin sans une seule erreur. | Tinta limpia: gana un recorrido del jardín sin un solo error. | Tinta limpa: vença um percurso do jardim sem nenhum erro. |
| `no_items_win` | Mit leeren Händen: Gewinne eine Gartenrunde, ohne einen Gegenstand zu benutzen. | Les mains vides : remportez un parcours du jardin sans utiliser d'objet. | Con las manos vacías: gana un recorrido del jardín sin usar objetos. | De mãos vazias: vença um percurso do jardim sem usar itens. |
| `one_hp_win` | Am seidenen Faden: Gewinne eine Gartenrunde mit 1 LP übrig. | Sur le fil : remportez un parcours du jardin avec 1 PV restant. | Por un hilo: gana un recorrido del jardín con 1 PV restante. | Por um fio: vença um percurso do jardim com 1 PV restante. |
| `cursed_three` | Den Fluch tragen: Beende drei verfluchte Rätsel in einer Runde. | Porter la malédiction : terminez trois puzzles maudits en un parcours. | Llevar la maldición: termina tres puzles malditos en un recorrido. | Carregar a maldição: conclua três quebra-cabeças amaldiçoados em um percurso. |
| `nine_by_nine` | Das volle Raster: Löse ein 9×9-Rätsel. | La grille complète : résolvez un puzzle 9×9. | La cuadrícula completa: resuelve un puzle de 9×9. | A grade completa: resolva um quebra-cabeça 9×9. |
| `five_stars` | Fünf Sterne: Löse ein Fünf-Sterne-Rätsel. | Cinq étoiles : résolvez un puzzle cinq étoiles. | Cinco estrellas: resuelve un puzle de cinco estrellas. | Cinco estrelas: resolva um quebra-cabeça de cinco estrelas. |
| `class_second` | Ein zweiter Wanderer: Schalte eine zweite Klasse frei. | Un deuxième marcheur : débloquez une deuxième classe. | Un segundo caminante: desbloquea una segunda clase. | Um segundo caminhante: desbloqueie uma segunda classe. |
| `class_all` | Acht Wanderer: Schalte alle acht Klassen frei. | Huit marcheurs : débloquez les huit classes. | Ocho caminantes: desbloquea las ocho clases. | Oito caminhantes: desbloqueie as oito classes. |
| `class_level_10` | Feste Schritte: Bringe eine Klasse auf Stufe 10. | Pas assurés : faites monter une classe au niveau 10. | Pasos firmes: sube una clase al nivel 10. | Passos firmes: leve uma classe ao nível 10. |
| `class_level_40` | Meister eines Pfades: Bringe eine Klasse auf Stufe 40. | Maître d'une voie : faites monter une classe au niveau 40. | Maestro de un camino: sube una clase al nivel 40. | Mestre de um caminho: leve uma classe ao nível 40. |
| `win_every_class` | Acht Wege hindurch: Gewinne eine Gartenrunde mit jeder Klasse. | Huit chemins : remportez un parcours du jardin avec chaque classe. | Ocho caminos: gana un recorrido del jardín con cada clase. | Oito caminhos: vença um percurso do jardim com cada classe. |
| `endless_25` | Der tiefe Garten: Erreiche Tiefe 25 im Endlosen Zen. | Le jardin profond : atteignez la profondeur 25 en Zen infini. | El jardín profundo: llega a la profundidad 25 en Zen infinito. | O jardim profundo: chegue à profundidade 25 no Zen infinito. |
| `trials_grandmaster` | Die Geister verneigen sich: Bestehe die Geisterprüfungen als Großmeister. | Les esprits s'inclinent : réussissez les Épreuves spirituelles au rang Grand maître. | Los espíritus se inclinan: supera las Pruebas espirituales en Gran maestro. | Os espíritos se curvam: passe nas Provas espirituais no nível Grão-mestre. |
| `monthly_walk` | Unter dem Mond: Beende einen Monatsgang. | Sous la lune : terminez une Promenade mensuelle. | Bajo la luna: termina un Paseo mensual. | Sob a lua: conclua uma Caminhada mensal. |
| `daily_streak_7` | Sieben Morgen: Halte eine Tagesgang-Serie sieben Tage lang. | Sept aubes : gardez une série de Promenades quotidiennes pendant 7 jours. | Siete amaneceres: mantén una racha del Paseo diario durante 7 días. | Sete alvoradas: mantenha uma sequência da Caminhada diária por 7 dias. |
| `codex_half` | Die halbe Sammlung: Finde die Hälfte von allem im Kodex. | La moitié de la collection : trouvez la moitié de tout le Codex. | La mitad de la colección: encuentra la mitad de todo el Códice. | Metade da coleção: encontre metade de tudo no Códice. |
| `codex_all` | Der vollständige Kodex: Finde alles im Kodex. | Le Codex complet : trouvez tout dans le Codex. | El Códice completo: encuentra todo en el Códice. | O Códice completo: encontre tudo no Códice. |
| `story_new_page` | Eine neue Seite: Öffne ein zweites Kapitel der Geschichte eines Hüters. | Une nouvelle page : ouvrez un deuxième chapitre du récit d'un gardien. | Una página nueva: abre un segundo capítulo del relato de un guardián. | Uma nova página: abra um segundo capítulo do conto de um guardião. |
| `story_tale_number_freak` | Die ganze Geschichte (Zahlen-Freak): Öffne alle fünf Kapitel des Hüters Zahlen-Freak. | Le récit complet (Maniaque des chiffres) : ouvrez les cinq chapitres du gardien Maniaque des chiffres. | El relato completo (Maniático de los números): abre los cinco capítulos del guardián Maniático de los números. | O conto completo (Maníaco dos números): abra os cinco capítulos do guardião Maníaco dos números. |
| `story_tale_garden_monk` | Die ganze Geschichte (Gartenmönch): Öffne alle fünf Kapitel des Hüters Gartenmönch. | Le récit complet (Moine du jardin) : ouvrez les cinq chapitres du gardien Moine du jardin. | El relato completo (Monje del jardín): abre los cinco capítulos del guardián Monje del jardín. | O conto completo (Monge do jardim): abra os cinco capítulos do guardião Monge do jardim. |
| `story_tale_shrine_archivist` | Die ganze Geschichte (Schrein-Archivar): Öffne alle fünf Kapitel des Hüters Schrein-Archivar. | Le récit complet (Archiviste du sanctuaire) : ouvrez les cinq chapitres du gardien Archiviste du sanctuaire. | El relato completo (Archivista del santuario): abre los cinco capítulos del guardián Archivista del santuario. | O conto completo (Arquivista do santuário): abra os cinco capítulos do guardião Arquivista do santuário. |
| `story_tale_koi_gambler` | Die ganze Geschichte (Koi-Spieler): Öffne alle fünf Kapitel des Hüters Koi-Spieler. | Le récit complet (Joueur de koï) : ouvrez les cinq chapitres du gardien Joueur de koï. | El relato completo (Jugador de koi): abre los cinco capítulos del guardián Jugador de koi. | O conto completo (Jogador de koi): abra os cinco capítulos do guardião Jogador de koi. |
| `story_tale_stone_gardener` | Die ganze Geschichte (Steingärtner): Öffne alle fünf Kapitel des Hüters Steingärtner. | Le récit complet (Jardinier de pierre) : ouvrez les cinq chapitres du gardien Jardinier de pierre. | El relato completo (Jardinero de piedra): abre los cinco capítulos del guardián Jardinero de piedra. | O conto completo (Jardineiro de pedra): abra os cinco capítulos do guardião Jardineiro de pedra. |
| `story_tale_lantern_seer` | Die ganze Geschichte (Laternenseher): Öffne alle fünf Kapitel des Hüters Laternenseher. | Le récit complet (Voyant de la lanterne) : ouvrez les cinq chapitres du gardien Voyant de la lanterne. | El relato completo (Vidente del farol): abre los cinco capítulos del guardián Vidente del farol. | O conto completo (Vidente da lanterna): abra os cinco capítulos do guardião Vidente da lanterna. |
| `story_tale_reed_duelist` | Die ganze Geschichte (Schilfduellant): Öffne alle fünf Kapitel des Hüters Schilfduellant. | Le récit complet (Duelliste du roseau) : ouvrez les cinq chapitres du gardien Duelliste du roseau. | El relato completo (Duelista de caña): abre los cinco capítulos del guardián Duelista de caña. | O conto completo (Duelista do junco): abra os cinco capítulos do guardião Duelista do junco. |
| `story_tale_quiet_cartographer` | Die ganze Geschichte (Stiller Kartograf): Öffne alle fünf Kapitel des Hüters Stiller Kartograf. | Le récit complet (Cartographe silencieux) : ouvrez les cinq chapitres du gardien Cartographe silencieux. | El relato completo (Cartógrafo silencioso): abre los cinco capítulos del guardián Cartógrafo silencioso. | O conto completo (Cartógrafo silencioso): abra os cinco capítulos do guardião Cartógrafo silencioso. |
| `story_ninth_keeper` | Der neunte Hüter: Öffne das letzte Kapitel der Geschichte der Neun. | Le neuvième gardien : ouvrez le dernier chapitre du récit des Neuf. | El noveno guardián: abre el último capítulo del relato de los Nueve. | O nono guardião: abra o último capítulo do conto dos Nove. |
| `challenge_number_freak_1` | Zehn saubere Rechnungen (Zahlen-Freak): gewinne eine Gartenrunde mit 10 perfekten Rätseln. | Dix comptes sans faute (Maniaque des chiffres) : remportez un parcours du jardin avec 10 puzzles parfaits. | Diez cuentas limpias (Maniático de los números): gana un recorrido del jardín con 10 puzles perfectos. | Dez contas limpas (Maníaco dos números): vença um percurso do jardim com 10 quebra-cabeças perfeitos. |
| `challenge_number_freak_2` | Der schnelle Pfad (Zahlen-Freak): gewinne eine Gartenrunde in unter 25 Minuten. | Le chemin rapide (Maniaque des chiffres) : remportez un parcours du jardin en moins de 25 minutes. | El camino veloz (Maniático de los números): gana un recorrido del jardín en menos de 25 minutos. | O caminho veloz (Maníaco dos números): vença um percurso do jardim em menos de 25 minutos. |
| `challenge_number_freak_3` | Der hohe Pfad (Zahlen-Freak): gewinne eine Gartenrunde auf Harmonie VI oder höher. | Le haut chemin (Maniaque des chiffres) : remportez un parcours du jardin en Harmonie VI ou plus. | El camino alto (Maniático de los números): gana un recorrido del jardín en Armonía VI o superior. | O caminho alto (Maníaco dos números): vença um percurso do jardim em Harmonia VI ou superior. |
| `challenge_garden_monk_1` | Leere Hände, volles Herz (Gartenmönch): gewinne eine Gartenrunde, ohne einen Gegenstand zu benutzen. | Mains vides, cœur plein (Moine du jardin) : remportez un parcours du jardin sans utiliser d'objet. | Manos vacías, corazón lleno (Monje del jardín): gana un recorrido del jardín sin usar objetos. | Mãos vazias, coração cheio (Monge do jardim): vença um percurso do jardim sem usar itens. |
| `challenge_garden_monk_2` | Der schnelle Pfad (Gartenmönch): gewinne eine Gartenrunde in unter 25 Minuten. | Le chemin rapide (Moine du jardin) : remportez un parcours du jardin en moins de 25 minutes. | El camino veloz (Monje del jardín): gana un recorrido del jardín en menos de 25 minutos. | O caminho veloz (Monge do jardim): vença um percurso do jardim em menos de 25 minutos. |
| `challenge_garden_monk_3` | Der hohe Pfad (Gartenmönch): gewinne eine Gartenrunde auf Harmonie VI oder höher. | Le haut chemin (Moine du jardin) : remportez un parcours du jardin en Harmonie VI ou plus. | El camino alto (Monje del jardín): gana un recorrido del jardín en Armonía VI o superior. | O caminho alto (Monge do jardim): vença um percurso do jardim em Harmonia VI ou superior. |
| `challenge_shrine_archivist_1` | Das Archiv der Vier (Schrein-Archivar): beende eine Gartenrunde mit 4 Relikten. | L'archive des quatre (Archiviste du sanctuaire) : terminez un parcours du jardin avec 4 reliques. | El archivo de los cuatro (Archivista del santuario): termina un recorrido del jardín con 4 reliquias. | O arquivo dos quatro (Arquivista do santuário): termine um percurso do jardim com 4 relíquias. |
| `challenge_shrine_archivist_2` | Der schnelle Pfad (Schrein-Archivar): gewinne eine Gartenrunde in unter 25 Minuten. | Le chemin rapide (Archiviste du sanctuaire) : remportez un parcours du jardin en moins de 25 minutes. | El camino veloz (Archivista del santuario): gana un recorrido del jardín en menos de 25 minutos. | O caminho veloz (Arquivista do santuário): vença um percurso do jardim em menos de 25 minutos. |
| `challenge_shrine_archivist_3` | Der hohe Pfad (Schrein-Archivar): gewinne eine Gartenrunde auf Harmonie VI oder höher. | Le haut chemin (Archiviste du sanctuaire) : remportez un parcours du jardin en Harmonie VI ou plus. | El camino alto (Archivista del santuario): gana un recorrido del jardín en Armonía VI o superior. | O caminho alto (Arquivista do santuário): vença um percurso do jardim em Harmonia VI ou superior. |
| `challenge_koi_gambler_1` | Drei Flüche gesetzt (Koi-Spieler): gewinne eine Gartenrunde mit 3 beendeten verfluchten Rätseln. | Trois malédictions misées (Joueur de koï) : remportez un parcours du jardin en terminant 3 puzzles maudits. | Tres maldiciones apostadas (Jugador de koi): gana un recorrido del jardín terminando 3 puzles malditos. | Três maldições apostadas (Jogador de koi): vença um percurso do jardim concluindo 3 quebra-cabeças amaldiçoados. |
| `challenge_koi_gambler_2` | Der schnelle Pfad (Koi-Spieler): gewinne eine Gartenrunde in unter 25 Minuten. | Le chemin rapide (Joueur de koï) : remportez un parcours du jardin en moins de 25 minutes. | El camino veloz (Jugador de koi): gana un recorrido del jardín en menos de 25 minutos. | O caminho veloz (Jogador de koi): vença um percurso do jardim em menos de 25 minutos. |
| `challenge_koi_gambler_3` | Der hohe Pfad (Koi-Spieler): gewinne eine Gartenrunde auf Harmonie VI oder höher. | Le haut chemin (Joueur de koï) : remportez un parcours du jardin en Harmonie VI ou plus. | El camino alto (Jugador de koi): gana un recorrido del jardín en Armonía VI o superior. | O caminho alto (Jogador de koi): vença um percurso do jardim em Harmonia VI ou superior. |
| `challenge_stone_gardener_1` | Der fleißige Rechen (Steingärtner): gewinne eine Gartenrunde und benutze dabei 8 Gegenstände. | Le râteau affairé (Jardinier de pierre) : remportez un parcours du jardin en utilisant 8 objets. | El rastrillo atareado (Jardinero de piedra): gana un recorrido del jardín usando 8 objetos. | O ancinho ocupado (Jardineiro de pedra): vença um percurso do jardim usando 8 itens. |
| `challenge_stone_gardener_2` | Der schnelle Pfad (Steingärtner): gewinne eine Gartenrunde in unter 25 Minuten. | Le chemin rapide (Jardinier de pierre) : remportez un parcours du jardin en moins de 25 minutes. | El camino veloz (Jardinero de piedra): gana un recorrido del jardín en menos de 25 minutos. | O caminho veloz (Jardineiro de pedra): vença um percurso do jardim em menos de 25 minutos. |
| `challenge_stone_gardener_3` | Der hohe Pfad (Steingärtner): gewinne eine Gartenrunde auf Harmonie VI oder höher. | Le haut chemin (Jardinier de pierre) : remportez un parcours du jardin en Harmonie VI ou plus. | El camino alto (Jardinero de piedra): gana un recorrido del jardín en Armonía VI o superior. | O caminho alto (Jardineiro de pedra): vença um percurso do jardim em Harmonia VI ou superior. |
| `challenge_lantern_seer_1` | Drei Lichter zugleich (Laternenseher): besiege einen Boss, der 3 Regeln zugleich bringt. | Trois lumières à la fois (Voyant de la lanterne) : battez un boss qui impose 3 règles à la fois. | Tres luces a la vez (Vidente del farol): derrota a un jefe que trae 3 reglas a la vez. | Três luzes de uma vez (Vidente da lanterna): derrote um chefe que traz 3 regras de uma vez. |
| `challenge_lantern_seer_2` | Der schnelle Pfad (Laternenseher): gewinne eine Gartenrunde in unter 25 Minuten. | Le chemin rapide (Voyant de la lanterne) : remportez un parcours du jardin en moins de 25 minutes. | El camino veloz (Vidente del farol): gana un recorrido del jardín en menos de 25 minutos. | O caminho veloz (Vidente da lanterna): vença um percurso do jardim em menos de 25 minutos. |
| `challenge_lantern_seer_3` | Der hohe Pfad (Laternenseher): gewinne eine Gartenrunde auf Harmonie VI oder höher. | Le haut chemin (Voyant de la lanterne) : remportez un parcours du jardin en Harmonie VI ou plus. | El camino alto (Vidente del farol): gana un recorrido del jardín en Armonía VI o superior. | O caminho alto (Vidente da lanterna): vença um percurso do jardim em Harmonia VI ou superior. |
| `challenge_reed_duelist_1` | Eine stille Etage (Schilfduellant): schließe eine Etage ohne Bleistiftnotiz und ohne LP-Verlust ab. | Un étage paisible (Duelliste du roseau) : terminez un étage sans annotation au crayon et sans perdre de PV. | Un piso tranquilo (Duelista de caña): supera un piso sin marcas de lápiz y sin perder PV. | Um andar tranquilo (Duelista do junco): conclua um andar sem marcas de lápis e sem perder PV. |
| `challenge_reed_duelist_2` | Der schnelle Pfad (Schilfduellant): gewinne eine Gartenrunde in unter 25 Minuten. | Le chemin rapide (Duelliste du roseau) : remportez un parcours du jardin en moins de 25 minutes. | El camino veloz (Duelista de caña): gana un recorrido del jardín en menos de 25 minutos. | O caminho veloz (Duelista do junco): vença um percurso do jardim em menos de 25 minutos. |
| `challenge_reed_duelist_3` | Der hohe Pfad (Schilfduellant): gewinne eine Gartenrunde auf Harmonie VI oder höher. | Le haut chemin (Duelliste du roseau) : remportez un parcours du jardin en Harmonie VI ou plus. | El camino alto (Duelista de caña): gana un recorrido del jardín en Armonía VI o superior. | O caminho alto (Duelista do junco): vença um percurso do jardim em Harmonia VI ou superior. |
| `challenge_quiet_cartographer_1` | Sechs Schritte Risiko (Stiller Kartograf): gewinne eine Gartenrunde mit 6 Feldern auf dem Risikopfad. | Six pas de risque (Cartographe silencieux) : remportez un parcours du jardin en prenant 6 cases du chemin risqué. | Seis pasos de riesgo (Cartógrafo silencioso): gana un recorrido del jardín pisando 6 casillas de la ruta de riesgo. | Seis passos de risco (Cartógrafo silencioso): vença um percurso do jardim passando por 6 casas da rota de risco. |
| `challenge_quiet_cartographer_2` | Der schnelle Pfad (Stiller Kartograf): gewinne eine Gartenrunde in unter 25 Minuten. | Le chemin rapide (Cartographe silencieux) : remportez un parcours du jardin en moins de 25 minutes. | El camino veloz (Cartógrafo silencioso): gana un recorrido del jardín en menos de 25 minutos. | O caminho veloz (Cartógrafo silencioso): vença um percurso do jardim em menos de 25 minutos. |
| `challenge_quiet_cartographer_3` | Der hohe Pfad (Stiller Kartograf): gewinne eine Gartenrunde auf Harmonie VI oder höher. | Le haut chemin (Cartographe silencieux) : remportez un parcours du jardin en Harmonie VI ou plus. | El camino alto (Cartógrafo silencioso): gana un recorrido del jardín en Armonía VI o superior. | O caminho alto (Cartógrafo silencioso): vença um percurso do jardim em Harmonia VI ou superior. |
