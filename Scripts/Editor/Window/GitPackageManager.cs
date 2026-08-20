using System;
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
    /// <para>
    /// Selecting a package also selects what it depends on, and a run processes the
    /// selection dependencies first, so the project compiles at every step.
    /// </para>
    /// </summary>
    internal sealed class GitPackageManager : EditorWindow
    {
        private const string ClearLabel = "Clear";
        private const string CreateInputServiceLabel = "Create ProjectInputService";
        private const string DependenciesLabel = "Resolve Dependencies";
        private const string DependenciesPrefsKey = "Base.PackageInstaller.ResolveDependencies";
        private const string DependenciesTooltip = "Off: only the packages ticked by hand are processed, so a "
            + "single package can be updated without its chain. The Required By column still reports what "
            + "depends on what.";
        private const string DeselectAllLabel = "Deselect All";
        private const string Description = "Installs the selected git packages or updates them to the latest remote "
            + "version if they are already installed. Packages a selection depends on are added to it "
            + "automatically and are installed first.";
        private const string EditListLabel = "Edit List";
        private const string InstallLabel = "Install Selected";
        private const string InstallOrUpdateLabel = "Install / Update Selected";
        private const string NothingSelectedLabel = "Nothing Selected";

#if BASE_PACKAGES_DEV
        private const bool IsBasePackageDev = true;
#else
        private const bool IsBasePackageDev = false;
#endif

        // The Menu Manager lives in the Tools package, which this window is meant to install in the
        // first place, so this is the one place a plain MenuItem is used instead of DynamicMenuItem.
        private const string MenuPath = "Tools/" + WindowTitle;
        private const int MenuPriority = -15;

        private const string PackagesHeader = "Git Packages";
        private const string ProgressVerb = "Processing";
        private const string ProjectSetupHeader = "Project Setup";
        private const string RefreshLabel = "Refresh";
        private const string ResultHeader = "Result";
        private const string SelectAllLabel = "Select All";
        private const string UpdateLabel = "Update Selected";

        /// <summary>The window title, also used to label this window on other pages.</summary>
        internal const string WindowTitle = "Git Package Manager";

        private static readonly GUILayoutOption ActionHeight =
            GUILayout.Height(InstallerTheme.Metrics.ActionButtonHeight);
        private static readonly GUILayoutOption SecondaryHeight =
            GUILayout.Height(InstallerTheme.Metrics.SecondaryButtonHeight);
        private static readonly GUILayoutOption ToolbarHeight =
            GUILayout.Height(InstallerTheme.Metrics.ToolbarButtonHeight);
        private static readonly GUILayoutOption RefreshWidth =
            GUILayout.Width(InstallerTheme.Metrics.RefreshButtonWidth);
        private static readonly GUILayoutOption EditListWidth =
            GUILayout.Width(InstallerTheme.Metrics.EditListButtonWidth);
        private static readonly GUILayoutOption ClearWidth = GUILayout.Width(InstallerTheme.Metrics.ClearButtonWidth);
        private static readonly GUIContent DependenciesContent = new(DependenciesLabel, DependenciesTooltip);
        private static readonly GUILayoutOption ExpandWidth = GUILayout.ExpandWidth(true);

        private readonly InstallerStyles _styles = new();

        private string _status;
        private bool _hasFailures;
        private bool _resolveDependencies = true;
        private Vector2 _scroll;

        private PackageEntry[] _packages;
        private string[] _normalizedUrls;
        private bool[] _selected;
        private bool[] _userSelected;
        private bool[] _appliedSelection;
        private string[] _requiredBy;
        private PackageStatus[] _rowStatuses;

        private IReadOnlyDictionary<string, PackageStatus> _statuses = new Dictionary<string, PackageStatus>();
        private bool _statusChecked;

        private PackageOperation _operation;
        private PackageStatusChecker _checker;
        private PackageTableView _table;

#region Unity Callbacks
        private void OnEnable()
        {
            _resolveDependencies = EditorPrefs.GetBool(DependenciesPrefsKey, true);

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

        private void OnFocus() => RefreshStatuses();

        private void OnDisable()
        {
            _operation.OnPackageStarted -= HandlePackageStarted;
            _operation.OnPackageCompleted -= HandlePackageCompleted;
            _operation.OnPackageFailed -= HandlePackageFailed;
            _operation.OnAllPackagesCompleted -= HandleAllPackagesCompleted;
            _checker.OnCompleted -= HandleStatusesReady;

            _styles.Dispose();
        }
#endregion

        [MenuItem(MenuPath, priority = MenuPriority)]
        private static void ShowWindow() => GetWindow<GitPackageManager>(WindowTitle);

        private static void HandlePackageCompleted(PackageResult result)
            => Debug.Log($"{WindowTitle}: {OperationSummaryFormatter.Describe(result)}");

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

            // Has to run before the table draws. The table echoes each unlocked row's drawn state
            // back into the user's picks, so drawing against a stale selection would overwrite
            // whatever changed since the last frame, including the Select All and Deselect All
            // buttons further down and the everything-selected default this window opens with.
            ApplyDependencies();

            _table.Draw(_packages, _selected, _userSelected, _requiredBy, _resolveDependencies, _rowStatuses,
                _statusChecked, ref _scroll);

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

            DrawDependencyToggle();

            GUILayout.Space(InstallerTheme.Metrics.ItemSpacing);

            if (GUILayout.Button(RefreshLabel, _styles.SecondaryButton, RefreshWidth, ToolbarHeight))
                RefreshAll();

            GUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            if (GUILayout.Button(EditListLabel, _styles.SecondaryButton, EditListWidth, ToolbarHeight))
                SettingsService.OpenProjectSettings(BasePackageSettingsProvider.Path);

            EditorGUILayout.EndHorizontal();
        }

        // Turning this off leaves the user's own picks untouched, so the effective selection
        // collapses back to them and switching it on again restores the full set.
        private void DrawDependencyToggle()
        {
            EditorGUI.BeginChangeCheck();

            bool resolve = GUILayout.Toggle(_resolveDependencies, DependenciesContent);

            if (!EditorGUI.EndChangeCheck())
                return;

            _resolveDependencies = resolve;
            EditorPrefs.SetBool(DependenciesPrefsKey, resolve);

            ForceApplyDependencies();
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
            EInstallAction action = ResolveAction();

            EditorGUI.BeginDisabledGroup(_operation.IsRunning
                || IsBasePackageDev
                || action == EInstallAction.Nothing);

            if (GUILayout.Button(GetActionLabel(action), _styles.PrimaryButton, ActionHeight))
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

        private MessageType GetStatusMessageType()
        {
            if (_operation.IsRunning)
                return MessageType.Info;

            return _hasFailures
                ? MessageType.Warning
                : MessageType.None;
        }

        private static string GetActionLabel(EInstallAction action) => action switch
        {
            EInstallAction.Install => InstallLabel,
            EInstallAction.Nothing => NothingSelectedLabel,
            EInstallAction.Update => UpdateLabel,
            _ => InstallOrUpdateLabel
        };

        private EInstallAction ResolveAction()
        {
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

            if (installed == 0 && notInstalled == 0)
                return EInstallAction.Nothing;

            // Which of the two verbs applies is only known once the statuses are in, so until then
            // the button offers both rather than guessing and changing its mind a moment later.
            if (!_statusChecked)
                return EInstallAction.InstallOrUpdate;

            if (notInstalled == 0)
                return EInstallAction.Update;

            if (installed == 0)
                return EInstallAction.Install;

            return EInstallAction.InstallOrUpdate;
        }

        private void ClearStatus()
        {
            _status = null;
            _hasFailures = false;

            Repaint();
        }

        private void SetAllSelected(bool value)
        {
            for (int i = 0; i < _userSelected.Length; i++)
                _userSelected[i] = value;
        }

        // The effective selection is derived from what the user picked rather than edited in
        // place, so releasing a package also releases whatever only it needed. Recomputed only
        // when the user's picks actually moved, because OnGUI runs on every repaint and the walk
        // joins a string per held row.
        private void ApplyDependencies()
        {
            if (!HasSelectionChanged())
                return;

            ForceApplyDependencies();
        }

        // The cached copy only tracks the user's picks, so a change to the toggle has to bypass it.
        private void ForceApplyDependencies()
        {
            PackageDependencyResolver.Resolve(_packages, _userSelected, _selected, _requiredBy,
                _resolveDependencies);

            Array.Copy(_userSelected, _appliedSelection, _userSelected.Length);
        }

        private bool HasSelectionChanged()
        {
            for (int i = 0; i < _userSelected.Length; i++)
            {
                if (_userSelected[i] != _appliedSelection[i])
                    return true;
            }

            return false;
        }

        private void StartOperation()
        {
            ForceApplyDependencies();

            List<string> urls = PackageDependencyResolver.ResolveOrder(_packages, _selected);

            _status = null;
            _hasFailures = false;

            _operation.Run(urls);
        }

        // Pulls in any new or changed BasePackageDefaults, then re-checks install statuses.
        private void RefreshAll()
        {
            if (BasePackageRegistry.instance.SyncWithDefaults())
                RefreshPackages();

            RefreshStatuses();
        }

        private void RefreshPackages()
        {
            _packages = BasePackageRegistry.instance.SortedPackages;
            _normalizedUrls = new string[_packages.Length];
            _selected = new bool[_packages.Length];
            _userSelected = new bool[_packages.Length];

            // Deliberately left all false so the first ApplyDependencies call sees a change and
            // derives the selection, rather than starting out with a stale requiredBy.
            _appliedSelection = new bool[_packages.Length];
            _requiredBy = new string[_packages.Length];
            _rowStatuses = new PackageStatus[_packages.Length];

            for (int i = 0; i < _packages.Length; i++)
            {
                _normalizedUrls[i] = PackageStatusChecker.Normalize(_packages[i].Url);
                _userSelected[i] = true;
            }

            FillRowStatuses();
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

        // Snapshots the current statuses into a per-row array so drawing does not do a dictionary
        // lookup for every package on every repaint.
        private void FillRowStatuses()
        {
            for (int i = 0; i < _packages.Length; i++)
                _rowStatuses[i] = _statuses.GetValueOrDefault(_normalizedUrls[i]);
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