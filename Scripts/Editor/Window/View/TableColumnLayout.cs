using Base.PackageInstaller.Window.Theme;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window.View
{
    /// <summary>
    /// Owns the table's column widths and the draggable dividers between them. The selection
    /// column is fixed; Name and Status are user-resizable and persisted in EditorPrefs; Version
    /// takes the remaining width. Given a row rectangle it hands back one rectangle per column, so
    /// the header and every row share the exact same x positions and stay perfectly aligned.
    /// </summary>
    internal sealed class TableColumnLayout
    {
        private const string NameWidthKey = "Base.PackageInstaller.Columns.NameWidth";
        private const string StatusWidthKey = "Base.PackageInstaller.Columns.StatusWidth";

        // The widths the user dragged to, persisted between sessions.
        private float _savedNameWidth;
        private float _savedStatusWidth;

        // The widths actually drawn this frame, clamped to the available space.
        private float _nameWidth;
        private float _statusWidth;
        private float _versionWidth;

        private ETableDivider _dragging = ETableDivider.None;

        /// <summary>Restores the column widths the user last dragged to.</summary>
        internal TableColumnLayout()
        {
            _savedNameWidth = EditorPrefs.GetFloat(NameWidthKey, InstallerTheme.Metrics.DefaultNameColumnWidth);
            _savedStatusWidth = EditorPrefs.GetFloat(StatusWidthKey, InstallerTheme.Metrics.DefaultStatusColumnWidth);
        }

        /// <summary>Recomputes the column widths to fill the available content width. Call once per frame.</summary>
        /// <param name="availableWidth">The content width the columns have to share.</param>
        internal void Recalculate(float availableWidth)
        {
            float flexible = availableWidth - InstallerTheme.Metrics.SelectionColumnWidth;

            _nameWidth = Mathf.Max(_savedNameWidth, InstallerTheme.Metrics.MinNameColumnWidth);
            _statusWidth = Mathf.Max(_savedStatusWidth, InstallerTheme.Metrics.MinStatusColumnWidth);
            _versionWidth = flexible - _nameWidth - _statusWidth;

            if (_versionWidth >= InstallerTheme.Metrics.MinVersionColumnWidth)
                return;

            // Not enough room for Version: reclaim space from Status first, then Name.
            float deficit = InstallerTheme.Metrics.MinVersionColumnWidth - _versionWidth;

            float fromStatus = Mathf.Min(deficit, _statusWidth - InstallerTheme.Metrics.MinStatusColumnWidth);
            _statusWidth -= fromStatus;
            deficit -= fromStatus;

            _nameWidth = Mathf.Max(InstallerTheme.Metrics.MinNameColumnWidth, _nameWidth - deficit);
            _versionWidth = Mathf.Max(InstallerTheme.Metrics.MinVersionColumnWidth,
                flexible - _nameWidth - _statusWidth);
        }

        /// <summary>The cell holding the selection toggle.</summary>
        /// <param name="area">The row area the columns are laid out in.</param>
        /// <returns>The selection cell rectangle.</returns>
        internal Rect SelectionRect(Rect area)
            => new(area.x, area.y, InstallerTheme.Metrics.SelectionColumnWidth, area.height);

        /// <summary>The cell holding the package name.</summary>
        /// <param name="area">The row area the columns are laid out in.</param>
        /// <returns>The name cell rectangle.</returns>
        internal Rect NameRect(Rect area) => new(area.x + InstallerTheme.Metrics.SelectionColumnWidth, area.y,
            _nameWidth, area.height);

        /// <summary>The cell holding the status pill.</summary>
        /// <param name="area">The row area the columns are laid out in.</param>
        /// <returns>The status cell rectangle.</returns>
        internal Rect StatusRect(Rect area) => new(NameRect(area).xMax, area.y, _statusWidth, area.height);

        /// <summary>The cell holding the installed version.</summary>
        /// <param name="area">The row area the columns are laid out in.</param>
        /// <returns>The version cell rectangle.</returns>
        internal Rect VersionRect(Rect area) => new(StatusRect(area).xMax, area.y, _versionWidth, area.height);

        /// <summary>Draws the divider lines and processes any resize drag across the table height.</summary>
        /// <param name="area">The full table area the dividers span.</param>
        internal void DrawAndProcessDividers(Rect area)
        {
            HandleDivider(ETableDivider.NameStatus, NameRect(area).xMax, area);
            HandleDivider(ETableDivider.StatusVersion, StatusRect(area).xMax, area);
        }

        private void HandleDivider(ETableDivider divider, float x, Rect area)
        {
            Rect line = new(x - InstallerTheme.Metrics.DividerThickness * 0.5f, area.y,
                InstallerTheme.Metrics.DividerThickness, area.height);

            Rect hit = new(x - InstallerTheme.Metrics.DividerHitWidth * 0.5f, area.y,
                InstallerTheme.Metrics.DividerHitWidth, area.height);

            EditorGUIUtility.AddCursorRect(hit, MouseCursor.ResizeHorizontal);

            Event current = Event.current;

            switch (current.type)
            {
                case EventType.Repaint:
                    Color color = _dragging == divider
                        ? InstallerTheme.Palette.DividerActive
                        : InstallerTheme.Palette.Divider;

                    EditorGUI.DrawRect(line, color);
                    break;

                case EventType.MouseDown when hit.Contains(current.mousePosition):
                    _dragging = divider;
                    current.Use();
                    break;

                case EventType.MouseDrag when _dragging == divider:
                    Resize(divider, current.mousePosition.x, area);
                    current.Use();
                    break;

                case EventType.MouseUp when _dragging == divider:
                    _dragging = ETableDivider.None;
                    Save();
                    current.Use();
                    break;
            }
        }

        private void Resize(ETableDivider divider, float mouseX, Rect area)
        {
            if (divider == ETableDivider.NameStatus)
            {
                float nameStart = area.x + InstallerTheme.Metrics.SelectionColumnWidth;
                _savedNameWidth = Mathf.Max(InstallerTheme.Metrics.MinNameColumnWidth, mouseX - nameStart);
                return;
            }

            float statusStart = area.x + InstallerTheme.Metrics.SelectionColumnWidth + _nameWidth;
            _savedStatusWidth = Mathf.Max(InstallerTheme.Metrics.MinStatusColumnWidth, mouseX - statusStart);
        }

        private void Save()
        {
            EditorPrefs.SetFloat(NameWidthKey, _savedNameWidth);
            EditorPrefs.SetFloat(StatusWidthKey, _savedStatusWidth);
        }
    }
}