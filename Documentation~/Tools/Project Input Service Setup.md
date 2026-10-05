# Project Input Service Setup

🔧 **Tool** · programmers only

Creates the input setup for a fresh project in one click: an input action asset plus the matching `ProjectInputService` class.

## Where to find it

In the [🔧 Git Package Manager](Git%20Package%20Manager.md) window, under **Project Setup**. The button is called **Create ProjectInputService**.

## What it creates

| File | Path |
| --- | --- |
| Input action asset | `Assets/Input/PlayerInputActions.inputactions` |
| Input service class | `Assets/Generated/Input/ProjectInputService.cs` |

## Good to know

- The **Project Setup** section disappears once both files exist. If you do not see the button, the project is already set up.
- Existing files are never overwritten. Only what is missing gets created.
- After running it, Unity recompiles. Wait for that to finish before using the new service.
