# Achievements

**Version:** 1.0 | **Date:** 2026-10-26 | **Status:** implemented (game side; the 21 new achievements still have to be created in Steamworks)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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
| `floor_5` | Summit in Sight | Reach Floor 5 in a Garden Run. | `FloorReached` ≥ 5 |
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
| `daily_streak_7` | Seven Dawns | Keep a Daily Walk streak for 7 days. | `DailyGoalState.CurrentStreak` ≥ 7 at a Garden Run's end |
| `codex_half` | Half the Collection | Find half of everything in the Codex. | Items, Relics, Rules and Curses together: found ≥ half |
| `codex_all` | The Complete Codex | Find everything in the Codex. | every Codex page complete |

## 3. Requirements

| ID | Requirement |
|---|---|
| ACHIEVE-RUN-001 | At every Garden Run's end (`ProfileService.RecordRunAndGetNewUnlocks`, after XP, unlocks and Codex finds are counted) the game grants the run achievements of §2 whose condition holds. Tutorials, custom puzzles and the other modes grant none of them. |
| ACHIEVE-CLASS-001 | At the same point the class achievements of §2 are checked; a won Garden Run adds its class to `ClassesWithAWin` (old saves start empty). |
| ACHIEVE-MODE-001 | The results of Endless Zen, Spirit Trials and the Monthly Walk check their achievement before they save (`EndScreenViewController`); a new achievement is saved even when the result is not a new best. |
| ACHIEVE-COLLECT-001 | The Daily Walk streak and the Codex achievements are checked at every Garden Run's end. |

Every grant is kept in the profile (`MasteryAchievementState.UnlockedAchievements`) and sent to Steam when Steam runs; unlocks made without Steam are sent at the next start with Steam (`SyncUnlockedAchievements`). An API name Steamworks does not know yet fails quietly (a warning in the log) and is sent again later.

## 4. Steamworks (owner steps, before the build that carries them goes live)

Steamworks → App 4858470 → Stats & Achievements → Achievements: one new achievement per row of §2 with the API name, name, description, hidden = no, and the two icons (`steam_assets/achievements/achieved_<api name>.jpg` and `unachieved_<api name>.jpg`, 256×256). Then publish (stop and check before the Publish button, as always). The texts in the other four languages are in §5 (Steamworks → the achievement → Localization).

## 5. Names and descriptions in the other languages

Name and description, separated by the colon; the mode and screen names are the game's own (Gartenrunde, Parcours du jardin, Recorrido del jardín, Percurso do jardim; Etage, Étage, Piso, Andar; LP and PV).

| API name | Deutsch | Français | Español | Português (Brasil) |
|---|---|---|---|---|
| `first_puzzle` | Der erste Stein: Löse dein erstes Rätsel. | La première pierre : résolvez votre premier puzzle. | La primera piedra: resuelve tu primer puzle. | A primeira pedra: resolva seu primeiro quebra-cabeça. |
| `first_boss` | Das Tor öffnet sich: Besiege deinen ersten Boss. | La porte s'ouvre : battez votre premier boss. | La puerta se abre: derrota a tu primer jefe. | O portão se abre: derrote seu primeiro chefe. |
| `floor_5` | Der Gipfel in Sicht: Erreiche Etage 5 in einer Gartenrunde. | Le sommet en vue : atteignez l'étage 5 dans un parcours du jardin. | La cima a la vista: llega al piso 5 en un recorrido del jardín. | O cume à vista: chegue ao andar 5 em um percurso do jardim. |
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
