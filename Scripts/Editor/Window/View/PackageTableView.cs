using Base.PackageInstaller.Data;
using Base.PackageInstaller.Window.Theme;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window.View
{
    /// <summary>
    /// Renders the package table: a column header plus one striped row per package inside a card,
    /// each row showing a selection toggle, name, what depends on it, a colored status pill and the
    /// installed version. Columns are laid out by explicit rectangles through
    /// <see cref="TableColumnLayout"/>, so the header and every row line up exactly and the
    /// dividers between columns can be dragged.
    /// <para>
    /// A row another selected package depends on is drawn dimmed with a locked toggle, and the
    /// Required By column names the packages holding it, so the lock never needs explaining.
    /// </para>
    /// </summary>
    internal sealed class PackageTableView
    {
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
        private readonly GUIContent _requiredContent = new();

        /// <summary>Creates the table view.</summary>
        /// <param name="styles">The shared style cache the table draws with.</param>
        internal PackageTableView(InstallerStyles styles) => _styles = styles;

        /// <summary>Draws the whole table, including the header and the resizable dividers.</summary>
        /// <param name="packages">The packages to list, one row each.</param>
        /// <param name="selected">The rows that will be processed, drawn as the tick state.</param>
        /// <param name="userSelected">
        /// The rows the user ticked, which is the only array a click writes to. What a package
        /// pulled in follows from this, so releasing a package releases what only it needed.
        /// </param>
        /// <param name="requiredBy">
        /// Per row, the selected packages that depend on it, or <c>null</c> when nothing does.
        /// </param>
        /// <param name="lockRequired">
        /// True while the window resolves dependencies, which is what makes a held row read-only.
        /// With resolving off the column still reports the holders, but every row stays editable.
        /// </param>
        /// <param name="statuses">The per-row install status.</param>
        /// <param name="statusChecked">False while the install statuses are still being queried.</param>
        /// <param name="scroll">The scroll position of the table, written back on user input.</param>
        internal void Draw(PackageEntry[] packages, bool[] selected, bool[] userSelected, string[] requiredBy,
            bool lockRequired, PackageStatus[] statuses, bool statusChecked, ref Vector2 scroll)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect card = EditorGUILayout.BeginVertical(_styles.Card);

            Rect header = ReserveRow(InstallerTheme.Metrics.RowHeight);
            _columns.Recalculate(ColumnsArea(header).width);
            DrawHeader(header);
            DrawSeparator();

            for (int i = 0; i < packages.Length; i++)
            {
                DrawRow(i, packages[i], selected, userSelected, RequiredBy(requiredBy, i), lockRequired,
                    statuses[i], statusChecked);
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

        private static string RequiredBy(string[] requiredBy, int index) => requiredBy == null
            ? null
            : requiredBy[index];

        private static void DrawSeparator()
        {
            Rect line = ReserveRow(InstallerTheme.Metrics.SeparatorThickness);

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(line, InstallerTheme.Palette.Separator);
        }

        private static string VersionText(PackageStatus status) => status.IsInstalled
            ? status.Version
            : MissingValue;

        private void DrawHeader(Rect row)
        {
            Rect area = ColumnsArea(row);

            GUI.Label(_columns.NameRect(area), PackageColumn, _styles.ColumnHeader);
            GUI.Label(_columns.RequiredRect(area), RequiredColumn, _styles.ColumnHeader);
            GUI.Label(_columns.StatusRect(area), StatusColumn, _styles.ColumnHeader);
            GUI.Label(_columns.VersionRect(area), VersionColumn, _styles.ColumnHeader);
        }

        private void DrawRow(int index, PackageEntry package, bool[] selected, bool[] userSelected,
            string requiredBy, bool lockRequired, PackageStatus status, bool statusChecked)
        {
            Rect row = ReserveRow(InstallerTheme.Metrics.RowHeight);

            if (Event.current.type == EventType.Repaint && index % 2 != 0)
                EditorGUI.DrawRect(row, InstallerTheme.Palette.RowStripe);

            Rect area = ColumnsArea(row);
            bool isLocked = lockRequired && !string.IsNullOrEmpty(requiredBy);

            DrawToggle(_columns.SelectionRect(area), selected, userSelected, index, isLocked);

            GUI.Label(_columns.NameRect(area), package.Name, isLocked
                ? _styles.RequiredLabel
                : _styles.RowLabel);

            DrawRequiredBy(_columns.RequiredRect(area), requiredBy);
            DrawStatusPill(_columns.StatusRect(area), status, statusChecked);
            GUI.Label(_columns.VersionRect(area), VersionText(status), _styles.RowLabel);
        }

        private void DrawToggle(Rect cell, bool[] selected, bool[] userSelected, int index, bool isLocked)
        {
            Rect toggle = ToggleRect(cell);

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
        private void DrawRequiredBy(Rect cell, string requiredBy)
        {
            bool isEmpty = string.IsNullOrEmpty(requiredBy);

            _requiredContent.text = isEmpty
                ? MissingValue
                : requiredBy;

            _requiredContent.tooltip = isEmpty
                ? string.Empty
                : requiredBy;

            GUI.Label(cell, _requiredContent, _styles.RequiredColumnLabel);
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