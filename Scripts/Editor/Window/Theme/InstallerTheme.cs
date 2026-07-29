using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window.Theme
{
    /// <summary>
    /// Single source of every spacing, size and color the installer window draws with.
    /// Views never hold a raw number of their own; they read from here so the look can be
    /// tuned in one place and stays consistent across the dark and light editor skins.
    /// </summary>
    internal static class InstallerTheme
    {
        /// <summary>Pixel spacings, sizes and font sizes for the installer layout.</summary>
        internal static class Metrics
        {
            internal const float ActionButtonHeight = 32f;
            internal const int CardCornerRadius = 6;
            internal const int CardVerticalPadding = 6;
            internal const int CellTextPadding = 4;
            internal const float ClearButtonWidth = 60f;
            internal const float DefaultNameColumnWidth = 180f;
            internal const float DefaultStatusColumnWidth = 112f;
            internal const int DescriptionFontSize = 11;
            internal const float DividerHitWidth = 8f;
            internal const float DividerThickness = 1f;
            internal const float EditListButtonWidth = 72f;

            /// <summary>How much a button background brightens while hovered.</summary>
            internal const float HoverLift = 0.06f;

            internal const float ItemSpacing = 8f;
            internal const float MinNameColumnWidth = 90f;
            internal const float MinStatusColumnWidth = 72f;
            internal const float MinVersionColumnWidth = 60f;
            internal const int PillCornerRadius = 8;
            internal const float PillHeight = 18f;
            internal const int PillPaddingX = 8;
            internal const int PillPaddingY = 2;

            /// <summary>How much a button background darkens while pressed.</summary>
            internal const float PressDrop = 0.08f;

            internal const float RefreshButtonWidth = 72f;
            internal const float RowHeight = 22f;
            internal const float SecondaryButtonHeight = 22f;
            internal const float SectionSpacing = 12f;
            internal const float SelectionColumnWidth = 22f;
            internal const float SeparatorThickness = 1f;
            internal const float SettingsPageSpacing = 4f;
            internal const float TableEdgeInset = 6f;
            internal const float TightSpacing = 4f;
            internal const int TitleFontSize = 15;
            internal const float ToggleSize = 16f;
            internal const float ToolbarButtonHeight = 20f;
        }

        /// <summary>Palette with separate values for the dark (pro) and light editor skins.</summary>
        internal static class Palette
        {
            internal static Color Title => Pick(new Color(0.90f, 0.90f, 0.92f), new Color(0.13f, 0.13f, 0.15f));

            internal static Color Description => Pick(new Color(0.62f, 0.62f, 0.66f), new Color(0.38f, 0.38f, 0.42f));

            internal static Color Accent => Pick(new Color(0.32f, 0.60f, 0.94f), new Color(0.20f, 0.48f, 0.86f));

            internal static Color AccentText => Color.white;

            internal static Color Secondary => Pick(new Color(0.30f, 0.30f, 0.33f), new Color(0.89f, 0.89f, 0.91f));

            internal static Color SecondaryText => Pick(new Color(0.86f, 0.86f, 0.88f), new Color(0.18f, 0.18f, 0.20f));

            internal static Color InstalledText => Pick(new Color(0.55f, 0.88f, 0.58f), new Color(0.14f, 0.52f, 0.22f));

            internal static Color InstalledPill => Pick(new Color(0.20f, 0.36f, 0.22f), new Color(0.80f, 0.93f, 0.81f));

            internal static Color NotInstalledText
                => Pick(new Color(0.74f, 0.74f, 0.77f), new Color(0.40f, 0.40f, 0.44f));

            internal static Color NotInstalledPill
                => Pick(new Color(0.30f, 0.30f, 0.32f), new Color(0.88f, 0.88f, 0.90f));

            internal static Color CheckingText => Pick(new Color(0.62f, 0.62f, 0.66f), new Color(0.45f, 0.45f, 0.50f));

            internal static Color Card => Pick(new Color(0.22f, 0.22f, 0.24f), new Color(0.85f, 0.85f, 0.87f));

            internal static Color RowStripe => Pick(new Color(1f, 1f, 1f, 0.03f), new Color(0f, 0f, 0f, 0.03f));

            internal static Color Separator => Pick(new Color(1f, 1f, 1f, 0.06f), new Color(0f, 0f, 0f, 0.08f));

            internal static Color Divider => Pick(new Color(0f, 0f, 0f, 0.35f), new Color(0f, 0f, 0f, 0.16f));

            internal static Color DividerActive => Accent;

            private static Color Pick(Color pro, Color personal) => EditorGUIUtility.isProSkin
                ? pro
                : personal;
        }
    }
}