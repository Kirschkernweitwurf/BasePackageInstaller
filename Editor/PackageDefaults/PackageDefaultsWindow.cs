using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Generates the installer's package list from the assembly definitions in the Base packages
    /// repository.
    /// <para>
    /// This is a personal tool and deliberately lives outside every package: it reads the packages
    /// from disk by path, so it never needs them installed, and nothing here should ship to a
    /// consuming project. It therefore has no Base package dependency at all.
    /// </para>
    /// <para>
    /// The installer cannot work the graph out for itself, because it has to know what a package
    /// needs before that package is anywhere on disk. Generating the list here and checking the
    /// result into the installer keeps the asmdefs as the single source of truth.
    /// </para>
    /// </summary>
    internal sealed class PackageDefaultsWindow : EditorWindow
    {
        private const string AddedPrefix = "+";
        private const string AutoRunLabel = "Run On Project Open";
        private const string BrowseLabel = "Browse";
        private const string CopyLabel = "Copy to Clipboard";
        private const string Description = "Reads every asmdef under the packages root, resolves the references "
            + "between packages and drops the edges another edge already implies. Optional assemblies behind a "
            + "define constraint and test assemblies are ignored, so they never become hard dependencies.";
        private const string DiffTab = "Diff";
        private const string EmptyState = "Nothing scanned yet. Check the packages root and press Scan.";
        private const string GraphTab = "Dependency Graph";
        private const string IdenticalMessage = "Identical to the file on disk";

        // The Menu Manager lives in the Tools package, which this tool deliberately does not depend on,
        // so this is one of two places a plain MenuItem is used instead of DynamicMenuItem. The path is
        // spelled out rather than built from GitPackageManager.MenuRoot, because that root is private
        // and this window is meant to stay independent of the installer window.
        private const string MenuPath = "Tools/Installer/Package Defaults";
        private const int MenuPriority = -14;

        private const string MissingMessage = "Target file does not exist yet";
        private const string NoDependencies = "none";
        private const string NoTargetMessage = "No target file selected";
        private const string OutputFileName = "BasePackageDefaults.cs";
        private const string PreviewTab = "Generated File";
        private const string RemovedPrefix = "-";
        private const string RootLabel = "Packages Root";
        private const string SaveDialogTitle = "Save generated defaults";
        private const string ScanLabel = "Scan";
        private const string ScriptFilter = "cs";
        private const string TargetLabel = "Target File";
        private const string UpToDateMessage = "Nothing to write, the file is already up to date.";
        private const string WindowTitle = "Package Defaults";
        private const string WriteLabel = "Write File";

        private static readonly string[] TabLabels =
        {
            GraphTab,
            PreviewTab,
            DiffTab
        };

        private readonly PackageDefaultsStyles _styles = new();

        private PackageDependencyInfo[] _packages;
        private DiffResult _diff;
        private string[] _previewLines = Array.Empty<string>();
        private string _preview;
        private string _root;
        private string _target;
        private EDefaultsTab _tab;
        private Vector2 _scroll;

#region Unity Callbacks
        private void OnEnable()
        {
            // Remembered per machine, so the path constants are only the first-run fallback.
            _root = PackageDefaultsPaths.LoadRoot();
            _target = PackageDefaultsPaths.LoadTarget();

            // Not persisted: a path found by searching should follow the project rather than
            // stick around after the installer moved. Only an explicit pick is remembered.
            if (string.IsNullOrEmpty(_target))
                _target = PackageDefaultsPaths.LocateTarget();

            Scan();
        }

        private void OnGUI()
        {
            _styles.EnsureBuilt();

            // One padded wrapper around everything, so no control ever sits flush against the
            // window edge and the scroll bar keeps a margin of its own.
            using (new EditorGUILayout.VerticalScope(_styles.Window))
            {
                DrawHeader();
                DrawStatus();
                DrawTabs();

                // The scroll view takes the height left over between the header and the footer, so
                // the buttons stay pinned to the bottom instead of scrolling away with the content.
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

                DrawContent();

                EditorGUILayout.EndScrollView();

                DrawFooter();
            }
        }

        private void OnDisable() => _styles.Dispose();
#endregion

        [MenuItem(MenuPath, priority = MenuPriority)]
        private static void Open()
        {
            PackageDefaultsWindow window = GetWindow<PackageDefaultsWindow>(WindowTitle);

            window.minSize = new Vector2(PackageDefaultsTheme.Metrics.MinimumWidth,
                PackageDefaultsTheme.Metrics.MinimumHeight);

            window.Show();
        }

        private static string Describe(PackageDependencyInfo package)
        {
            if (package.DirectDependencies.Count == 0)
                return NoDependencies;

            return string.Join(", ", package.DirectDependencies);
        }

        private static Rect Reserve(float width, float height)
            => GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));

        private void DrawHeader()
        {
            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.ItemSpacing);

            GUILayout.Label(WindowTitle, _styles.Title);
            GUILayout.Label(Description, _styles.Description);

            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.ItemSpacing);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                string root = EditorGUILayout.TextField(RootLabel, _root);

                if (EditorGUI.EndChangeCheck())
                {
                    _root = root;
                    PackageDefaultsPaths.SaveRoot(root);
                }

                if (GUILayout.Button(ScanLabel, _styles.SecondaryButton,
                        GUILayout.Width(PackageDefaultsTheme.Metrics.InlineButtonWidth),
                        GUILayout.Height(PackageDefaultsTheme.Metrics.InlineButtonHeight)))
                    Scan();
            }

            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.TightSpacing);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                string target = EditorGUILayout.TextField(TargetLabel, _target);

                if (EditorGUI.EndChangeCheck())
                    SetTarget(target);

                if (GUILayout.Button(BrowseLabel, _styles.SecondaryButton,
                        GUILayout.Width(PackageDefaultsTheme.Metrics.InlineButtonWidth),
                        GUILayout.Height(PackageDefaultsTheme.Metrics.InlineButtonHeight)))
                    Browse();
            }

            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.TightSpacing);

            EditorGUI.BeginChangeCheck();

            bool autoRun = EditorGUILayout.Toggle(AutoRunLabel, PackageDefaultsAutoRun.IsEnabled);

            if (EditorGUI.EndChangeCheck())
                PackageDefaultsAutoRun.IsEnabled = autoRun;
        }

        private void DrawStatus()
        {
            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.ItemSpacing);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUIContent content = new(StatusText());
                GUIStyle style = StatusStyle();

                Rect pill = Reserve(style.CalcSize(content).x, PackageDefaultsTheme.Metrics.PillHeight);

                GUI.Label(pill, content, style);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.ItemSpacing);
        }

        private void DrawTabs()
        {
            // GUILayout.Toolbar sizes itself to its content rather than the window, so the rectangle is
            // reserved first and the toolbar drawn into it to make it span the full width.
            Rect area = GUILayoutUtility.GetRect(0f, PackageDefaultsTheme.Metrics.ToolbarHeight,
                GUILayout.ExpandWidth(true));

            _tab = (EDefaultsTab)GUI.Toolbar(area, (int)_tab, TabLabels);

            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.ItemSpacing);
        }

        private void DrawContent()
        {
            if (_packages == null || _packages.Length == 0)
            {
                EditorGUILayout.HelpBox(EmptyState, MessageType.Info);
                return;
            }

            switch (_tab)
            {
                case EDefaultsTab.Preview:
                    DrawPreview();
                    break;

                case EDefaultsTab.Diff:
                    DrawDiff();
                    break;

                default:
                    DrawGraph();
                    break;
            }
        }

        private void DrawGraph()
        {
            using (new EditorGUILayout.VerticalScope(_styles.Card))
            {
                for (int i = 0; i < _packages.Length; i++)
                {
                    PackageDependencyInfo package = _packages[i];

                    Rect row = GUILayoutUtility.GetRect(0f, PackageDefaultsTheme.Metrics.RowHeight,
                        GUILayout.ExpandWidth(true));

                    if (Event.current.type == EventType.Repaint && i % 2 != 0)
                        EditorGUI.DrawRect(row, PackageDefaultsTheme.Palette.RowStripe);

                    float nameWidth = row.width * PackageDefaultsTheme.Metrics.DependencyColumnSplit;

                    Rect name = new(row.x, row.y, nameWidth, row.height);
                    Rect list = new(name.xMax, row.y, row.width - nameWidth, row.height);

                    GUI.Label(name, package.DisplayName, _styles.RowLabel);
                    GUI.Label(list, Describe(package), _styles.RowValue);
                }
            }
        }

        private void DrawPreview()
        {
            // Measured once per frame rather than once per line: the widest line decides the width of
            // every row, so asking for it inside the loop would rescan the whole file on every row.
            float width = ContentWidth(_previewLines);

            using (new EditorGUILayout.VerticalScope(_styles.CodeCard))
            {
                foreach (string line in _previewLines)
                    DrawCodeLine(line, EDiffKind.Unchanged, width);
            }
        }

        private void DrawDiff()
        {
            if (_diff.Lines.Count == 0)
            {
                EditorGUILayout.HelpBox(StatusText(), MessageType.Info);
                return;
            }

            float width = ContentWidth(_diff.Lines);

            using (new EditorGUILayout.VerticalScope(_styles.CodeCard))
            {
                foreach (DiffLine line in _diff.Lines)
                    DrawCodeLine(line.Text, line.Kind, width);
            }
        }

        // Every code row is measured and reserved explicitly. Letting the layout system guess is what
        // clipped the old preview: it sized the text area to a minimum height and the rest was simply
        // cut off, with no way to scroll to it.
        private void DrawCodeLine(string text, EDiffKind kind, float width)
        {
            Rect row = GUILayoutUtility.GetRect(width, PackageDefaultsTheme.Metrics.RowHeight,
                GUILayout.ExpandWidth(false));

            if (Event.current.type == EventType.Repaint && kind != EDiffKind.Unchanged)
                EditorGUI.DrawRect(row, kind == EDiffKind.Added
                    ? PackageDefaultsTheme.Palette.AddedRow
                    : PackageDefaultsTheme.Palette.RemovedRow);

            Rect gutter = new(row.x, row.y, PackageDefaultsTheme.Metrics.GutterWidth, row.height);
            Rect body = new(gutter.xMax, row.y, row.width - gutter.width, row.height);

            if (kind == EDiffKind.Added)
                GUI.Label(gutter, AddedPrefix, _styles.AddedGutter);
            else if (kind == EDiffKind.Removed)
                GUI.Label(gutter, RemovedPrefix, _styles.RemovedGutter);

            GUI.Label(body, text, _styles.Code);
        }

        // The widest line decides the content width, so long lines scroll sideways instead of wrapping
        // and throwing the measured height off.
        private float ContentWidth(IReadOnlyList<DiffLine> lines)
        {
            float widest = 0f;

            foreach (DiffLine line in lines)
                widest = Mathf.Max(widest, _styles.Code.CalcSize(new GUIContent(line.Text)).x);

            return Measured(widest);
        }

        private float ContentWidth(IReadOnlyList<string> lines)
        {
            float widest = 0f;

            foreach (string line in lines)
                widest = Mathf.Max(widest, _styles.Code.CalcSize(new GUIContent(line)).x);

            return Measured(widest);
        }

        private float Measured(float widest)
        {
            float available = position.width
                - PackageDefaultsTheme.Metrics.CardPadding * 2f
                - PackageDefaultsTheme.Metrics.ScrollBarAllowance;

            return Mathf.Max(widest + PackageDefaultsTheme.Metrics.GutterWidth, available);
        }

        private string StatusText() => _diff.State switch
        {
            EDiffState.Identical => IdenticalMessage,
            EDiffState.Missing => MissingMessage,
            EDiffState.Changed => $"{_diff.AddedCount} added, {_diff.RemovedCount} removed",
            _ => NoTargetMessage
        };

        private GUIStyle StatusStyle() => _diff.State switch
        {
            EDiffState.Identical => _styles.OkPill,
            EDiffState.Changed => _styles.WarnPill,
            _ => _styles.MutedPill
        };

        private void DrawFooter()
        {
            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.ItemSpacing);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_preview)))
                {
                    if (GUILayout.Button(CopyLabel, _styles.SecondaryButton,
                            GUILayout.Width(PackageDefaultsTheme.Metrics.FooterButtonWidth),
                            GUILayout.Height(PackageDefaultsTheme.Metrics.ButtonHeight)))
                        EditorGUIUtility.systemCopyBuffer = _preview;

                    GUILayout.Space(PackageDefaultsTheme.Metrics.TightSpacing);

                    if (GUILayout.Button(WriteLabel, _styles.PrimaryButton,
                            GUILayout.Width(PackageDefaultsTheme.Metrics.FooterButtonWidth),
                            GUILayout.Height(PackageDefaultsTheme.Metrics.ButtonHeight)))
                        Write();
                }
            }

            EditorGUILayout.Space(PackageDefaultsTheme.Metrics.ItemSpacing);
        }

        private void Scan()
        {
            _packages = PackageDependencyScanner.Scan(_root);

            _preview = _packages.Length == 0
                ? string.Empty
                : PackageDefaultsWriter.Render(_packages);

            _previewLines = _preview.Length == 0
                ? Array.Empty<string>()
                : _preview.Replace("\r\n", "\n").Split('\n');

            RefreshDiff();
            Repaint();
        }

        private void RefreshDiff()
        {
            _diff = TextDiff.Compare(_preview, PackageDefaultsFile.Read(_target),
                hasTarget: !string.IsNullOrEmpty(_target));

            Repaint();
        }

        private void SetTarget(string path)
        {
            _target = path;

            PackageDefaultsPaths.SaveTarget(path);
            RefreshDiff();
        }

        private void Browse()
        {
            string directory = string.IsNullOrEmpty(_target)
                ? string.Empty
                : Path.GetDirectoryName(_target);

            string path = EditorUtility.SaveFilePanel(SaveDialogTitle, directory, OutputFileName, ScriptFilter);

            if (string.IsNullOrEmpty(path))
                return;

            SetTarget(path);
        }

        private void Write()
        {
            if (string.IsNullOrEmpty(_target))
            {
                Browse();

                if (string.IsNullOrEmpty(_target))
                    return;
            }

            if (_diff.State == EDiffState.Identical)
            {
                Debug.Log($"{nameof(PackageDefaultsWindow)}: {UpToDateMessage}");
                return;
            }

            PackageDefaultsFile.Write(_target, _preview);
            RefreshDiff();

            Debug.Log($"{nameof(PackageDefaultsWindow)}: wrote {Path.GetFileName(_target)} "
                + $"with {_packages.Length} packages.");
        }
    }
}