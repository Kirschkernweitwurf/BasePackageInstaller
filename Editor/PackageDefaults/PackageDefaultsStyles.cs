using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Builds and caches every GUIStyle the window draws with, including the rounded corner textures
    /// generated at runtime for the cards, pills and buttons. Rebuilds automatically when the editor
    /// skin changes and frees its textures on <see cref="Dispose"/>.
    /// </summary>
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

        private readonly List<Texture2D> _ownedTextures = new();

        private Font _ownedFont;
        private bool _built;
        private bool _builtForProSkin;

        /// <summary>Rebuilds the styles only when needed (first use or a skin change).</summary>
        internal void EnsureBuilt()
        {
            if (_built && _builtForProSkin == EditorGUIUtility.isProSkin)
                return;

            Release();
            Build();

            _built = true;
            _builtForProSkin = EditorGUIUtility.isProSkin;
        }

        /// <summary>Destroys the generated textures. Call when the owning window closes.</summary>
        internal void Dispose() => Release();

        private static Color ColorAt(Color color, int x, int y, int size, int radius)
        {
            // Distance from the pixel center to the nearest point of the rectangle inset by the
            // radius. Inside that core the pixel is solid; near a corner it fades over one pixel.
            float pointX = x + 0.5f;
            float pointY = y + 0.5f;

            float nearestX = Mathf.Clamp(pointX, radius, size - radius);
            float nearestY = Mathf.Clamp(pointY, radius, size - radius);

            float distance = Mathf.Sqrt(Square(pointX - nearestX) + Square(pointY - nearestY));
            float coverage = Mathf.Clamp01(radius + 0.5f - distance);

            return new Color(color.r, color.g, color.b, color.a * coverage);
        }

        private static Color Shift(Color color, float amount)
            => new(color.r + amount, color.g + amount, color.b + amount, color.a);

        // Labels inherit hover and active states from the editor skin, which makes plain text light up
        // like a button. Pin every state to one color.
        private static void PinTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
        }

        private static float Square(float value) => value * value;

        private static RectOffset Uniform(int value) => new(value, value, value, value);

        private static RectOffset HorizontalPadding(int value) => new(value, value, 0, 0);

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

            PinTextColor(Title, PackageDefaultsTheme.Palette.Title);

            Description = new GUIStyle(EditorStyles.label)
            {
                fontSize = PackageDefaultsTheme.Metrics.DescriptionFontSize,
                wordWrap = true
            };

            PinTextColor(Description, PackageDefaultsTheme.Palette.Description);

            SectionHeader = new GUIStyle(EditorStyles.boldLabel);
            PinTextColor(SectionHeader, PackageDefaultsTheme.Palette.Title);

            RowLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = HorizontalPadding(PackageDefaultsTheme.Metrics.CellPadding)
            };

            PinTextColor(RowLabel, PackageDefaultsTheme.Palette.Title);

            RowValue = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = HorizontalPadding(PackageDefaultsTheme.Metrics.CellPadding)
            };

            PinTextColor(RowValue, PackageDefaultsTheme.Palette.Description);

            Window = new GUIStyle
            {
                padding = Uniform(PackageDefaultsTheme.Metrics.WindowPadding)
            };

            Card = RoundedStyle(PackageDefaultsTheme.Palette.Card,
                PackageDefaultsTheme.Metrics.CardCornerRadius);

            Card.padding = Uniform(PackageDefaultsTheme.Metrics.CardPadding);

            CodeCard = RoundedStyle(PackageDefaultsTheme.Palette.Code,
                PackageDefaultsTheme.Metrics.CardCornerRadius);

            CodeCard.padding = Uniform(PackageDefaultsTheme.Metrics.CardPadding);

            Code = new GUIStyle(EditorStyles.label)
            {
                font = _ownedFont,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                richText = false,
                padding = HorizontalPadding(PackageDefaultsTheme.Metrics.CellPadding)
            };

            PinTextColor(Code, PackageDefaultsTheme.Palette.CodeText);

            AddedGutter = GutterStyle(PackageDefaultsTheme.Palette.AddedText);
            RemovedGutter = GutterStyle(PackageDefaultsTheme.Palette.RemovedText);

            OkPill = PillStyle(PackageDefaultsTheme.Palette.OkPill, PackageDefaultsTheme.Palette.OkText);
            WarnPill = PillStyle(PackageDefaultsTheme.Palette.WarnPill, PackageDefaultsTheme.Palette.WarnText);
            MutedPill = PillStyle(PackageDefaultsTheme.Palette.MutedPill, PackageDefaultsTheme.Palette.MutedText);

            PrimaryButton = ButtonStyle(PackageDefaultsTheme.Palette.Accent,
                PackageDefaultsTheme.Palette.AccentText, FontStyle.Bold);

            SecondaryButton = ButtonStyle(PackageDefaultsTheme.Palette.Secondary,
                PackageDefaultsTheme.Palette.SecondaryText, FontStyle.Normal);
        }

        private GUIStyle GutterStyle(Color color)
        {
            GUIStyle style = new(Code)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                padding = Uniform(0)
            };

            PinTextColor(style, color);

            return style;
        }

        private GUIStyle PillStyle(Color background, Color text)
        {
            GUIStyle style = RoundedStyle(background, PackageDefaultsTheme.Metrics.PillCornerRadius);

            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = EditorStyles.miniLabel.fontSize;
            style.padding = new RectOffset(PackageDefaultsTheme.Metrics.PillPaddingX,
                PackageDefaultsTheme.Metrics.PillPaddingX, PackageDefaultsTheme.Metrics.PillPaddingY,
                PackageDefaultsTheme.Metrics.PillPaddingY);

            style.normal.textColor = text;

            return style;
        }

        private GUIStyle ButtonStyle(Color background, Color textColor, FontStyle fontStyle)
        {
            int radius = PackageDefaultsTheme.Metrics.CardCornerRadius;
            GUIStyle style = RoundedStyle(background, radius);

            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = fontStyle;

            PinTextColor(style, textColor);

            style.hover.background =
                RoundedTexture(Shift(background, PackageDefaultsTheme.Metrics.HoverLift), radius);

            style.active.background =
                RoundedTexture(Shift(background, -PackageDefaultsTheme.Metrics.PressDrop), radius);

            style.focused.background = style.normal.background;

            return style;
        }

        private GUIStyle RoundedStyle(Color color, int radius)
        {
            GUIStyle style = new()
            {
                border = Uniform(radius)
            };

            style.normal.background = RoundedTexture(color, radius);

            return style;
        }

        // A 9-sliced rounded rect texture: a (2r+1) square whose one pixel center stretches, so only the
        // rounded corners are drawn at their true size regardless of the target rectangle.
        private Texture2D RoundedTexture(Color color, int radius)
        {
            int size = radius * 2 + 1;

            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[size * size];

            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = ColorAt(color, index % size, index / size, size, radius);

            texture.SetPixels(pixels);
            texture.Apply();

            _ownedTextures.Add(texture);

            return texture;
        }

        private void Release()
        {
            foreach (Texture2D texture in _ownedTextures)
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }

            _ownedTextures.Clear();

            if (_ownedFont != null)
                Object.DestroyImmediate(_ownedFont);

            _ownedFont = null;
            _built = false;
        }
    }
}