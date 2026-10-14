# Git Packages List

🔧 **Tool** · programmers only

**Tags:** #programmer #setup

The list of packages the [🔧 Git Package Manager](Git%20Package%20Manager.md) offers. Edit it here to add your own Git packages or remove ones this project does not need.

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
- New or changed defaults are merged in on their own the next time the list is read, after the installer updates. Entries you added yourself stay. A default you removed stays removed until the defaults change again.
- **Refresh** in the window does the same merge right away.
- Any Git package works here, not just the Base ones.
