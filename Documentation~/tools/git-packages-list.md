# Git Packages List

The list of packages the [🔧 Git Package Manager](git-package-manager.md) offers. Edit it here to add your own Git packages or remove ones this project does not need.

## Where to find it

**Project Settings › Base Tools › Git Packages**, or the **Edit List** button in the Git Package Manager window.

## Fields per entry

| Field | What it does |
| --- | --- |
| Name | The label shown in the package table. Pick anything readable. |
| URL | The Git URL that gets added as a dependency. |

## Good to know

- The list is stored per project in `ProjectSettings/BasePackageRegistry.asset`, so it goes into version control and everyone on the project sees the same list.
- The default Base packages are filled in the first time you use the tool.
- **Refresh** in the window merges in new or changed defaults without throwing away entries you added yourself.
- Any Git package works here, not just the Base ones.
