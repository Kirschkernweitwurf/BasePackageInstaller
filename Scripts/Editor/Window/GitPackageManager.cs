#if UNITY_EDITOR
using System.Collections.Generic;
using Base.PackageInstaller.Data;
using Base.PackageInstaller.Operations;
using Base.PackageInstaller.ProjectInput;
using Base.PackageInstaller.Settings;
using Base.PackageInstaller.Window.Format;
using Base.PackageInstaller.Window.Theme;
using Base.PackageInstaller.Window.View;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Window
{
    /// <summary>
    /// Editor window for managing base packages. Adds the selected packages as Git
    /// dependencies, installing any that are missing and updating any that are already
    /// present to the latest remote version in a single action. Each package's current
    /// install status and version are shown in a table.
    /// </summary>
    public sealed class GitPackageManager : EditorWindow
    {
#if BASE_PACKAGES_DEV
        private const bool IsBasePackageDev = true;
#else
        private const bool IsBasePackageDev = false;
#endif

        private const string WindowTitle = "Git Package Manager";
        private const string Description = "Installs the selected git packages or updates them to the latest remote "
            + "version if they are already installed.";

        private const string PackagesHeader = "Git Packages";
        private const string ProjectSetupHeader = "Project Setup";
        private const string RefreshLabel = "Refresh";
        private const string EditListLabel = "Edit List";
        private const string SelectAllLabel = "Select All";
        private const string DeselectAllLabel = "Deselect All";
        private const string CreateInputServiceLabel = "Create ProjectInputService";
        private const string ResultHeader = "Result";
        private const string ClearLabel = "Clear";

        private const string InstallLabel = "Install Selected";
        private const string UpdateLabel = "Update Selected";
        private const string InstallOrUpdateLabel = "Install / Update Selected";
        private const string ProgressVerb = "Processing";

        private static readonly GUILayoutOption ActionHeight = GUILayout.Height(InstallerTheme.Metrics.ActionButtonHeight);
        private static readonly GUILayoutOption SecondaryHeight =
            GUILayout.Height(InstallerTheme.Metrics.SecondaryButtonHeight);
        private static readonly GUILayoutOption ToolbarHeight =
            GUILayout.Height(InstallerTheme.Metrics.ToolbarButtonHeight);
        private static readonly GUILayoutOption RefreshWidth = GUILayout.Width(InstallerTheme.Metrics.RefreshButtonWidth);
        private static readonly GUILayoutOption EditListWidth =
            GUILayout.Width(InstallerTheme.Metrics.EditListButtonWidth);
        private static readonly GUILayoutOption ClearWidth = GUILayout.Width(InstallerTheme.Metrics.ClearButtonWidth);
        private static readonly GUILayoutOption ExpandWidth = GUILayout.ExpandWidth(true);

        private readonly InstallerStyles _styles = new();

        private string _status;
        private bool _hasFailures;
        private Vector2 _scroll;

        private PackageEntry[] _packages;
        private string[] _normalizedUrls;
        private bool[] _selected;
        private PackageStatus[] _rowStatuses;

        private IReadOnlyDictionary<string, PackageStatus> _statuses = new Dictionary<string, PackageStatus>();
        private bool _statusChecked;

        private PackageOperation _operation;
        private PackageStatusChecker _checker;
        private PackageTableView _table;

#region Unity Callbacks
        private void OnEnable()
        {
            RefreshPackages();

            _operation ??= new GitPackageOperation();
            _checker ??= new PackageStatusChecker();
            _table ??= new PackageTableView(_styles);

            _operation.OnPackageStarted += HandlePackageStarted;
            _operation.OnPackageCompleted += HandlePackageCompleted;
            _operation.OnPackageFailed += HandlePackageFailed;
            _operation.OnAllPackagesCompleted += HandleAllPackagesCompleted;
            _checker.OnCompleted += HandleStatusesReady;

            // A package install can trigger a domain reload that re-creates this window and
            // its operation. Resume here so an interrupted run continues where it left off.
            _operation.Resume();

            RefreshStatuses();
        }

        private void OnGUI()
        {
            _styles.EnsureBuilt();

            DrawHeader();
            EditorGUILayout.Space(InstallerTheme.Metrics.SectionSpacing);

            DrawPackagesSection();

            DrawProjectSetupSection();
            DrawStatusFooter();
        }

        private void OnDisable()
        {
            _operation.OnPackageStarted -= HandlePackageStarted;
            _operation.OnPackageCompleted -= HandlePackageCompleted;
            _operation.OnPackageFailed -= HandlePackageFailed;
            _operation.OnAllPackagesCompleted -= HandleAllPackagesCompleted;
            _checker.OnCompleted -= HandleStatusesReady;

            _styles.Dispose();
        }

        private void OnFocus() => RefreshStatuses();
#endregion

        [MenuItem("Tools/Git Package Manager", priority = -15)]
        public static void ShowWindow() => GetWindow<GitPackageManager>(WindowTitle);

        private void DrawHeader()
        {
            GUILayout.Label(WindowTitle, _styles.Title);
            EditorGUILayout.Space(InstallerTheme.Metrics.TightSpacing);
            GUILayout.Label(Description, _styles.Description);
        }

        private void DrawPackagesSection()
        {
            DrawPackagesToolbar();
            EditorGUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            _table.Draw(_packages, _selected, _rowStatuses, _statusChecked, ref _scroll);

            EditorGUILayout.Space(InstallerTheme.Metrics.ItemSpacing);
            DrawSelectionButtons();

            EditorGUILayout.Space(InstallerTheme.Metrics.TightSpacing);
            DrawActionButton();
        }

        private void DrawPackagesToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(PackagesHeader, _styles.SectionHeader);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(RefreshLabel, _styles.SecondaryButton, RefreshWidth, ToolbarHeight))
                RefreshAll();

            GUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            if (GUILayout.Button(EditListLabel, _styles.SecondaryButton, EditListWidth, ToolbarHeight))
                SettingsService.OpenProjectSettings(BasePackageSettingsProvider.Path);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(SelectAllLabel, _styles.SecondaryButton, SecondaryHeight, ExpandWidth))
                SetAllSelected(true);

            GUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            if (GUILayout.Button(DeselectAllLabel, _styles.SecondaryButton, SecondaryHeight, ExpandWidth))
                SetAllSelected(false);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionButton()
        {
            EditorGUI.BeginDisabledGroup(_operation.IsRunning || IsBasePackageDev);

            if (GUILayout.Button(GetActionLabel(), _styles.PrimaryButton, ActionHeight))
                StartOperation();

            EditorGUI.EndDisabledGroup();
        }

        private void DrawProjectSetupSection()
        {
            if (ProjectInputServiceSetup.IsSetUp)
                return;

            EditorGUILayout.Space(InstallerTheme.Metrics.SectionSpacing);

            GUILayout.Label(ProjectSetupHeader, _styles.SectionHeader);
            EditorGUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            if (GUILayout.Button(CreateInputServiceLabel, ActionHeight))
                ProjectInputServiceSetup.Run();
        }

        private void DrawStatusFooter()
        {
            if (string.IsNullOrEmpty(_status))
                return;

            EditorGUILayout.Space(InstallerTheme.Metrics.SectionSpacing);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(ResultHeader, _styles.SectionHeader);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(ClearLabel, _styles.SecondaryButton, ClearWidth, ToolbarHeight))
                ClearStatus();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            EditorGUILayout.HelpBox(_status, GetStatusMessageType());
        }

        private void ClearStatus()
        {
            _status = null;
            _hasFailures = false;

            Repaint();
        }

        private void RefreshPackages()
        {
            _packages = BasePackageRegistry.instance.SortedPackages;
            _normalizedUrls = new string[_packages.Length];
            _selected = new bool[_packages.Length];
            _rowStatuses = new PackageStatus[_packages.Length];

            for (int i = 0; i < _packages.Length; i++)
            {
                _normalizedUrls[i] = PackageStatusChecker.Normalize(_packages[i].Url);
                _selected[i] = true;
            }

            FillRowStatuses();
        }

        // Snapshots the current statuses into a per-row array so drawing does not do a dictionary
        // lookup for every package on every repaint.
        private void FillRowStatuses()
        {
            for (int i = 0; i < _packages.Length; i++)
                _rowStatuses[i] = _statuses.GetValueOrDefault(_normalizedUrls[i]);
        }

        private void RefreshStatuses()
        {
            if (_checker == null || _checker.IsRunning)
                return;

            if (_operation is
                {
                    IsRunning: true
                })
                return;

            _checker.Refresh();
        }

        // Pulls in any new or changed BasePackageDefaults, then re-checks install statuses.
        private void RefreshAll()
        {
            if (BasePackageRegistry.instance.SyncWithDefaults())
                RefreshPackages();

            RefreshStatuses();
        }

        private string GetActionLabel()
        {
            return ResolveAction() switch
            {
                EInstallAction.Install => InstallLabel,
                EInstallAction.Update => UpdateLabel,
                _ => InstallOrUpdateLabel
            };
        }

        private EInstallAction ResolveAction()
        {
            if (!_statusChecked)
                return EInstallAction.InstallOrUpdate;

            int installed = 0;
            int notInstalled = 0;

            for (int i = 0; i < _packages.Length; i++)
            {
                if (!_selected[i])
                    continue;

                if (_rowStatuses[i].IsInstalled)
                    installed++;
                else
                    notInstalled++;
            }

            if (notInstalled == 0 && installed > 0)
                return EInstallAction.Update;

            if (installed == 0 && notInstalled > 0)
                return EInstallAction.Install;

            return EInstallAction.InstallOrUpdate;
        }

        private void SetAllSelected(bool value)
        {
            for (int i = 0; i < _selected.Length; i++)
                _selected[i] = value;
        }

        private void StartOperation()
        {
            List<string> urls = new();

            for (int i = 0; i < _packages.Length; i++)
            {
                if (_selected[i])
                    urls.Add(_packages[i].Url);
            }

            _status = null;
            _hasFailures = false;

            _operation.Run(urls);
        }

        private MessageType GetStatusMessageType()
        {
            if (_operation.IsRunning)
                return MessageType.Info;

            return _hasFailures
                ? MessageType.Warning
                : MessageType.None;
        }

        private void HandleStatusesReady(IReadOnlyDictionary<string, PackageStatus> statuses)
        {
            _statuses = statuses;
            _statusChecked = true;

            FillRowStatuses();
            Repaint();
        }

        private void HandlePackageStarted(string label)
        {
            _status = $"{ProgressVerb}: {label}...";
            Repaint();
        }

        private static void HandlePackageCompleted(PackageResult result)
            => Debug.Log($"{WindowTitle}: {OperationSummaryFormatter.Describe(result)}");

        private void HandlePackageFailed(PackageResult result)
        {
            _hasFailures = true;

            Debug.LogWarning($"{WindowTitle}: {OperationSummaryFormatter.Describe(result)}");
        }

        private void HandleAllPackagesCompleted(OperationSummary summary)
        {
            _hasFailures = summary.HasFailures;
            _status = OperationSummaryFormatter.BuildSummary(summary);

            if (summary.HasFailures)
                Debug.LogWarning($"{WindowTitle}: {_status}");
            else
                Debug.Log($"{WindowTitle}: {_status}");

            RefreshStatuses();
            Repaint();
        }
    }
}
#endif
