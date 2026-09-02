# Changelog

All notable changes to this package. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions are the ones this package
actually shipped under, read back from `package.json` at each commit that changed it.

## [1.4.4] - 2026-09-02

### Added

- An editor test suite for the dependency graph. Covers the effective selection in both directions,
  with expansion on and off, the install and removal order, the packages a removal leaves behind
  broken, and every registry validation rule.
- A check on the shipped defaults themselves: they have to validate, name only dependencies the list
  holds, and resolve into an install order that satisfies every edge. A bad generation run is caught
  here rather than in a project that has already installed it.

## [1.3.8] - 2026-08-25

### Changed

- Project input setup flow improved.
- README rewritten with installation and dependency detail.

## [1.3.6] - 2026-08-23

### Added

- Uninstall mode. The window now walks the dependency graph in both directions: installing a package
  pulls in what it needs, removing one pulls in what needs it, and each runs in an order that leaves
  the project compiling at every step.
- A confirmation dialog before a removal, listing what goes and what would be left behind broken.

## [1.3.5] - 2026-08-20

### Added

- The Content package added to the default registry.

### Changed

- The Editor folder restructured into Data, Operations, Window and ProjectInput, with the persistence
  layer split out from the operations that use it.

### Fixed

- Duplicate code removed.

## [1.3.1] - 2026-08-18

### Changed

- Base package dependencies now resolved here rather than in `package.json`. UPM would try to resolve
  a Base-to-Base dependency through a registry that does not have it, so the graph lives in
  `PackageEntry.DependsOn` instead.

## [1.3.0] - 2026-07-29

### Changed

- Code cleanup pass.

## [1.2.2] - 2026-07-22

### Changed

- Visual overhaul of the installer window: rounded cards, status pills, a resizable table and a
  segmented mode switch.
- The install button is disabled in a Base package development project.
- Performance improvements.

## [1.2.0] - 2026-07-08

### Changed

- The Systems package renamed to Core.

### Fixed

- The namespace in the generated project input service.
- `ProjectInputServiceCodeTemplate` brought up to the current Attributes package.

## [1.1.4] - 2026-06-20

### Changed

- Defaults can be updated without discarding project-specific registry entries.
- Controller Support and Localization added to the defaults.
- Install status shown per package.
- Packages sorted alphabetically.
- The package list reduced to a single window.
- Editor-only platform include.

## [1.1.0] - 2026-06-03

### Changed

- MenuItem sorting priority.
- The project input service section hides itself once the setup is complete.

## [1.0.5] - 2026-05-23

### Added

- The package updater window, with selectable packages.

### Changed

- Progress persisted across the domain reload an install triggers, so a run resumes where it left off.
- Better logging and error handling.

## [1.0.0] - 2026-05-18

### Added

- First release. Installs Base packages from Git URLs in one action.
- One-click generation of the project-specific input action map class and its service.