# Installer Package

📦 **Package** · reusable, works in any of our projects

A small Unity tool for installing and updating the Base packages, and any other Git package, without copying Git URLs by hand.

Everything happens in one editor window. Tick what you need, click once, done.

## What's inside

| Tool | What it does |
| --- | --- |
| [🔧 Git Package Manager](Tools/Git%20Package%20Manager.md) | The main window. Shows every package with its status and version, and installs or updates the ones you tick. |
| [🔧 Git Packages List](Tools/Git%20Packages%20List.md) | The per-project list of packages the window offers. Add your own Git URLs here. |
| [🔧 Project Input Service Setup](Tools/Project%20Input%20Service%20Setup.md) | One click to create the input action asset and input service in a new project. |

## Installation

| Step | What to do |
| --- | --- |
| 1   | Open the project and go to **Window › Package Manager**. |
| 2   | Click **+** and pick **Install package from git URL**. |
| 3   | Paste `https://github.com/Kirschkernweitwurf/BasePackageInstaller.git` and hit Enter. |

## Included packages

The list starts with the 16 Base packages and can be edited per project. Dependencies are ticked automatically.

| Package | What it covers |
| --- | --- |
| Attributes | Inspector attributes that replace the default inspector |
| Audio | Audio manager, audio containers, pooled sources |
| Content | Ready-made prefabs and assets that wire the packages together |
| Controller Support | Gamepad navigation, input glyphs, rumble |
| Core | Menus, scenes, timers, state machines, input, pooling |
| Core Debug | In-game debug menu, cheat console, debug drawing |
| Editor UI | The shared look of all Base editor windows |
| Localization | Google Sheets sync for String Tables |
| Memory Profiler | Automatic memory snapshots |
| Save System | Slot-based saving and loading |
| Services | Service locator, bootstrapper, shutdown order |
| Settings | Game settings with ready-made UI |
| Tools | Editor tools: project health, generators, menu manager |
| Tweening | Tween components and tween assets |
| UI | Buttons, confirmation dialog, small UI helpers |
| Utility | Collections, logging, helpers used by everything else |

## Next

| Section | What you find there |
| --- | --- |
| [🧰 Tools](Tools.md) | Every tool, where to find it and what each button does |

## Reading these docs

Every page shows its icon and who it is for right under the title. Links to it carry the same icon:

| Area | Everyone | Programmers only |
| --- | --- | --- |
| Components 🗳️ | 🔖 | 🗞️ |
| Scriptable Objects 📬 | 💌 | ✉️ |
| Tools 🧰 | 🪛 | 🔧 |

Menu paths are defaults. They can be moved with the Menu Item Manager in the Tools package, so your project may differ.

Something wrong or missing? [Open an issue](https://github.com/Kirschkernweitwurf/BasePackageInstaller/issues). Half a sentence is enough.
