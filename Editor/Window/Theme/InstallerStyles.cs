using Base.PackageInstaller.Shared;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window.Theme
{
    /// <summary>
    /// Builds and caches every GUIStyle the installer draws with, including the rounded-corner
    /// textures generated at runtime for the status pills, card and buttons. Rebuilds
    /// automatically when the editor skin changes and frees its textures on <see cref="Dispose"/>.
    /// </summary>
    /// <remarks>
    /// The shapes come from <see cref="InstallerStyleBuilder"/>, which the Package Defaults window
    /// draws from as well. What stays here is which color goes on which control, since that is the
    /// only part the two windows do not agree on.
    /// </remarks>
    internal sealed class InstallerStyles
    {
        /// <summary>The window title.</summary>
        internal GUIStyle Title { get; private set; }

        /// <summary>The wrapping paragraph under the title.</summary>
        internal GUIStyle Description { get; private set; }

        /// <summary>The heading above a section of the window.</summary>
        internal GUIStyle SectionHeader { get; private set; }

        /// <summary>The column names above the table, smaller and dimmed.</summary>
        internal GUIStyle ColumnHeader { get; private set; }

        /// <summary>The package name in a row, and the version beside it.</summary>
        internal GUIStyle RowLabel { get; private set; }

        /// <summary>
        /// A row another selected package drags in, dimmed so its locked toggle reads as deliberate.
        /// </summary>
        internal GUIStyle DimmedLabel { get; private set; }

        /// <summary>
        /// The holders column, drawn smaller and clipped with an ellipsis rather than pushing the other columns
        /// around.
        /// </summary>
        internal GUIStyle HeldColumnLabel { get; private set; }

        /// <summary>The panel the table sits in.</summary>
        internal GUIStyle Card { get; private set; }

        /// <summary>The pill on a row whose package is installed.</summary>
        internal GUIStyle InstalledPill { get; private set; }

        /// <summary>The pill on a row whose package is missing.</summary>
        internal GUIStyle NotInstalledPill { get; private set; }

        /// <summary>
        /// The placeholder in the status column while the install statuses are still being queried.
        /// </summary>
        internal GUIStyle CheckingLabel { get; private set; }

        /// <summary>The action button at the bottom, filled with the accent color.</summary>
        internal GUIStyle PrimaryButton { get; private set; }

        /// <summary>The toolbar and selection buttons.</summary>
        internal GUIStyle SecondaryButton { get; private set; }

        /// <summary>The recessed rail the two mode segments sit in.</summary>
        internal GUIStyle SegmentTrack { get; private set; }

        /// <summary>A mode segment that is not the active one, letting the rail show through.</summary>
        internal GUIStyle Segment { get; private set; }

        /// <summary>The active mode segment, filled with the accent color.</summary>
        internal GUIStyle SegmentSelected { get; private set; }

        /// <summary>
        /// A transparent wrapper that keeps everything off the window edges. Nothing but an inset.
        /// </summary>
        internal GUIStyle WindowBody { get; private set; }

        private readonly InstallerStyleBuilder _builder = new();

        private bool _built;
        private bool _builtForProSkin;
        private int _builtForThemeRevision;

        /// <summary>
        /// Rebuilds the styles only when needed: first use, a skin change, or a change to the shared
        /// theme this window follows while the Editor UI package is installed.
        /// </summary>
        internal void EnsureBuilt()
        {
            if (_built
                && _builtForProSkin == EditorGUIUtility.isProSkin
                && _builtForThemeRevision == EditorUiBridge.Revision)
                return;

            Release();
            Build();

            _built = true;
            _builtForProSkin = EditorGUIUtility.isProSkin;
            _builtForThemeRevision = EditorUiBridge.Revision;
        }

        /// <summary>Destroys the generated textures. Call when the owning window closes.</summary>
        internal void Dispose() => Release();

        private static GUIStyle SegmentStyle(Color textColor, FontStyle fontStyle)
        {
            GUIStyle style = new()
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = fontStyle,
                border = InstallerStyleBuilder.Uniform(InstallerTheme.Metrics.CardCornerRadius)
            };

            InstallerStyleBuilder.PinTextColor(style, textColor);

            return style;
        }

        private void Build()
        {
            Title = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = InstallerTheme.Metrics.TitleFontSize
            };

            InstallerStyleBuilder.PinTextColor(Title, InstallerTheme.Palette.Title);

            Description = new GUIStyle(EditorStyles.label)
            {
                fontSize = InstallerTheme.Metrics.DescriptionFontSize,
                wordWrap = true
            };

            InstallerStyleBuilder.PinTextColor(Description, InstallerTheme.Palette.Description);

            SectionHeader = new GUIStyle(EditorStyles.boldLabel);
            InstallerStyleBuilder.PinTextColor(SectionHeader, InstallerTheme.Palette.Title);

            ColumnHeader = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = InstallerStyleBuilder.HorizontalPadding(InstallerTheme.Metrics.CellTextPadding)
            };

            InstallerStyleBuilder.PinTextColor(ColumnHeader, InstallerTheme.Palette.Description);

            RowLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = InstallerStyleBuilder.HorizontalPadding(InstallerTheme.Metrics.CellTextPadding)
            };

            InstallerStyleBuilder.PinTextColor(RowLabel, EditorStyles.label.normal.textColor);

            // A package another one depends on: same row, dimmed, so the locked toggle next to it
            // reads as deliberate rather than broken.
            DimmedLabel = new GUIStyle(RowLabel);
            InstallerStyleBuilder.PinTextColor(DimmedLabel, InstallerTheme.Palette.Description);

            // The list of holders is secondary information and can be long, so it is drawn smaller
            // and clipped with an ellipsis instead of pushing the other columns around.
            HeldColumnLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Ellipsis,
                padding = InstallerStyleBuilder.HorizontalPadding(InstallerTheme.Metrics.CellTextPadding)
            };

            InstallerStyleBuilder.PinTextColor(HeldColumnLabel, InstallerTheme.Palette.Description);

            Card = _builder.RoundedStyle(InstallerTheme.Palette.Card, InstallerTheme.Metrics.CardCornerRadius);
            Card.padding = new RectOffset(0, 0, InstallerTheme.Metrics.CardVerticalPadding,
                InstallerTheme.Metrics.CardVerticalPadding);

            InstalledPill = Pill(InstallerTheme.Palette.InstalledPill, InstallerTheme.Palette.InstalledText);
            NotInstalledPill = Pill(InstallerTheme.Palette.NotInstalledPill,
                InstallerTheme.Palette.NotInstalledText);

            CheckingLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = InstallerStyleBuilder.HorizontalPadding(InstallerTheme.Metrics.CellTextPadding)
            };

            InstallerStyleBuilder.PinTextColor(CheckingLabel, InstallerTheme.Palette.CheckingText);

            PrimaryButton = Button(InstallerTheme.Palette.Accent, InstallerTheme.Palette.AccentText,
                FontStyle.Bold);

            SecondaryButton = Button(InstallerTheme.Palette.Secondary, InstallerTheme.Palette.SecondaryText,
                FontStyle.Normal);

            BuildSegments();

            // Nothing but an inset. Every section inside keeps its own spacing, so this only stops
            // the content sitting flush against the window edges.
            WindowBody = new GUIStyle
            {
                padding = InstallerStyleBuilder.Uniform(InstallerTheme.Metrics.WindowPadding)
            };
        }

        // The rail is drawn with the same rounded corners as the card and the buttons, so the mode
        // switch reads as part of the window rather than as an editor toolbar dropped in. Only the
        // active segment is filled; the other one lets the rail show through, so the pair reads as one
        // control with a highlight moving across it.
        private void BuildSegments()
        {
            int radius = InstallerTheme.Metrics.CardCornerRadius;

            SegmentTrack = _builder.RoundedStyle(InstallerTheme.Palette.SegmentTrack, radius);

            Segment = SegmentStyle(InstallerTheme.Palette.SecondaryText, FontStyle.Normal);
            SegmentSelected = SegmentStyle(InstallerTheme.Palette.AccentText, FontStyle.Bold);

            SegmentSelected.normal.background = _builder.RoundedTexture(InstallerTheme.Palette.Accent, radius);

            SegmentSelected.hover.background = _builder.RoundedTexture(
                InstallerStyleBuilder.Shift(InstallerTheme.Palette.Accent, InstallerTheme.Metrics.HoverLift),
                radius);

            Segment.hover.background = _builder.RoundedTexture(InstallerTheme.Palette.SegmentHover, radius);

            SegmentSelected.active.background = SegmentSelected.normal.background;
            SegmentSelected.focused.background = SegmentSelected.normal.background;
            Segment.active.background = Segment.hover.background;
        }

        private GUIStyle Pill(Color background, Color text) => _builder.PillStyle(background, text,
            InstallerTheme.Metrics.PillCornerRadius, InstallerTheme.Metrics.PillPaddingX,
            InstallerTheme.Metrics.PillPaddingY);

        private GUIStyle Button(Color background, Color textColor, FontStyle fontStyle)
            => _builder.ButtonStyle(background, textColor, fontStyle, InstallerTheme.Metrics.CardCornerRadius,
                InstallerTheme.Metrics.HoverLift, InstallerTheme.Metrics.PressDrop);

        private void Release()
        {
            _builder.Release();
            _built = false;
        }
    }
}