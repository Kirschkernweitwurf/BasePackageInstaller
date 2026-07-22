#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window.Theme
{
    /// <summary>
    /// Builds and caches every GUIStyle the installer draws with, including the rounded-corner
    /// textures generated at runtime for the status pills, card and buttons. Rebuilds
    /// automatically when the editor skin changes and frees its textures on <see cref="Dispose"/>.
    /// </summary>
    internal sealed class InstallerStyles
    {
        private const float HoverLift = 0.06f;
        private const float PressDrop = 0.08f;

        private readonly List<Texture2D> _ownedTextures = new();

        private bool _built;
        private bool _builtForProSkin;

        internal GUIStyle Title { get; private set; }
        internal GUIStyle Description { get; private set; }
        internal GUIStyle SectionHeader { get; private set; }
        internal GUIStyle ColumnHeader { get; private set; }
        internal GUIStyle RowLabel { get; private set; }
        internal GUIStyle Card { get; private set; }
        internal GUIStyle InstalledPill { get; private set; }
        internal GUIStyle NotInstalledPill { get; private set; }
        internal GUIStyle CheckingLabel { get; private set; }
        internal GUIStyle PrimaryButton { get; private set; }
        internal GUIStyle SecondaryButton { get; private set; }

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

        private void Build()
        {
            Title = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = InstallerTheme.Metrics.TitleFontSize
            };
            PinTextColor(Title, InstallerTheme.Palette.Title);

            Description = new GUIStyle(EditorStyles.label)
            {
                fontSize = InstallerTheme.Metrics.DescriptionFontSize,
                wordWrap = true
            };
            PinTextColor(Description, InstallerTheme.Palette.Description);

            SectionHeader = new GUIStyle(EditorStyles.boldLabel);
            PinTextColor(SectionHeader, InstallerTheme.Palette.Title);

            ColumnHeader = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = HorizontalPadding(InstallerTheme.Metrics.CellTextPadding)
            };
            PinTextColor(ColumnHeader, InstallerTheme.Palette.Description);

            RowLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = HorizontalPadding(InstallerTheme.Metrics.CellTextPadding)
            };
            PinTextColor(RowLabel, EditorStyles.label.normal.textColor);

            Card = RoundedStyle(InstallerTheme.Palette.Card, InstallerTheme.Metrics.CardCornerRadius);
            Card.padding = new RectOffset(0, 0, InstallerTheme.Metrics.CardVerticalPadding,
                InstallerTheme.Metrics.CardVerticalPadding);

            InstalledPill = PillStyle(InstallerTheme.Palette.InstalledPill, InstallerTheme.Palette.InstalledText);
            NotInstalledPill = PillStyle(InstallerTheme.Palette.NotInstalledPill, InstallerTheme.Palette.NotInstalledText);

            CheckingLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = HorizontalPadding(InstallerTheme.Metrics.CellTextPadding)
            };
            PinTextColor(CheckingLabel, InstallerTheme.Palette.CheckingText);

            PrimaryButton = ButtonStyle(InstallerTheme.Palette.Accent, InstallerTheme.Palette.AccentText,
                FontStyle.Bold);

            SecondaryButton = ButtonStyle(InstallerTheme.Palette.Secondary, InstallerTheme.Palette.SecondaryText,
                FontStyle.Normal);
        }

        private GUIStyle PillStyle(Color background, Color text)
        {
            GUIStyle style = RoundedStyle(background, InstallerTheme.Metrics.PillCornerRadius);

            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = EditorStyles.miniLabel.fontSize;
            style.padding = new RectOffset(InstallerTheme.Metrics.PillPaddingX, InstallerTheme.Metrics.PillPaddingX,
                InstallerTheme.Metrics.PillPaddingY, InstallerTheme.Metrics.PillPaddingY);
            style.normal.textColor = text;

            return style;
        }

        private GUIStyle ButtonStyle(Color background, Color textColor, FontStyle fontStyle)
        {
            int radius = InstallerTheme.Metrics.CardCornerRadius;
            GUIStyle style = RoundedStyle(background, radius);

            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = fontStyle;

            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            style.focused.textColor = textColor;

            style.hover.background = RoundedTexture(Shift(background, HoverLift), radius);
            style.active.background = RoundedTexture(Shift(background, -PressDrop), radius);
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

        // A 9-sliced rounded-rect texture: a (2r+1) square whose 1px center stretches, so only the
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

            foreach (int index in Indices(pixels.Length))
            {
                int x = index % size;
                int y = index / size;

                pixels[index] = ColorAt(color, x, y, size, radius);
            }

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
            _built = false;
        }

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

        private static IEnumerable<int> Indices(int count)
        {
            for (int i = 0; i < count; i++)
                yield return i;
        }

        private static Color Shift(Color color, float amount)
            => new(color.r + amount, color.g + amount, color.b + amount, color.a);

        // Labels inherit hover/active/focused states from the editor skin (white text in the dark
        // skin), which makes plain text light up like a button. Pin every state to one color.
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
    }
}
#endif
