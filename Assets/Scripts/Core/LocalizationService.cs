using System.Collections.Generic;

namespace SudokuRoguelike.Core
{
    public static class LocalizationService
    {
        private static LanguageOption _current = LanguageOption.English;

        private static readonly Dictionary<string, string> De = new Dictionary<string, string>
        {
            // Main Menu
            { "Start Game", "Spiel starten" },
            { "Resume Game", "Fortsetzen" },
            { "Tutorial", "Anleitung" },
            { "Meta Progression", "Fortschritt" },
            { "Game Modes", "Spielmodi" },
            { "Items", "Gegenstände" },
            { "Options", "Einstellungen" },
            { "Credits", "Mitwirkende" },
            { "Quit", "Beenden" },
            { "Ready.", "Bereit." },

            // Class Select
            { "Select Class", "Klasse wählen" },
            { "Continue", "Weiter" },
            { "Back", "Zurück" },
            { "Allow Irregular Puzzles", "Unregelmäßige Rätsel erlauben" },
            { "Class not yet unlocked.", "Klasse noch nicht freigeschaltet." },

            // Options
            { "Audio", "Audio" },
            { "Master Volume", "Lautstärke" },
            { "Music Volume", "Musiklautstärke" },
            { "SFX Volume", "Effektlautstärke" },
            { "UI Volume", "UI-Lautstärke" },
            { "Mute when unfocused", "Stumm im Hintergrund" },
            { "Display", "Anzeige" },
            { "Fullscreen", "Vollbild" },
            { "Language", "Sprache" },
            { "Gameplay", "Spielablauf" },
            { "Highlight Errors", "Fehler hervorheben" },
            { "Accessibility", "Barrierefreiheit" },
            { "Colorblind Mode", "Farbenblind-Modus" },
            { "High Contrast", "Hoher Kontrast" },
            { "Reduce Motion", "Bewegungen reduzieren" },
            { "Alt Constraint Symbols", "Altern. Symbole" },
            { "Controls", "Steuerung" },

            // Class names
            { "Number Freak",       "Zahlen-Freak" },
            { "Garden Monk",        "Gartenmönch" },
            { "Shrine Archivist",   "Schrein-Archivar" },
            { "Koi Gambler",        "Koi-Spieler" },
            { "Stone Gardener",     "Steingärtner" },
            { "Lantern Seer",       "Laternenseher" },
            { "Reed Duelist",       "Schilfduellant" },
            { "Quiet Cartographer", "Stiller Kartograf" },

            // Tutorial
            { "Tutorial Setup", "Anleitung einrichten" },
            { "Board Size", "Feldgröße" },
            { "Difficulty", "Schwierigkeit" },
            { "Region Layout", "Regionslayout" },
            { "Resource Mode", "Ressourcenmodus" },
            { "Free (∞ HP/Pencil)", "Frei (∞ LP/Stift)" },
            { "Class-Based", "Klassenbasiert" },
            { "Class:", "Klasse:" },
            { "Sudoku Modes", "Sudoku-Modi" },
            { "Start Puzzle", "Rätsel starten" },

            // Game Modes
            { "Start Garden Run", "Gartenrunde starten" },
            { "Start Endless Zen", "Endloses Zen starten" },
            { "Start Spirit Trials", "Geisterprüfungen starten" },

            // Meta / Items
            { "Meta Progression Panel", "Fortschrittstafel" },
            { "Items Codex", "Gegenstandskodex" },

            // In-game
            { "HP", "LP" },
            { "Pencil", "Bleistift" },
            { "Passive", "Passiv" },
            { "Item Slots", "Gegenstandspl." },
            { "Locked", "Gesperrt" },
            { "Not Started", "Nicht begonnen" },
            { "Level", "Stufe" },
            { "Prestige", "Prestige" },

            // Misc
            { "No run to resume.", "Kein Spiel zum Fortsetzen." },
            { "Debug: All features enabled.", "Debug: Alles freigeschaltet." },
            { "Debug mode off.", "Debug-Modus aus." },
        };

        public static void SetLanguage(LanguageOption lang)
        {
            _current = lang;
        }

        public static LanguageOption Current => _current;

        public static string T(string key)
        {
            if (_current == LanguageOption.English) return key;
            return De.TryGetValue(key, out var val) ? val : key;
        }
    }
}
