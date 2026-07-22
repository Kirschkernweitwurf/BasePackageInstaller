#if UNITY_EDITOR
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

        private const int NameStatusDivider = 0;
        private const int StatusVersionDivider = 1;
        private const int NoDivider = -1;

        private float _nameWidth;
        private float _statusWidth;
        private int _dragging = NoDivider;

        private float _name;
        private float _status;
        private float _version;

        internal TableColumnLayout()
        {
            _nameWidth = EditorPrefs.GetFloat(NameWidthKey, InstallerTheme.Metrics.DefaultNameColumnWidth);
            _statusWidth = EditorPrefs.GetFloat(StatusWidthKey, InstallerTheme.Metrics.DefaultStatusColumnWidth);
        }

        /// <summary>Recomputes the column widths to fill the available content width. Call once per frame.</summary>
        internal void Recalculate(float availableWidth)
        {
            float flexible = availableWidth - InstallerTheme.Metrics.SelectionColumnWidth;

            _name = Mathf.Max(_nameWidth, InstallerTheme.Metrics.MinNameColumnWidth);
            _status = Mathf.Max(_statusWidth, InstallerTheme.Metrics.MinStatusColumnWidth);
            _version = flexible - _name - _status;

            if (_version >= InstallerTheme.Metrics.MinVersionColumnWidth)
                return;

            // Not enough room for Version: reclaim space from Status first, then Name.
            float deficit = InstallerTheme.Metrics.MinVersionColumnWidth - _version;

            float fromStatus = Mathf.Min(deficit, _status - InstallerTheme.Metrics.MinStatusColumnWidth);
            _status -= fromStatus;
            deficit -= fromStatus;

            _name = Mathf.Max(InstallerTheme.Metrics.MinNameColumnWidth, _name - deficit);
            _version = Mathf.Max(InstallerTheme.Metrics.MinVersionColumnWidth, flexible - _name - _status);
        }

        internal Rect SelectionRect(Rect area)
            => new(area.x, area.y, InstallerTheme.Metrics.SelectionColumnWidth, area.height);

        internal Rect NameRect(Rect area)
            => new(area.x + InstallerTheme.Metrics.SelectionColumnWidth, area.y, _name, area.height);

        internal Rect StatusRect(Rect area)
            => new(NameRect(area).xMax, area.y, _status, area.height);

        internal Rect VersionRect(Rect area)
            => new(StatusRect(area).xMax, area.y, _version, area.height);

        /// <summary>Draws the divider lines and processes any resize drag across the table height.</summary>
        internal void DrawAndProcessDividers(Rect area)
        {
            HandleDivider(NameStatusDivider, NameRect(area).xMax, area);
            HandleDivider(StatusVersionDivider, StatusRect(area).xMax, area);
        }

        private void HandleDivider(int divider, float x, Rect area)
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
                    _dragging = NoDivider;
                    Save();
                    current.Use();
                    break;
            }
        }

        private void Resize(int divider, float mouseX, Rect area)
        {
            if (divider == NameStatusDivider)
            {
                float start = area.x + InstallerTheme.Metrics.SelectionColumnWidth;
                _nameWidth = Mathf.Max(InstallerTheme.Metrics.MinNameColumnWidth, mouseX - start);
                return;
            }

            float statusStart = area.x + InstallerTheme.Metrics.SelectionColumnWidth + _name;
            _statusWidth = Mathf.Max(InstallerTheme.Metrics.MinStatusColumnWidth, mouseX - statusStart);
        }

        private void Save()
        {
            EditorPrefs.SetFloat(NameWidthKey, _nameWidth);
            EditorPrefs.SetFloat(StatusWidthKey, _statusWidth);
        }
    }
}
#endif
