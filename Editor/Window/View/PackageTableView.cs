using Base.PackageInstaller.Data;
using Base.PackageInstaller.Window.Theme;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window.View
{
    /// <summary>
    /// Renders the package table: a column header plus one striped row per package inside a card,
    /// each row showing a selection toggle, name, what drags it into the run, a colored status
    /// pill and the installed version. Columns are laid out by explicit rectangles through
    /// <see cref="TableColumnLayout"/>, so the header and every row line up exactly and the
    /// dividers between columns can be dragged.
    /// <para>
    /// A row another selected package drags in is drawn dimmed with a locked toggle, and the third
    /// column names the packages holding it, so the lock never needs explaining. Uninstalling,
    /// a package that is not installed has nothing to remove and is drawn dimmed and unpickable.
    /// </para>
    /// </summary>
    internal sealed class PackageTableView
    {
        private const string DependsOnColumn = "Depends On";
        private const string MissingValue = "-";
        private const string PackageColumn = "Package";
        private const string RequiredColumn = "Required By";
        private const string StatusColumn = "Status";
        private const string VersionColumn = "Version";

        private static readonly GUIContent CheckingContent = new("Checking...");
        private static readonly GUIContent InstalledContent = new("Installed");
        private static readonly GUIContent NotInstalledContent = new("Not installed");

        private readonly InstallerStyles _styles;
        private readonly TableColumnLayout _columns = new();
        private readonly GUIContent _heldContent = new();

        /// <summary>Creates the table view.</summary>
        /// <param name="styles">The shared style cache the table draws with.</param>
        internal PackageTableView(InstallerStyles styles) => _styles = styles;

        /// <summary>Draws the whole table, including the header and the resizable dividers.</summary>
        /// <param name="packages">The packages to list, one row each.</param>
        /// <param name="selected">The rows that will be processed, drawn as the tick state.</param>
        /// <param name="userSelected">
        /// The rows the user ticked, which is the only array a click writes to. What a package
        /// dragged in follows from this, so releasing a package releases what only it held.
        /// </param>
        /// <param name="heldBy">
        /// Per row, the selected packages that drag it into the run, or <c>null</c> when nothing
        /// does. Installing, those are the packages that require it; removing, the ones it requires.
        /// </param>
        /// <param name="lockHeld">
        /// True while the window resolves dependencies, which is what makes a held row read-only.
        /// With resolving off the column still reports the holders, but every row stays editable.
        /// </param>
        /// <param name="statuses">The per-row install status.</param>
        /// <param name="statusChecked">False while the install statuses are still being queried.</param>
        /// <param name="mode">The direction the window is working in.</param>
        /// <param name="scroll">The scroll position of the table, written back on user input.</param>
        internal void Draw(PackageEntry[] packages, bool[] selected, bool[] userSelected, string[] heldBy,
            bool lockHeld, PackageStatus[] statuses, bool statusChecked, EPackageMode mode, ref Vector2 scroll)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect card = EditorGUILayout.BeginVertical(_styles.Card);

            Rect header = ReserveRow(InstallerTheme.Metrics.RowHeight);
            _columns.Recalculate(ColumnsArea(header).width);
            DrawHeader(header, mode);
            DrawSeparator();

            for (int i = 0; i < packages.Length; i++)
            {
                DrawRow(i, packages[i], selected, userSelected, HeldBy(heldBy, i), lockHeld,
                    statuses[i], statusChecked, mode);
            }

            // Dividers span the whole card so a column can be resized from any row, not just the
            // header. This must run inside the scroll view so the card rectangle and the mouse
            // position share the same coordinate space. The rectangle from BeginVertical is not
            // computed yet during the Layout event, so that event is skipped.
            if (Event.current.type != EventType.Layout)
                _columns.DrawAndProcessDividers(ColumnsArea(InsetVertically(card)));

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private static Rect ReserveRow(float height)
            => GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));

        private static Rect ColumnsArea(Rect row)
        {
            float inset = InstallerTheme.Metrics.TableEdgeInset;

            return new Rect(row.x + inset, row.y, row.width - inset * 2f, row.height);
        }

        private static Rect InsetVertically(Rect card)
        {
            float padding = InstallerTheme.Metrics.CardVerticalPadding;

            return new Rect(card.x, card.y + padding, card.width, card.height - padding * 2f);
        }

        private static Rect ToggleRect(Rect cell)
        {
            float size = InstallerTheme.Metrics.ToggleSize;
            float y = cell.y + (cell.height - size) * 0.5f;

            return new Rect(cell.x, y, size, size);
        }

        private static string HeldBy(string[] heldBy, int index) => heldBy == null
            ? null
            : heldBy[index];

        private static void DrawSeparator()
        {
            Rect line = ReserveRow(InstallerTheme.Metrics.SeparatorThickness);

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(line, InstallerTheme.Palette.Separator);
        }

        private static string VersionText(PackageStatus status) => status.IsInstalled
            ? status.Version
            : MissingValue;

        // Both modes name the packages that drag a row into the run, but along opposite edges:
        // installing they are the ones that require it, removing the ones it requires.
        private static string HeldColumn(EPackageMode mode) => mode == EPackageMode.Install
            ? RequiredColumn
            : DependsOnColumn;

        private void DrawHeader(Rect row, EPackageMode mode)
        {
            Rect area = ColumnsArea(row);

            GUI.Label(_columns.NameRect(area), PackageColumn, _styles.ColumnHeader);
            GUI.Label(_columns.RequiredRect(area), HeldColumn(mode), _styles.ColumnHeader);
            GUI.Label(_columns.StatusRect(area), StatusColumn, _styles.ColumnHeader);
            GUI.Label(_columns.VersionRect(area), VersionColumn, _styles.ColumnHeader);
        }

        private void DrawRow(int index, PackageEntry package, bool[] selected, bool[] userSelected,
            string heldBy, bool lockHeld, PackageStatus status, bool statusChecked, EPackageMode mode)
        {
            Rect row = ReserveRow(InstallerTheme.Metrics.RowHeight);

            if (Event.current.type == EventType.Repaint && index % 2 != 0)
                EditorGUI.DrawRect(row, InstallerTheme.Palette.RowStripe);

            Rect area = ColumnsArea(row);

            // Nothing to take away, so the row is out of the run whatever the graph says about it.
            bool isUnavailable = mode == EPackageMode.Uninstall && statusChecked && !status.IsInstalled;
            bool isLocked = lockHeld && !string.IsNullOrEmpty(heldBy);

            DrawToggle(_columns.SelectionRect(area), selected, userSelected, index, isLocked, isUnavailable);

            GUI.Label(_columns.NameRect(area), package.Name, isLocked || isUnavailable
                ? _styles.DimmedLabel
                : _styles.RowLabel);

            DrawHeldBy(_columns.RequiredRect(area), isUnavailable
                ? null
                : heldBy);

            DrawStatusPill(_columns.StatusRect(area), status, statusChecked);
            GUI.Label(_columns.VersionRect(area), VersionText(status), _styles.RowLabel);
        }

        private void DrawToggle(Rect cell, bool[] selected, bool[] userSelected, int index, bool isLocked,
            bool isUnavailable)
        {
            Rect toggle = ToggleRect(cell);

            // Drawn rather than skipped, so the table keeps one row per package in both modes and
            // the columns to the right still read as belonging to something.
            if (isUnavailable)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Toggle(toggle, false);
                EditorGUI.EndDisabledGroup();

                return;
            }

            if (!isLocked)
            {
                // Writes to what the user picked, never to the derived selection: the difference
                // between the two is exactly what was pulled in on someone else's behalf.
                userSelected[index] = EditorGUI.Toggle(toggle, selected[index]);
                return;
            }

            // Drawn disabled rather than skipped, so the row still reads as selected while making
            // clear that this particular tick is not the user's to remove.
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.Toggle(toggle, true);
            EditorGUI.EndDisabledGroup();
        }

        // The column is narrow enough to clip a long list, so the full text is repeated as a
        // tooltip rather than being lost.
        private void DrawHeldBy(Rect cell, string heldBy)
        {
            bool isEmpty = string.IsNullOrEmpty(heldBy);

            _heldContent.text = isEmpty
                ? MissingValue
                : heldBy;

            _heldContent.tooltip = isEmpty
                ? string.Empty
                : heldBy;

            GUI.Label(cell, _heldContent, _styles.HeldColumnLabel);
        }

        private void DrawStatusPill(Rect cell, PackageStatus status, bool statusChecked)
        {
            if (!statusChecked)
            {
                GUI.Label(cell, CheckingContent, _styles.CheckingLabel);
                return;
            }

            GUIContent content = status.IsInstalled
                ? InstalledContent
                : NotInstalledContent;

            GUIStyle style = status.IsInstalled
                ? _styles.InstalledPill
                : _styles.NotInstalledPill;

            // Inset by the same padding the text columns use, so the pill does not sit flush
            // against the divider line to its left.
            float inset = InstallerTheme.Metrics.CellTextPadding;
            float width = Mathf.Min(style.CalcSize(content).x, Mathf.Max(0f, cell.width - inset));
            float y = cell.y + (cell.height - InstallerTheme.Metrics.PillHeight) * 0.5f;
            Rect pill = new(cell.x + inset, y, width, InstallerTheme.Metrics.PillHeight);

            GUI.Label(pill, content, style);
        }
    }
}