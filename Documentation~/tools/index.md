# Tools

The editor tools that come with the **Base Package Installer**. They run in the Unity Editor only and never ship with the game.

## Available tools

| Tool | Where | What it does |
| --- | --- | --- |
| [🔧 Git Package Manager](git-package-manager.md) | `Tools > Installer > Git Package Manager` | Installs and updates Git packages from one window |
| [🔧 Git Packages List](git-packages-list.md) | Project Settings › Base Tools › Git Packages | The list of packages the window offers |
| [🔧 Project Input Service Setup](project-input-service-setup.md) | Inside the Git Package Manager window | Creates the input asset and input service for a new project |

`Tools > Installer > Package Defaults` is a maintainer tool that regenerates the default package list from the packages repository. Team members do not need it.

## Typical order

| Step | What to do |
| --- | --- |
| 1   | Open the window and check the package list is right for this project. |
| 2   | Tick the packages you need and run install or update. |
| 3   | On a fresh project, run Project Input Service Setup once. |
