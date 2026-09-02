using Base.PackageInstaller.Shared;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window.Theme
{
    /// <summary>
    /// Single source of every spacing, size and color the installer window draws with.
    /// Views never hold a raw number of their own; they read from here so the look can be
    /// tuned in one place and stays consistent across the dark and light editor skins.
    /// </summary>
    /// <remarks>
    /// Anything the Editor UI package also names is read from there through
    /// <see cref="EditorUIBridge"/> once that package is installed, so the installer follows the same
    /// theme as every other Base window. The values written here are what it looks like on its own,
    /// in a project that has nothing installed yet. Anything only this window understands, such as a
    /// column width or the mode switch, is written here and stays here.
    /// </remarks>
    internal static class InstallerTheme
    {
        /// <summary>Pixel spacings, sizes and font sizes for the installer layout.</summary>
        internal static class Metrics
        {
            /// <summary>Height of the primary action button at the bottom of the window.</summary>
            internal const float ActionButtonHeight = 32f;

            /// <summary>Inset above and below the card content, so the first row is not flush with the edge.</summary>
            internal const int CardVerticalPadding = 6;

            /// <summary>Inset of the text inside a table cell, which the status pill matches.</summary>
            internal const int CellTextPadding = 4;

            /// <summary>Width of the Clear button in the result footer.</summary>
            internal const float ClearButtonWidth = 60f;

            /// <summary>Width of the Copy button in the result footer.</summary>
            internal const float CopyButtonWidth = 56f;

            /// <summary>Width the Package column starts at before the user drags it.</summary>
            internal const float DefaultNameColumnWidth = 180f;

            /// <summary>Width the Required By column starts at.</summary>
            internal const float DefaultRequiredColumnWidth = 150f;

            /// <summary>Width the Status column starts at.</summary>
            internal const float DefaultStatusColumnWidth = 112f;

            /// <summary>Width of the Edit List button in the toolbar.</summary>
            internal const float EditListButtonWidth = 72f;

            /// <summary>Narrowest the Package column may be dragged.</summary>
            internal const float MinNameColumnWidth = 90f;

            /// <summary>Narrowest the Required By column may be dragged.</summary>
            internal const float MinRequiredColumnWidth = 80f;

            /// <summary>Narrowest the Status column may be dragged.</summary>
            internal const float MinStatusColumnWidth = 72f;

            /// <summary>Narrowest the Version column may become. It takes whatever the others leave.</summary>
            internal const float MinVersionColumnWidth = 60f;

            /// <summary>Inset left and right of the pill text.</summary>
            internal const int PillPaddingX = 8;

            /// <summary>Inset above and below the pill text.</summary>
            internal const int PillPaddingY = 2;

            /// <summary>Width of the Refresh button in the toolbar.</summary>
            internal const float RefreshButtonWidth = 72f;

            /// <summary>Height of the Select All and Deselect All buttons.</summary>
            internal const float SecondaryButtonHeight = 22f;

            /// <summary>Height of one segment of the install and uninstall switch.</summary>
            internal const float SegmentHeight = 24f;

            /// <summary>Inset of the segments inside the rail they sit in.</summary>
            internal const int SegmentTrackPadding = 2;

            /// <summary>Width of the fixed toggle column, which the user cannot resize.</summary>
            internal const float SelectionColumnWidth = 22f;

            /// <summary>Gap between the blocks on the project settings page.</summary>
            internal const float SettingsPageSpacing = 4f;

            /// <summary>Inset of the columns from the card edge, left and right.</summary>
            internal const float TableEdgeInset = 6f;

            /// <summary>Size of the selection toggle, centered in its cell.</summary>
            internal const float ToggleSize = 16f;

            /// <summary>Height of the buttons in the packages toolbar.</summary>
            internal const float ToolbarButtonHeight = 20f;

            /// <summary>Inset of everything from the window edges.</summary>
            internal const int WindowPadding = 6;

            /// <summary>Corner radius of the card, the buttons and the segment rail.</summary>
            internal static int CardCornerRadius => EditorUIBridge.Metric("CardCornerRadius", 6);

            /// <summary>Font size of the paragraph under the window title.</summary>
            internal static int DescriptionFontSize => EditorUIBridge.Metric("DescriptionFontSize", 11);

            /// <summary>How wide the grab area of a column divider is, which is wider than the line itself.</summary>
            internal static float DividerHitWidth => EditorUIBridge.Metric("DividerHitWidth", 8f);

            /// <summary>Drawn width of a column divider.</summary>
            internal static float DividerThickness => EditorUIBridge.Metric("DividerThickness", 1f);

            /// <summary>How much a button background brightens while hovered.</summary>
            internal static float HoverLift => EditorUIBridge.Metric("HoverLift", 0.06f);

            /// <summary>Gap between two controls that belong together.</summary>
            internal static float ItemSpacing => EditorUIBridge.Metric("ItemGap", 8f);

            /// <summary>Corner radius of a status pill, rounder than a card so it reads as a pill.</summary>
            internal static int PillCornerRadius => EditorUIBridge.Metric("PillCornerRadius", 8);

            /// <summary>Height of a status pill.</summary>
            internal static float PillHeight => EditorUIBridge.Metric("PillHeight", 18f);

            /// <summary>How much a button background darkens while pressed.</summary>
            internal static float PressDrop => EditorUIBridge.Metric("PressDrop", 0.08f);

            /// <summary>Height of one table row, and of the column header.</summary>
            internal static float RowHeight => EditorUIBridge.Metric("RowHeight", 22f);

            /// <summary>Gap between two sections of the window.</summary>
            internal static float SectionSpacing => EditorUIBridge.Metric("SectionGap", 12f);

            /// <summary>Drawn height of the hairline under the column header.</summary>
            internal static float SeparatorThickness => EditorUIBridge.Metric("SeparatorThickness", 1f);

            /// <summary>Gap between two controls that sit close together.</summary>
            internal static float TightSpacing => EditorUIBridge.Metric("TightGap", 4f);

            /// <summary>Font size of the window title.</summary>
            internal static int TitleFontSize => EditorUIBridge.Metric("TitleFontSize", 15);
        }

        /// <summary>Palette with separate values for the dark (pro) and light editor skins.</summary>
        internal static class Palette
        {
            /// <summary>Text color of the window title and the section headers.</summary>
            internal static Color Title => EditorUIBridge.PaletteColor("Text",
                Pick(new Color(0.90f, 0.90f, 0.92f), new Color(0.13f, 0.13f, 0.15f)));

            /// <summary>
            /// Text color of secondary text: the description, the column headers, the held-by column.
            /// </summary>
            internal static Color Description => EditorUIBridge.PaletteColor("DimText",
                Pick(new Color(0.62f, 0.62f, 0.66f), new Color(0.38f, 0.38f, 0.42f)));

            /// <summary>The one strong color, used by the primary button and the dragged divider.</summary>
            internal static Color Accent => EditorUIBridge.PaletteColor("Accent",
                Pick(new Color(0.32f, 0.60f, 0.94f), new Color(0.20f, 0.48f, 0.86f)));

            /// <summary>Text drawn on top of the accent color.</summary>
            internal static Color AccentText => EditorUIBridge.PaletteColor("AccentText", Color.white);

            /// <summary>Background of the toolbar and selection buttons.</summary>
            internal static Color Secondary => EditorUIBridge.PaletteColor("Secondary",
                Pick(new Color(0.30f, 0.30f, 0.33f), new Color(0.89f, 0.89f, 0.91f)));

            /// <summary>Text drawn on top of the secondary color.</summary>
            internal static Color SecondaryText => EditorUIBridge.PaletteColor("SecondaryText",
                Pick(new Color(0.86f, 0.86f, 0.88f), new Color(0.18f, 0.18f, 0.20f)));

            /// <summary>Text color of the installed pill.</summary>
            internal static Color InstalledText => EditorUIBridge.PaletteColor("Success",
                Pick(new Color(0.55f, 0.88f, 0.58f), new Color(0.14f, 0.52f, 0.22f)));

            /// <summary>Background of the installed pill.</summary>
            internal static Color InstalledPill => EditorUIBridge.TableColor("OkBadgeColor",
                Pick(new Color(0.20f, 0.36f, 0.22f), new Color(0.80f, 0.93f, 0.81f)));

            /// <summary>Text color of the not installed pill.</summary>
            internal static Color NotInstalledText => EditorUIBridge.PaletteColor("DimText",
                Pick(new Color(0.74f, 0.74f, 0.77f), new Color(0.40f, 0.40f, 0.44f)));

            /// <summary>Background of the not installed pill.</summary>
            internal static Color NotInstalledPill => EditorUIBridge.TableColor("NeutralBadgeColor",
                Pick(new Color(0.30f, 0.30f, 0.32f), new Color(0.88f, 0.88f, 0.90f)));

            /// <summary>Text color of the placeholder shown while install statuses are still being queried.</summary>
            internal static Color CheckingText => EditorUIBridge.PaletteColor("DimText",
                Pick(new Color(0.62f, 0.62f, 0.66f), new Color(0.45f, 0.45f, 0.50f)));

            /// <summary>Background of the card the table sits in.</summary>
            internal static Color Card => EditorUIBridge.PaletteColor("Card",
                Pick(new Color(0.22f, 0.22f, 0.24f), new Color(0.85f, 0.85f, 0.87f)));

            /// <summary>The recessed rail the mode segments sit in, one step behind the card.</summary>
            internal static Color SegmentTrack => EditorUIBridge.PaletteColor("Field",
                Pick(new Color(0.17f, 0.17f, 0.19f), new Color(0.78f, 0.78f, 0.80f)));

            /// <summary>A segment the pointer is over but which is not the active one.</summary>
            internal static Color SegmentHover => EditorUIBridge.PaletteColor("Hover",
                Pick(new Color(1f, 1f, 1f, 0.06f), new Color(0f, 0f, 0f, 0.05f)));

            /// <summary>Overlay on every second row, which is what draws the zebra striping.</summary>
            internal static Color RowStripe => EditorUIBridge.PaletteColor("Stripe",
                Pick(new Color(1f, 1f, 1f, 0.03f), new Color(0f, 0f, 0f, 0.03f)));

            /// <summary>The hairline under the column header.</summary>
            internal static Color Separator => EditorUIBridge.PaletteColor("Separator",
                Pick(new Color(1f, 1f, 1f, 0.06f), new Color(0f, 0f, 0f, 0.08f)));

            /// <summary>A column divider at rest.</summary>
            internal static Color Divider => EditorUIBridge.PaletteColor("Divider",
                Pick(new Color(0f, 0f, 0f, 0.35f), new Color(0f, 0f, 0f, 0.16f)));

            /// <summary>A column divider while it is being dragged.</summary>
            internal static Color DividerActive => Accent;

            private static Color Pick(Color pro, Color personal) => EditorGUIUtility.isProSkin
                ? pro
                : personal;
        }
    }
}