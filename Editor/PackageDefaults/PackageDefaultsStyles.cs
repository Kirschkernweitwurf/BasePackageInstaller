using Base.PackageInstaller.Shared;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Builds and caches every GUIStyle the window draws with, including the rounded corner textures
    /// generated at runtime for the cards, pills and buttons. Rebuilds automatically when the editor
    /// skin changes and frees its textures on <see cref="Dispose"/>.
    /// </summary>
    /// <remarks>
    /// The shapes come from <see cref="InstallerStyleBuilder"/>, which the installer window draws from
    /// as well. What stays here is which color goes on which control, plus the code font, which only
    /// this window needs.
    /// </remarks>
    internal sealed class PackageDefaultsStyles
    {
        /// <summary>The window title.</summary>
        internal GUIStyle Title { get; private set; }

        /// <summary>The wrapping paragraph under the title.</summary>
        internal GUIStyle Description { get; private set; }

        /// <summary>The heading above a section of the window.</summary>
        internal GUIStyle SectionHeader { get; private set; }

        /// <summary>The package name in a dependency row.</summary>
        internal GUIStyle RowLabel { get; private set; }

        /// <summary>The dependency list beside a package name, dimmed since it is the derived half.</summary>
        internal GUIStyle RowValue { get; private set; }

        /// <summary>Transparent wrapper that keeps every control off the window edges.</summary>
        internal GUIStyle Window { get; private set; }

        /// <summary>The panel a section sits in.</summary>
        internal GUIStyle Card { get; private set; }

        /// <summary>
        /// The panel the generated source sits in, darker so it reads as code rather than as content.
        /// </summary>
        internal GUIStyle CodeCard { get; private set; }

        /// <summary>Fixed pitch, no wrapping, so a code line keeps its shape and scrolls sideways.</summary>
        internal GUIStyle Code { get; private set; }

        /// <summary>The plus mark beside a diff line the generated file adds.</summary>
        internal GUIStyle AddedGutter { get; private set; }

        /// <summary>The minus mark beside a diff line the generated file removes.</summary>
        internal GUIStyle RemovedGutter { get; private set; }

        /// <summary>The pill reporting that the file on disk is already up to date.</summary>
        internal GUIStyle OkPill { get; private set; }

        /// <summary>The pill reporting that the file differs from what was scanned.</summary>
        internal GUIStyle WarnPill { get; private set; }

        /// <summary>The pill reporting that nothing has been scanned yet, or that no target is selected.</summary>
        internal GUIStyle MutedPill { get; private set; }

        /// <summary>The Write File button, filled with the accent color.</summary>
        internal GUIStyle PrimaryButton { get; private set; }

        /// <summary>The Browse, Scan and Copy buttons.</summary>
        internal GUIStyle SecondaryButton { get; private set; }

        // The first font the machine actually has wins, so this covers Windows, macOS and Linux.
        private static readonly string[] CodeFontNames =
        {
            "Consolas",
            "Menlo",
            "DejaVu Sans Mono",
            "Courier New"
        };

        private readonly InstallerStyleBuilder _builder = new();

        private Font _ownedFont;
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
                && _builtForThemeRevision == EditorUIBridge.Revision)
                return;

            Release();
            Build();

            _built = true;
            _builtForProSkin = EditorGUIUtility.isProSkin;
            _builtForThemeRevision = EditorUIBridge.Revision;
        }

        /// <summary>Destroys the generated textures and the code font. Call when the owning window closes.</summary>
        internal void Dispose() => Release();

        private void Build()
        {
            _ownedFont = Font.CreateDynamicFontFromOSFont(CodeFontNames, EditorStyles.label.fontSize);

            // Without this the font is a plain scene object: an assembly reload wipes it while the
            // styles still point at it, and the editor logs "Deleting invalid font reference".
            // Release still destroys it from OnDisable, which runs before every reload, so it is
            // owned rather than leaked.
            if (_ownedFont != null)
                _ownedFont.hideFlags = HideFlags.HideAndDontSave;

            Title = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = PackageDefaultsTheme.Metrics.TitleFontSize
            };

            InstallerStyleBuilder.PinTextColor(Title, PackageDefaultsTheme.Palette.Title);

            Description = new GUIStyle(EditorStyles.label)
            {
                fontSize = PackageDefaultsTheme.Metrics.DescriptionFontSize,
                wordWrap = true
            };

            InstallerStyleBuilder.PinTextColor(Description, PackageDefaultsTheme.Palette.Description);

            SectionHeader = new GUIStyle(EditorStyles.boldLabel);
            InstallerStyleBuilder.PinTextColor(SectionHeader, PackageDefaultsTheme.Palette.Title);

            RowLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = InstallerStyleBuilder.HorizontalPadding(PackageDefaultsTheme.Metrics.CellPadding)
            };

            InstallerStyleBuilder.PinTextColor(RowLabel, PackageDefaultsTheme.Palette.Title);

            RowValue = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = InstallerStyleBuilder.HorizontalPadding(PackageDefaultsTheme.Metrics.CellPadding)
            };

            InstallerStyleBuilder.PinTextColor(RowValue, PackageDefaultsTheme.Palette.Description);

            Window = new GUIStyle
            {
                padding = InstallerStyleBuilder.Uniform(PackageDefaultsTheme.Metrics.WindowPadding)
            };

            BuildCards();

            AddedGutter = GutterStyle(PackageDefaultsTheme.Palette.AddedText);
            RemovedGutter = GutterStyle(PackageDefaultsTheme.Palette.RemovedText);

            OkPill = Pill(PackageDefaultsTheme.Palette.OkPill, PackageDefaultsTheme.Palette.OkText);
            WarnPill = Pill(PackageDefaultsTheme.Palette.WarnPill, PackageDefaultsTheme.Palette.WarnText);
            MutedPill = Pill(PackageDefaultsTheme.Palette.MutedPill, PackageDefaultsTheme.Palette.MutedText);

            PrimaryButton = Button(PackageDefaultsTheme.Palette.Accent,
                PackageDefaultsTheme.Palette.AccentText, FontStyle.Bold);

            SecondaryButton = Button(PackageDefaultsTheme.Palette.Secondary,
                PackageDefaultsTheme.Palette.SecondaryText, FontStyle.Normal);
        }

        private void BuildCards()
        {
            int radius = PackageDefaultsTheme.Metrics.CardCornerRadius;

            // Each card gets its own RectOffset. GUIStyle keeps the instance it is handed, so sharing
            // one between two styles would tie their padding together for good.
            Card = _builder.RoundedStyle(PackageDefaultsTheme.Palette.Card, radius);
            Card.padding = InstallerStyleBuilder.Uniform(PackageDefaultsTheme.Metrics.CardPadding);

            CodeCard = _builder.RoundedStyle(PackageDefaultsTheme.Palette.Code, radius);
            CodeCard.padding = InstallerStyleBuilder.Uniform(PackageDefaultsTheme.Metrics.CardPadding);

            Code = new GUIStyle(EditorStyles.label)
            {
                font = _ownedFont,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                richText = false,
                padding = InstallerStyleBuilder.HorizontalPadding(PackageDefaultsTheme.Metrics.CellPadding)
            };

            InstallerStyleBuilder.PinTextColor(Code, PackageDefaultsTheme.Palette.CodeText);
        }

        private GUIStyle GutterStyle(Color color)
        {
            GUIStyle style = new(Code)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                padding = InstallerStyleBuilder.Uniform(0)
            };

            InstallerStyleBuilder.PinTextColor(style, color);

            return style;
        }

        private GUIStyle Pill(Color background, Color text) => _builder.PillStyle(background, text,
            PackageDefaultsTheme.Metrics.PillCornerRadius, PackageDefaultsTheme.Metrics.PillPaddingX,
            PackageDefaultsTheme.Metrics.PillPaddingY);

        private GUIStyle Button(Color background, Color textColor, FontStyle fontStyle)
            => _builder.ButtonStyle(background, textColor, fontStyle,
                PackageDefaultsTheme.Metrics.CardCornerRadius, PackageDefaultsTheme.Metrics.HoverLift,
                PackageDefaultsTheme.Metrics.PressDrop);

        private void Release()
        {
            _builder.Release();

            if (_ownedFont != null)
                Object.DestroyImmediate(_ownedFont);

            _ownedFont = null;
            _built = false;
        }
    }
}