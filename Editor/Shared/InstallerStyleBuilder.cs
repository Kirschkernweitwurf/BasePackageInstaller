using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Shared
{
    /// <summary>
    /// Builds the rounded panels, pills and buttons both installer windows are drawn with, and owns
    /// the textures generated for them.
    /// </summary>
    /// <remarks>
    /// The installer cannot depend on the EditorUI package, since it has to compile in a project where
    /// no Base package is installed yet. That is why it carries its own look, and it is also why this
    /// exists: the reason applies once, not once per window.
    /// <para>
    /// Colors and metrics stay with each window's own theme. Only the shapes are here, because a pill
    /// is a pill whichever palette fills it. Every texture handed out is remembered, so one
    /// <see cref="Release"/> frees the lot when the owning window closes or the skin changes.
    /// </para>
    /// </remarks>
    internal sealed class InstallerStyleBuilder
    {
        private readonly List<Texture2D> _ownedTextures = new();

        // Labels inherit hover, active and focused states from the editor skin, which makes plain text
        // light up like a button. Pin every state to one color.
        internal static void PinTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
        }

        /// <summary>The same color moved toward white, or toward black for a negative amount.</summary>
        /// <param name="color">The color to move.</param>
        /// <param name="amount">How far each channel moves.</param>
        /// <returns>The shifted color.</returns>
        internal static Color Shift(Color color, float amount)
            => new(color.r + amount, color.g + amount, color.b + amount, color.a);

        /// <summary>The same inset on all four sides.</summary>
        /// <param name="value">The inset in points.</param>
        /// <returns>The offset.</returns>
        internal static RectOffset Uniform(int value) => new(value, value, value, value);

        /// <summary>An inset on the left and right only, for a cell that sets its own height.</summary>
        /// <param name="value">The inset in points.</param>
        /// <returns>The offset.</returns>
        internal static RectOffset HorizontalPadding(int value) => new(value, value, 0, 0);

        /// <summary>A style whose background is a rounded rectangle in the given color.</summary>
        /// <param name="color">Fill color of the rectangle.</param>
        /// <param name="radius">Corner radius in points.</param>
        /// <returns>The style.</returns>
        internal GUIStyle RoundedStyle(Color color, int radius)
        {
            GUIStyle style = new()
            {
                border = Uniform(radius),
                normal =
                {
                    background = RoundedTexture(color, radius)
                }
            };

            return style;
        }

        /// <summary>A small bold status pill.</summary>
        /// <param name="background">Fill color of the pill.</param>
        /// <param name="text">Color of the text on it.</param>
        /// <param name="radius">Corner radius in points.</param>
        /// <param name="paddingX">Inset at the left and right of the text.</param>
        /// <param name="paddingY">Inset above and below the text.</param>
        /// <returns>The style.</returns>
        internal GUIStyle PillStyle(Color background, Color text, int radius, int paddingX, int paddingY)
        {
            GUIStyle style = RoundedStyle(background, radius);

            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = EditorStyles.miniLabel.fontSize;
            style.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            style.normal.textColor = text;

            return style;
        }

        /// <summary>A filled button that lifts on hover and drops when pressed.</summary>
        /// <param name="background">Fill color at rest.</param>
        /// <param name="textColor">Color of the label, pinned across every state.</param>
        /// <param name="fontStyle">Weight of the label.</param>
        /// <param name="radius">Corner radius in points.</param>
        /// <param name="hoverLift">How much lighter the fill goes under the mouse.</param>
        /// <param name="pressDrop">How much darker the fill goes while held.</param>
        /// <returns>The style.</returns>
        internal GUIStyle ButtonStyle(Color background, Color textColor, FontStyle fontStyle, int radius,
            float hoverLift, float pressDrop)
        {
            GUIStyle style = RoundedStyle(background, radius);

            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = fontStyle;

            PinTextColor(style, textColor);

            style.hover.background = RoundedTexture(Shift(background, hoverLift), radius);
            style.active.background = RoundedTexture(Shift(background, -pressDrop), radius);
            style.focused.background = style.normal.background;

            return style;
        }

        /// <summary>
        /// A 9-sliced rounded rect texture: a (2r+1) square whose one pixel center stretches, so only
        /// the rounded corners are drawn at their true size regardless of the target rectangle.
        /// </summary>
        /// <param name="color">Fill color of the rectangle.</param>
        /// <param name="radius">Corner radius in points.</param>
        /// <returns>The texture, which this builder owns.</returns>
        internal Texture2D RoundedTexture(Color color, int radius)
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

        /// <summary>Destroys every texture handed out so far.</summary>
        internal void Release()
        {
            foreach (Texture2D texture in _ownedTextures)
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }

            _ownedTextures.Clear();
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

        private static float Square(float value) => value * value;
    }
}