# BaseProjectPackageInstaller

A small Unity tool that makes installing, updating and removing my [BaseProjectPackages](https://github.com/Kirschkernweitwurf/BaseProjectPackages) and any other Git package quick and painless. No need to copy-paste Git URLs one by one, just click the menu item and you're set.

**This is the recommended way to install the base packages and for most of them the only practical one.** It holds the dependency graph, so ticking one package brings in everything it needs, in an order that leaves the project compiling at every step.

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

## Why install through this rather than by hand

The base packages depend on each other, but none of them declares that in its `package.json`. A `package.json` dependency is resolved through a registry and these packages live in a Git repo instead. Declaring one would send UPM looking in a registry that does not have it and the install fails.

The graph therefore lives here, in the registry entries, where it can be resolved against Git URLs. Two things follow from that:

- **Pasting a Git URL yourself gets you one package and nothing else.** Unity compiles it against a project missing everything it needs and you get errors until you have worked out the whole chain and added it in the right order.
- **Order matters, not just membership.** Each install triggers a recompile. Arriving before its dependencies means the project is red until the last one lands. This window installs from the leaves upward so that never happens.

Ticking `UI`, for example, resolves to seven packages and installs them like this:

```
Editor UI -> Utility -> Attributes -> Services -> Tweening -> Core -> UI
```

The same holds in reverse, and the Package Manager knows about that even less. See [Uninstalling](#uninstalling).

## What it does

- Adds a single **Git Package Manager** window under `Tools > Git Package Manager` that handles installing, updating and uninstalling
- Resolves dependencies from your ticks, so you never have to remember what needs what
- Installs in dependency order, deepest first, so the project compiles after every step
- Uninstalls in the reverse order, and warns before leaving anything behind that would no longer compile
- Shows a table with each package's install status, version and what pulls it into the run
- Keeps the package list in a per-project registry you can edit under **Project Settings -> Custom Tools -> Git Packages**
- Checks that registry for missing names, missing URLs, duplicates, unknown dependencies and dependency loops
- Survives the domain reloads that package installs and removals trigger, so a run always finishes
- Offers a one-click project setup that generates a preferred `ProjectInputService`, a `PlayerInputActions` action asset and the matching auto-generated C# class

## Using the window

1. Open `Tools > Git Package Manager`
2. Pick **Install / Update** or **Uninstall** at the top
3. Tick the packages you want (or use **Select All**)
4. Click the action button

Switching mode resets the ticks to that mode's default: everything for installing, nothing for uninstalling. The whole stack should never be one click away from being removed.

In install mode the button label adapts to your selection: **Install Selected** when nothing selected is installed yet, **Update Selected** when everything selected is already installed, **Install / Update Selected** for a mix and **Nothing Selected** when there is nothing to do. Installing and updating are the same operation under the hood: each package is re-resolved as a Git dependency, so missing packages get installed and present ones get pulled to the latest remote version.

In uninstall mode it reads **Uninstall Selected**, or **Nothing Selected** when nothing installed is ticked. Packages that are not installed are dimmed and cannot be ticked, because there is nothing to take away.

**Refresh** re-checks install statuses and pulls in any new or changed default packages. **Edit List** jumps to the registry in Project Settings.

## Dependencies

**Resolve Dependencies** is on by default and is what you want almost always. Both modes read the same graph, just in opposite directions.

Installing, with it on:

- Ticking a package ticks everything it needs, all the way down.
- Unticking it releases what it pulled in, unless something else still ticked needs that, or you ticked it yourself.
- The run is ordered so every package lands in a project where its dependencies are already present.

The third column is then headed **Required By** and names the ticked packages that require each row.

Turn the toggle **off** to take your ticks exactly as they are. That is for updating one already-installed package on its own without dragging its whole chain into the run.

The column is filled whether the toggle is on or off, so with it off you can still see what a pick would have pulled in before deciding whether leaving it out is safe.

## Uninstalling

UPM knows nothing about how these packages relate, for the same reason the install side exists: none of it is in `package.json`. Removing `Core` from the Package Manager while `UI`, `Settings System` and `Memory Profiler` are installed leaves the project red with no warning at all. That is the same blind spot, pointing the other way.

So in **Uninstall** mode the graph is walked upwards:

- Ticking a package ticks everything that depends on it, all the way up.
- The third column is headed **Depends On** and names the ticked packages each row is being removed for.
- The run is ordered dependents first, so nothing is ever left referencing something that is already gone.

Ticking `Core` therefore resolves to six packages and removes them like this:

```
Content -> UI -> Settings System -> Memory Profiler -> Controller Support -> Core
```

Turn **Resolve Dependencies** off to remove exactly what you ticked. Anything left installed that still depends on the selection is then listed in a warning above the button, whether it depends on it directly or through another package, so leaving it out is a decision rather than a surprise.

Uninstalling always asks for confirmation first, listing what will go and repeating that warning if there is one. It is the one action here that pressing the button again does not undo.

## The window UI

The package table lives in a rounded card with alternating row striping and a colored status pill per package: green for **Installed**, grey for **Not installed**, with the installed version next to it. The look adapts to both the dark and light editor skins.

- **Resizable columns**: drag the divider lines between the columns to resize them. The dividers span the whole table, so you can grab them at any row and the widths are remembered across sessions.
- **Copy or clear the result**: after a run, the result summary appears at the bottom, with a **Copy** button that puts the whole report on the clipboard and a **Clear** button to dismiss it.

All spacings, sizes and colors are defined in a single theme class, so the look can be tuned in one place.

## The package registry

The list of available packages is stored per project in `ProjectSettings/BasePackageRegistry.asset`, so it can be version controlled and edited per project. It is seeded with the default base packages on first use; after that you can add, remove or rename entries under **Project Settings -> Custom Tools -> Git Packages**. New or changed defaults are merged in on **Refresh** without discarding your project-specific entries.

Each entry has a name, a Git URL and the names of the entries it directly needs. Only direct dependencies are listed; the rest of the chain is walked for you. Dependencies are matched by name, so renaming an entry means updating everything that names it. The settings page reports it when something is off: an entry with no name or no URL, a name listed twice, a dependency that is not in the list, an entry depending on itself, or two entries depending on each other.

Any Git package works here, not just mine. Add your own entries and they get the same dependency handling in both directions.

## Logging and status

The window reports clearly what is going on:

- A live status line shows which package is being processed.
- Each package logs its result to the Console with the resolved name and version, for example:
    - `Installed Tools 1.2.0.`
    - `Updated UI 1.1.0 -> 1.2.0.`
    - `Core is already up to date (1.0.4).`
    - `Removed UI (1.2.0).`
- If a package runs into a problem, the run **does not stop**. Remaining packages are still processed, the failure is logged as a warning and the final status box shows a short summary like `Done. 5 ok, 1 failed.` or `Done. 6 removed.` followed by a per-package breakdown.
- A package install or removal can trigger a script recompile and domain reload mid-run. Progress is persisted and the run resumes automatically where it left off, in either mode.
- A reload can land between the package manager applying a change and the run recording it, so the resumed run asks for that one package a second time. Any failure is therefore checked against the project before it is reported, and a removal whose package is already gone counts as done rather than as an error.

## Included packages

The default registry contains the following. **Needs** lists direct dependencies only; the installer walks the rest.

| Package | Description | Needs |
|---|---|---|
| `Attributes` | Custom attributes for the inspector and more | Editor UI, Utility |
| `Content` | Manager prefabs and configured assets wiring the other packages together | Controller Support, Save System, Settings System, UI |
| `Controller Support` | Gamepad navigation and input glyphs | Core |
| `Core` | Core systems (menus, audio, scene loading, timers) | Tweening |
| `Editor UI` | Shared styling and widgets for editor tooling | nothing |
| `Localization` | Localization support | Utility |
| `Memory Profiler` | Memory profiling tools | Core |
| `Save System` | Saving and loading game data | Services |
| `Services` | Service locator and service lifecycle | Attributes |
| `Settings System` | Game settings management | Core |
| `Tools` | General-purpose editor tools | Attributes |
| `Tweening` | Tweening and easing helpers | Services |
| `UI` | UI helpers and menu management | Core |
| `Utility` | Common utilities | nothing |

All of these live in the [BaseProjectPackages](https://github.com/Kirschkernweitwurf/BaseProjectPackages) repo.

## Why?

I built this so I can spin up new Unity projects with my full stack in seconds. Feel free to use it for your own projects too.
