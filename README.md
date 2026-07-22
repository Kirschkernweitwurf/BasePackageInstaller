# BaseProjectPackageInstaller

A small Unity tool that makes installing and updating my [BaseProjectPackages](https://github.com/Kirschkernweitwurf/BaseProjectPackages) and any other Git package quick and painless. No need to copy-paste Git URLs one by one, just click the menu item and you're set.

## Installation

1. Open your project in Unity
2. Open the **Package Manager**
3. Click **+** and select **Install package from git URL**
4. Paste:
   ```
   https://github.com/Kirschkernweitwurf/BasePackageInstaller.git
   ```
5. Hit **Enter**
6. Enjoy

## What it does

- Adds a single **Git Package Manager** window under `Tools > Git Package Manager` that handles both installing and updating
- Shows a table with each package's current install status and version
- Lets you tick exactly which packages to install or update
- Keeps the package list in a per-project registry you can edit under **Project Settings → Custom Tools → Git Packages**
- Survives the domain reloads that package installs trigger, so a run always finishes
- Offers a one-click project setup that generates a preferred `ProjectInputService`, a `PlayerInputActions` action asset, and the matching auto-generated C# class

## Using the window

1. Open `Tools > Git Package Manager`
2. Tick the packages you want (or use **Select All**)
3. Click the action button

The button label adapts to your selection: **Install Selected** when nothing selected is installed yet, **Update Selected** when everything selected is already installed, and **Install / Update Selected** for a mix. Installing and updating are the same operation under the hood: each package is re-resolved as a Git dependency, so missing packages get installed and present ones get pulled to the latest remote version.

**Refresh** re-checks install statuses and pulls in any new or changed default packages. **Edit List** jumps to the registry in Project Settings.

## The package registry

The list of available packages is stored per project in `ProjectSettings/BasePackageRegistry.asset`, so it can be version controlled and edited per project. It is seeded with the default base packages on first use; after that you can add, remove or rename entries under **Project Settings → Custom Tools → Git Packages**. New or changed defaults are merged in on **Refresh** without discarding your project-specific entries.

## Logging and status

The window reports clearly what is going on:

- A live status line shows which package is being processed.
- Each package logs its result to the Console with the resolved name and version, for example:
  - `Installed Tools 1.2.0.`
  - `Updated UI 1.1.0 → 1.2.0.`
  - `Core is already up to date (1.0.4).`
- If a package runs into a problem, the run **does not stop**. Remaining packages are still processed, the failure is logged as a warning and the final status box shows a short summary like `Done. 5 ok, 1 failed.` followed by a per-package breakdown.
- A package install can trigger a script recompile and domain reload mid-run. Progress is persisted and the run resumes automatically where it left off.

## Included packages

The default registry contains the following:

| Package | Description |
|---|---|
| `Tools` | General-purpose editor tools |
| `Attributes` | Custom attributes for the inspector and more |
| `Core` | Core systems (ServiceLocator, GameActions, Tweening, etc.) |
| `UI` | UI helpers and menu management |
| `Utility` | Common utilities |
| `Save System` | Saving and loading game data |
| `Settings System` | Game settings management |
| `Localization` | Localization support |
| `Memory Profiler` | Memory profiling tools |
| `Controller Support` | Gamepad navigation and input glyphs |

All of these live in the [BaseProjectPackages](https://github.com/Kirschkernweitwurf/BaseProjectPackages) repo.

## Why?

I built this so I can spin up new Unity projects with my full stack in seconds. Feel free to use it for your own projects too.