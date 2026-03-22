# Unity Installation & Setup Guide (Windows)

This guide installs Unity and prepares your machine for developing this project.

## 1) Prerequisites

- Windows 10/11 (64-bit)
- Admin rights for installation
- Stable internet connection
- At least 20 GB free disk space

Recommended:
- 16 GB RAM
- SSD storage

## 2) Install Unity Hub

1. Go to `https://unity.com/download`
2. Download **Unity Hub for Windows**
3. Run installer and finish setup
4. Sign in with a Unity account (create one if needed)

## 3) Install Unity Editor (LTS)

1. Open Unity Hub
2. Go to **Installs** -> **Install Editor**
3. Choose latest **LTS** version (prefer Unity 6 LTS)
4. In modules, select:
   - Microsoft Visual Studio Community (optional if already installed)
   - Windows Build Support (IL2CPP)
   - Documentation (optional)
5. Install and wait for completion

## 4) Install IDE (Choose One)

## Option A: Visual Studio 2022 (recommended for Unity)

- Install workload: **Game development with Unity**
- Ensure .NET desktop tools are included

## Option B: VS Code

Install:
- .NET SDK (8 or newer)
- C# extension (`ms-dotnettools.csharp`)
- Unity extension (`Visual Studio Editor` package in project)

## 5) Create Project

1. Unity Hub -> **Projects** -> **New Project**
2. Template: **2D (URP)**
3. Project name: `SudokuRoguelike`
4. Location: your workspace folder
5. Create project

## 6) Project Settings (Initial)

In Unity editor:

1. **Edit -> Project Settings -> Player**
   - Company/Product names
   - Set default orientation (landscape or portrait as desired)
2. **Project Settings -> Input System Package**
   - Active Input Handling: `Both` (or `Input System Package`)
3. **Project Settings -> Quality**
   - Keep default for prototype
4. **Project Settings -> Time**
   - Default is fine unless implementing deterministic replay

## 7) Required Packages

Open **Window -> Package Manager** and confirm:

- Input System
- 2D Sprite
- 2D Tilemap
- TextMeshPro
- (Optional) Cinemachine
- (Optional) Addressables

## 8) Unity + Git Setup

1. Ensure `.gitignore` includes Unity ignores
2. Enable **Visible Meta Files**:
   - Edit -> Project Settings -> Editor
   - Version Control Mode: `Visible Meta Files`
3. Asset Serialization Mode: `Force Text`

## 9) First Run Validation Checklist

- Unity project opens without compile errors
- `Assets/Scenes/MainMenu.unity` created and opens
- Play mode enters and exits cleanly
- New script compiles after save

## 10) Common Install Problems

- **Hub cannot detect editor**: reinstall Hub as admin
- **Compiler errors immediately**: install correct .NET/IDE workloads
- **Missing module for Windows build**: add module from Hub installs panel
- **Slow import**: move project to SSD and exclude folder from antivirus scans

## 11) Suggested Next Steps for This Game

1. Create folder structure from architecture doc
2. Implement Sudoku board model + validator first
3. Add HP/Pencil/Gold HUD bindings
4. Build one complete loop: play -> reward -> next level