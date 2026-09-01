using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Single source of every spacing, size and color the Package Defaults window draws with, so the
    /// look can be tuned in one place and stays readable in both the dark and light editor skins.
    /// </summary>
    internal static class PackageDefaultsTheme
    {
        /// <summary>Pixel spacings, sizes and font sizes for the window layout.</summary>
        internal static class Metrics
        {
            /// <summary>Height of the footer buttons.</summary>
            internal const float ButtonHeight = 24f;
            /// <summary>Corner radius of the cards, pills and buttons.</summary>
            internal const int CardCornerRadius = 6;
            /// <summary>Inset of the content inside a card.</summary>
            internal const int CardPadding = 8;
            /// <summary>Inset of the text inside a table cell.</summary>
            internal const int CellPadding = 6;
            /// <summary>Share of the row the package name takes, leaving the rest to its dependencies.</summary>
            internal const float DependencyColumnSplit = 0.34f;
            /// <summary>Font size of the paragraph under the window title.</summary>
            internal const int DescriptionFontSize = 11;
            /// <summary>Width of the Copy and Write buttons in the footer.</summary>
            internal const float FooterButtonWidth = 130f;
            /// <summary>Width of the diff gutter that carries the plus and minus marks.</summary>
            internal const float GutterWidth = 18f;

            /// <summary>How much a button background brightens while hovered.</summary>
            internal const float HoverLift = 0.06f;

            /// <summary>Matches the height of the text field an inline button sits next to.</summary>
            internal const float InlineButtonHeight = 18f;

            /// <summary>Width of the Browse and Scan buttons that sit beside a text field.</summary>
            internal const float InlineButtonWidth = 70f;

            /// <summary>Gap between two controls that belong together.</summary>
            internal const float ItemSpacing = 8f;
            /// <summary>Smallest height the window may be resized to.</summary>
            internal const float MinimumHeight = 520f;
            /// <summary>Smallest width the window may be resized to. The diff needs the room.</summary>
            internal const float MinimumWidth = 720f;
            /// <summary>Corner radius of a status pill, rounder than a card so it reads as a pill.</summary>
            internal const int PillCornerRadius = 8;
            /// <summary>Height of a status pill.</summary>
            internal const float PillHeight = 18f;
            /// <summary>Inset left and right of the pill text.</summary>
            internal const int PillPaddingX = 8;
            /// <summary>Inset above and below the pill text.</summary>
            internal const int PillPaddingY = 2;

            /// <summary>How much a button background darkens while pressed.</summary>
            internal const float PressDrop = 0.08f;

            /// <summary>Height of one row in the dependency table and the diff.</summary>
            internal const float RowHeight = 20f;
            /// <summary>Width kept free for the vertical scroll bar, so a long line does not sit under it.</summary>
            internal const float ScrollBarAllowance = 16f;
            /// <summary>Gap between two controls that sit close together.</summary>
            internal const float TightSpacing = 4f;
            /// <summary>Font size of the window title.</summary>
            internal const int TitleFontSize = 15;
            /// <summary>Height of the tab strip.</summary>
            internal const float ToolbarHeight = 22f;
            /// <summary>Inset of everything from the window edges.</summary>
            internal const int WindowPadding = 10;
        }

        /// <summary>Palette with separate values for the dark (pro) and light editor skins.</summary>
        internal static class Palette
        {
            /// <summary>Text color of the window title and the section headers.</summary>
            internal static Color Title => Pick(new Color(0.90f, 0.90f, 0.92f), new Color(0.13f, 0.13f, 0.15f));

            /// <summary>Text color of secondary text.</summary>
            internal static Color Description => Pick(new Color(0.62f, 0.62f, 0.66f), new Color(0.38f, 0.38f, 0.42f));

            /// <summary>The one strong color, used by the primary button and the selected tab.</summary>
            internal static Color Accent => Pick(new Color(0.32f, 0.60f, 0.94f), new Color(0.20f, 0.48f, 0.86f));

            /// <summary>Text drawn on top of the accent color.</summary>
            internal static Color AccentText => Color.white;

            /// <summary>Background of the secondary buttons.</summary>
            internal static Color Secondary => Pick(new Color(0.30f, 0.30f, 0.33f), new Color(0.89f, 0.89f, 0.91f));

            /// <summary>Text drawn on top of the secondary color.</summary>
            internal static Color SecondaryText => Pick(new Color(0.86f, 0.86f, 0.88f), new Color(0.18f, 0.18f, 0.20f));

            /// <summary>Background of a card.</summary>
            internal static Color Card => Pick(new Color(0.22f, 0.22f, 0.24f), new Color(0.85f, 0.85f, 0.87f));

            /// <summary>

            /// Background of the card the generated source is shown in, darker than a plain card.

            /// </summary>
            internal static Color Code => Pick(new Color(0.16f, 0.16f, 0.18f), new Color(0.96f, 0.96f, 0.97f));

            /// <summary>Text color of the generated source.</summary>
            internal static Color CodeText => Pick(new Color(0.82f, 0.84f, 0.86f), new Color(0.15f, 0.16f, 0.18f));

            /// <summary>Overlay on every second row, which is what draws the zebra striping.</summary>
            internal static Color RowStripe => Pick(new Color(1f, 1f, 1f, 0.03f), new Color(0f, 0f, 0f, 0.03f));

            /// <summary>Background of the pill reporting that the file on disk is already up to date.</summary>
            internal static Color OkPill => Pick(new Color(0.20f, 0.36f, 0.22f), new Color(0.80f, 0.93f, 0.81f));

            /// <summary>Text color of the up to date pill.</summary>
            internal static Color OkText => Pick(new Color(0.55f, 0.88f, 0.58f), new Color(0.14f, 0.52f, 0.22f));

            /// <summary>Background of the pill reporting that the file differs from what was scanned.</summary>
            internal static Color WarnPill => Pick(new Color(0.40f, 0.33f, 0.16f), new Color(0.99f, 0.91f, 0.75f));

            /// <summary>Text color of the differs pill.</summary>
            internal static Color WarnText => Pick(new Color(0.95f, 0.80f, 0.45f), new Color(0.51f, 0.36f, 0.05f));

            /// <summary>Background of the pill reporting that nothing has been scanned yet.</summary>
            internal static Color MutedPill => Pick(new Color(0.30f, 0.30f, 0.32f), new Color(0.88f, 0.88f, 0.90f));

            /// <summary>Text color of the muted pill.</summary>
            internal static Color MutedText => Pick(new Color(0.74f, 0.74f, 0.77f), new Color(0.40f, 0.40f, 0.44f));

            /// <summary>Background of a diff line the generated file adds.</summary>
            internal static Color AddedRow => Pick(new Color(0.22f, 0.42f, 0.24f, 0.45f),
                new Color(0.78f, 0.94f, 0.79f, 0.85f));

            /// <summary>Text color of an added diff line.</summary>
            internal static Color AddedText => Pick(new Color(0.62f, 0.92f, 0.65f), new Color(0.10f, 0.42f, 0.16f));

            /// <summary>Background of a diff line the generated file removes.</summary>
            internal static Color RemovedRow => Pick(new Color(0.46f, 0.22f, 0.24f, 0.45f),
                new Color(0.98f, 0.82f, 0.83f, 0.85f));

            /// <summary>Text color of a removed diff line.</summary>
            internal static Color RemovedText => Pick(new Color(0.96f, 0.62f, 0.63f), new Color(0.55f, 0.11f, 0.14f));

            private static Color Pick(Color pro, Color personal) => EditorGUIUtility.isProSkin
                ? pro
                : personal;
        }
    }
}