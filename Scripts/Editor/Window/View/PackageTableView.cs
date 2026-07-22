#if UNITY_EDITOR
using Base.PackageInstaller.Data;
using Base.PackageInstaller.Window.Theme;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window.View
{
    /// <summary>
    /// Renders the package table: a column header plus one striped row per package inside a card,
    /// each row showing a selection toggle, name, a colored status pill and the installed version.
    /// Columns are laid out by explicit rectangles through <see cref="TableColumnLayout"/>, so the
    /// header and every row line up exactly and the dividers between columns can be dragged.
    /// </summary>
    internal sealed class PackageTableView
    {
        private const string PackageColumn = "Package";
        private const string StatusColumn = "Status";
        private const string VersionColumn = "Version";
        private const string MissingVersion = "—";

        private static readonly GUIContent CheckingContent = new("Checking…");
        private static readonly GUIContent InstalledContent = new("Installed");
        private static readonly GUIContent NotInstalledContent = new("Not installed");

        private readonly InstallerStyles _styles;
        private readonly TableColumnLayout _columns = new();

        internal PackageTableView(InstallerStyles styles) => _styles = styles;

        internal void Draw(PackageEntry[] packages, bool[] selected, PackageStatus[] statuses,
            bool statusChecked, ref Vector2 scroll)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect card = EditorGUILayout.BeginVertical(_styles.Card);

            Rect header = ReserveRow(InstallerTheme.Metrics.RowHeight);
            _columns.Recalculate(ColumnsArea(header).width);
            DrawHeader(header);
            DrawSeparator();

            for (int i = 0; i < packages.Length; i++)
                DrawRow(i, packages[i], selected, statuses[i], statusChecked);

            // Dividers span the whole card so a column can be resized from any row, not just the
            // header. This must run inside the scroll view so the card rectangle and the mouse
            // position share the same coordinate space. The rectangle from BeginVertical is not
            // computed yet during the Layout event, so that event is skipped.
            if (Event.current.type != EventType.Layout)
                _columns.DrawAndProcessDividers(ColumnsArea(InsetVertically(card)));

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader(Rect row)
        {
            Rect area = ColumnsArea(row);

            GUI.Label(_columns.NameRect(area), PackageColumn, _styles.ColumnHeader);
            GUI.Label(_columns.StatusRect(area), StatusColumn, _styles.ColumnHeader);
            GUI.Label(_columns.VersionRect(area), VersionColumn, _styles.ColumnHeader);
        }

        private void DrawRow(int index, PackageEntry package, bool[] selected, PackageStatus status,
            bool statusChecked)
        {
            Rect row = ReserveRow(InstallerTheme.Metrics.RowHeight);

            if (Event.current.type == EventType.Repaint && index % 2 != 0)
                EditorGUI.DrawRect(row, InstallerTheme.Palette.RowStripe);

            Rect area = ColumnsArea(row);

            selected[index] = EditorGUI.Toggle(ToggleRect(_columns.SelectionRect(area)), selected[index]);
            GUI.Label(_columns.NameRect(area), package.Name, _styles.RowLabel);
            DrawStatusPill(_columns.StatusRect(area), status, statusChecked);
            GUI.Label(_columns.VersionRect(area), VersionText(status), _styles.RowLabel);
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

            float width = Mathf.Min(style.CalcSize(content).x, cell.width);
            float y = cell.y + (cell.height - InstallerTheme.Metrics.PillHeight) * 0.5f;
            Rect pill = new(cell.x, y, width, InstallerTheme.Metrics.PillHeight);

            GUI.Label(pill, content, style);
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

        private static void DrawSeparator()
        {
            Rect line = ReserveRow(InstallerTheme.Metrics.SeparatorThickness);

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(line, InstallerTheme.Palette.Separator);
        }

        private static string VersionText(PackageStatus status)
        {
            return status.IsInstalled
                ? status.Version
                : MissingVersion;
        }
    }
}
#endif
