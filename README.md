# BasePackageInstaller

A Unity editor window that installs and updates my [BaseProjectPackages](https://github.com/Kirschkernweitwurf/BaseProjectPackages), and any other Git package, without pasting URLs one by one.

**This is the recommended way to install the base packages and for most of them the only practical one.** It holds the dependency graph, so ticking one package brings in everything it needs, in an order that leaves the project compiling at every step.

## Installation

1. Open your project in Unity
2. Open the **Package Manager**
3. Click **+** and select **Install package from git URL**
4. Paste:
   ```
   https://github.com/Kirschkernweitwurf/BasePackageInstaller.git
   ```
5. Open `Tools > Installer > Git Package Manager`

## Why install through this rather than by hand

The base packages depend on each other, but none of them declares that in its `package.json`. A `package.json` dependency is resolved through a registry and these packages live in a Git repo instead. Declaring one would send UPM looking in a registry that does not have it and the install fails.

The graph therefore lives here, in the registry entries, where it can be resolved against Git URLs. Two things follow from that:

- **Pasting a Git URL yourself gets you one package and nothing else.** Unity compiles it against a project missing everything it needs and you get errors until you have worked out the whole chain and added it in the right order.
- **Order matters, not just membership.** Each install triggers a recompile. Arriving before its dependencies means the project is red until the last one lands. This window installs from the leaves upward so that never happens.

Ticking `UI`, for example, resolves to seven packages and installs them like this:

```
Editor UI -> Utility -> Attributes -> Services -> Tweening -> Core -> UI
```

## Using the window

1. Open `Tools > Installer > Git Package Manager`
2. Tick the packages you want (or use **Select All**)
3. Click the action button

Installing and updating are the same operation under the hood: each package is re-resolved as a Git dependency, so missing packages get installed and present ones get pulled to the latest remote version. The button label follows the selection: **Install Selected**, **Update Selected**, **Install / Update Selected** for a mix, **Nothing Selected** when there is nothing to do.

**Refresh** re-checks install statuses and pulls in any new or changed default packages. **Edit List** jumps to the registry in Project Settings.

The table shows each package with its install status, installed version and the ticked packages that require it. Columns are resizable by dragging the dividers, which span the whole table so they can be grabbed at any row, and the widths are remembered across sessions.

## Dependency resolution

**Resolve Dependencies** is on by default and is what you want almost always. With it on:

- Ticking a package ticks everything it needs, all the way down.
- Unticking it releases what it pulled in, unless something else still ticked needs that, or you ticked it yourself.
- A row another selected package holds is drawn dimmed with a locked toggle, so the lock never needs explaining.
- The run is ordered so every package lands in a project where its dependencies are already present.

Turn it **off** to take your ticks exactly as they are, which is what you want for updating one already-installed package without dragging its whole chain into the run. The **Required By** column is filled either way, so with the toggle off you can still see what a pick would have pulled in before deciding whether leaving it out is safe.

## The package registry

The package list lives per project in `ProjectSettings/BasePackageRegistry.asset`, so it can be version controlled and edited per project. It is seeded with the default base packages on first use; after that you can add, remove or edit entries under **Project Settings -> Base Tools -> Git Packages**. New or changed defaults are merged in on **Refresh** without discarding your project-specific entries.

Each entry has a name, a Git URL and the names of the entries it directly needs. Only direct dependencies are listed; the rest of the chain is walked for you. Dependencies are matched by name, so renaming an entry means updating everything that names it.

The settings page validates the registry and reports the mistakes that are otherwise invisible, because the resolver answers all of them by quietly doing nothing: an entry with no name or no URL, a name listed twice, a dependency that is not in the list, an entry depending on itself, and two entries depending on each other.

Any Git package works here, not just mine. Add your own entries and they get the same dependency handling.

## Generating the defaults

`Tools > Installer > Package Defaults` regenerates the seeded list from the assembly definitions in
the packages repository. It reads them off disk by path, so the packages do not have to be installed
for it to run, and nothing it needs ships to a consuming project.

Point it at the packages root and press **Scan**. It reads every asmdef, resolves the references
between packages, and drops the edges another edge already implies, so the list stays the direct
dependencies rather than the whole closure. Assemblies behind a define constraint and test assemblies
are left out, so an optional integration never becomes a hard dependency.

Three tabs: the resolved graph, the file it would generate, and a diff against the file on disk. The
pill beside the target says whether the two already match. **Write File** only writes when they do
not.

This exists because the installer cannot work the graph out at runtime. It has to know what a package
needs before that package is anywhere on disk. Generating the list here and checking the result in
keeps the asmdefs the single source of truth.

The same run happens on its own the first time the project is opened, so the checked in file cannot
quietly fall behind the asmdefs. It only writes when the result differs, only when the packages root
exists, and never into the package cache, so a consuming project never sees it. The **Run On Project
Open** toggle in the window turns it off.

## Project setup

When the project has no input service yet, the window offers **Create ProjectInputService**. It creates `Assets/Input/PlayerInputActions.inputactions` (moving an existing action asset rather than creating a second one), turns on wrapper class generation for it, and writes `Assets/Generated/Input/ProjectInputService.cs` from a template. The button disappears once both files exist.

## Logging and status

- A live status line shows which package is being processed.
- Each package logs its result to the Console with the resolved name and version, for example `Installed Tools 1.2.0.`, `Updated UI 1.1.0 -> 1.2.0.` or `Core is already up to date (1.0.4).`
- A failure does not stop the run. Remaining packages are still processed, the failure is logged as a warning, and the final status box shows a summary like `Done. 5 ok, 1 failed.` followed by a per-package breakdown.
- A package install can trigger a script recompile and domain reload mid-run. Progress is persisted in `SessionState` and the run resumes automatically where it left off.

## Included packages

The default registry contains the fourteen packages of the [BaseProjectPackages](https://github.com/Kirschkernweitwurf/BaseProjectPackages) repo. That repo's README lists them with what each one does and what it directly needs.

## Requirements

- Unity `6000.3` or newer
- `com.unity.inputsystem` `1.19.0`, for the project input setup

No Base package is required. This is the tool that installs them, so in a fresh project it has to compile before any of them exists, which is why it carries its own theme and table code rather than using `Base.EditorUIPackage.Editor`.

## Why?

I built this so I can spin up new Unity projects with my full stack in seconds. You are welcome to use it in your own projects too.

## License

[PolyForm Shield 1.0.0](https://polyformproject.org/licenses/shield/1.0.0). Use it in whatever you
build, including commercial work. The one thing it does not allow is building something that competes
with these packages, so you cannot repackage them and sell them as your own library.