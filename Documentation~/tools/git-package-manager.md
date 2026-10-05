# Git Package Manager

Installs and updates all Base packages, and any other Git package, from one window. No copying Git URLs one by one.

## Where to find it

**Menu:** `Tools > Installer > Git Package Manager`

## The window

| Part | What it does |
| --- | --- |
| Package table | One row per package with a checkbox, its status pill (green **Installed**, grey **Not installed**) and the installed version. |
| Refresh | Re-checks the status of every package and pulls in new default packages. |
| Edit List | Opens the [🔧 Git Packages List](git-packages-list.md) page in the Project Settings. |
| Select All / Deselect All | Ticks or unticks every row. |
| Action button | Runs the job on the ticked packages. |
| Project Setup | Only shows while the input service is missing. See [🔧 Project Input Service Setup](project-input-service-setup.md). |
| Result | Summary after a run, with a **Clear** button to dismiss it. |

## The action button

The label changes with your selection, but the job is always the same: each package is re-resolved from Git.

| Label | When you see it |
| --- | --- |
| Install Selected | Nothing you ticked is installed yet |
| Update Selected | Everything you ticked is already installed |
| Install / Update Selected | A mix of both |

## Steps

| Step | What to do |
| --- | --- |
| 1   | Open `Tools > Installer > Git Package Manager`. |
| 2   | Tick the packages you want, or use **Select All**. |
| 3   | Click the action button and wait for the result box. |

## Good to know

- Columns are resizable. Drag the divider between two columns, at any row, and the widths are kept for next time.
- A run keeps going when a package fails. The failure lands in the Console as a warning and the summary reads like `Done. 5 ok, 1 failed.`
- Installing can trigger a script recompile that restarts the window. The run picks up where it left off on its own, so let it finish.
- Every package writes its result to the Console, for example
  `Updated UI 1.1.0 → 1.2.0.`
