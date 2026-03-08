# Unity Installation & Setup Guide (Windows)

This guide walks you through the process of setting up Unity and preparing your machine for developing the Run of the Nine (Unity Sudoku roguelike) project.

## 1) Prerequisites

To start, ensure your machine meets the following requirements:

- Operating System: Windows 10/11 (64-bit)
- Admin rights for installation
- Stable internet connection
- At least 20 GB free disk space

Recommended:
- 16 GB RAM
- SSD storage

## 2) Install Unity Hub

Unity Hub is a unified platform for managing and scheduling your projects. Here's how to install it:

1. Go to `https://unity.com/download`
2. Download ÔÇ£Unity Hub for WindowsÔÇØ
3. Run the installer and follow the setup instructions
4. Sign in to your Unity account (create one if needed)

## 3) Install Unity Editor (LTS)

Unity's latest Long Term Support (LTS) version is recommended for this project. To install it:

1. Open Unity Hub
2. Navigate to ÔÇ£InstallsÔÇØ -> ÔÇ£Install EditorÔÇØ
3. Choose the latest LTS version (preferably Unity 6 LTS)
4. In modules, select:
    - Microsoft Visual Studio Community (if not already installed)
    - Windows Build Support (IL2CPP)
    - Documentation (optional)
5. Install and wait for completion

## 4) Install IDE (Choose One)

There are two options for IDEs that work well with Unity:

## Option A: Visual Studio 2022 (recommended for Unity)

- Install workload: ÔÇ£Game development with UnityÔÇØ
- Ensure ÔÇ£.NET desktop toolsÔÇØ are included

## Option B: VS Code

To install:
- ÔÇ£.NET SDKÔÇØ (8 or newer)
- ÔÇ£C#ÔÇØ extension (`ms-dotnettools.csharp`)
- Unity extension (`Visual Studio Editor` package in project)

## 5) Create Project

To start the project, follow these steps:

1. Unity Hub -> ÔÇ£ProjectsÔÇØ -> ÔÇ£New ProjectÔÇØ
2. Template: ÔÇ£2D (URP)ÔÇØ
3. Project name: `SudokuRoguelike`
4. Location: your workspace folder
5. Create project

## 6) Project Settings (Initial)

Before starting development, make sure to set up your project settings:

1. ÔÇ£EditÔÇØ -> ÔÇ£Project SettingsÔÇØ -> ÔÇ£PlayerÔÇØ
    - Company/Product names
    - Set default orientation (landscape or portrait as desired)
2. ÔÇ£Project SettingsÔÇØ -> ÔÇ£Input System PackageÔÇØ
    - Active Input Handling: `Both` (or `Input System Package`)
3. ÔÇ£Project SettingsÔÇØ -> ÔÇ£QualityÔÇØ
    - Keep default for prototype
4. ÔÇ£Project SettingsÔÇØ -> ÔÇ£TimeÔÇØ
    - Default is fine unless implementing deterministic replay

## 7) Required Packages

Open ÔÇ£WindowÔÇØ -> ÔÇ£Package ManagerÔÇØ and confirm the following packages are installed:

- Input System
- 2D Sprite
- 2D Tilemap
- TextMeshPro
- (Optional) Cinemachine
- (Optional) Addressables

## 8) Unity + Git Setup

To enable version control for your Unity project:

1. Ensure `.gitignore` includes Unity ignores
2. Enable ÔÇ£Visible Meta FilesÔÇØ:
    - ÔÇ£EditÔÇØ -> ÔÇ£Project SettingsÔÇØ -> ÔÇ£EditorÔÇØ
    - Version Control Mode: `Visible Meta Files`
3. Asset Serialization Mode: `Force Text`

## 9) First Run Validation Checklist

- Unity project opens without compile errors
- `Assets/Scenes/MainMenu.unity` created and opens
- Play mode enters and exits cleanly
- New script compiles after save

## 10) Common Install Problems

- ÔÇ£Hub cannot detect editorÔÇØ: reinstall Unity Hub as admin
- ÔÇ£Compiler errors immediatelyÔÇØ: install correct ÔÇ£.NET/IDEÔÇØ workloads
- ÔÇ£Missing module for Windows buildÔÇØ: add module from Hub installs panel
- ÔÇ£Slow importÔÇØ: move project to SSD and exclude folder from antivirus scans

## 11) Suggested Next Steps for This Game

1. Create folder structure based on the architecture doc
2. Implement Sudoku board model + validator first
3. Add HP/Pencil/Gold HUD bindings
4. Build one complete loop: play -> reward -> next level


