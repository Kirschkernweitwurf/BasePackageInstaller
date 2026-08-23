using System;
using System.Collections.Generic;
using System.Text;
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
    /// Editor window for managing base packages. In install mode it adds the selected packages as
    /// Git dependencies, installing any that are missing and updating any that are already present
    /// to the latest remote version in a single action. In uninstall mode it removes them again.
    /// Each package's current install status and version are shown in a table.
    /// <para>
    /// Both modes read the same dependency graph, in opposite directions. Selecting a package to
    /// install also selects what it needs and installs that first; selecting one to remove also
    /// selects what needs it and removes that first. Either way the project compiles at every step.
    /// </para>
    /// </summary>
    internal sealed class GitPackageManager : EditorWindow
    {
        private const string BrokenFormat = "{0} would be left installed while depending on packages that are "
            + "being removed, so the project will not compile until they are removed too.";
        private const string CancelLabel = "Cancel";
        private const string ClearLabel = "Clear";
        private const string ConfirmIntro = "These packages will be removed from the project:";
        private const string ConfirmRemoveLabel = "Remove";
        private const string ConfirmTitle = "Uninstall Packages";
        private const double CopiedFadeSeconds = 1d;
        private const string CopiedNotification = "Copied Results";
        private const string CopyLabel = "Copy";
        private const string CreateInputServiceLabel = "Create ProjectInputService";
        private const string DependenciesLabel = "Resolve Dependencies";
        private const string DependenciesPrefsKey = "Base.PackageInstaller.ResolveDependencies";
        private const string DeselectAllLabel = "Deselect All";
        private const string EditListLabel = "Edit List";
        private const string InstallDescription = "Installs the selected git packages or updates them to the latest "
            + "remote version if they are already installed. Packages a selection depends on are added to it "
            + "automatically and are installed first.";
        private const string InstallLabel = "Install Selected";
        private const string InstallOrUpdateLabel = "Install / Update Selected";
        private const string InstallTooltip = "Off: only the packages ticked by hand are processed, so a "
            + "single package can be updated without its chain. The Required By column still reports what "
            + "depends on what.";
        private const string NothingSelectedLabel = "Nothing Selected";
        private const string PackagesHeader = "Git Packages";
        private const string Paragraph = "\n\n";
        private const string ProgressVerb = "Processing";
        private const string ProjectSetupHeader = "Project Setup";
        private const string RefreshLabel = "Refresh";
        private const string ResultHeader = "Result";
        private const string SelectAllLabel = "Select All";
        private const string SeparatorText = ", ";
        private const string UninstallDescription = "Removes the selected git packages from the project. Packages "
            + "that depend on a selection are removed with it and are removed first, so nothing is ever left "
            + "behind referencing something that is already gone.";
        private const string UninstallLabel = "Uninstall Selected";
        private const string UninstallTooltip = "Off: only the packages ticked by hand are removed. The Depends On "
            + "column still reports what would have come along, and anything left behind depending on the "
            + "selection is called out below.";
        private const string UpdateLabel = "Update Selected";

#if BASE_PACKAGES_DEV
        private const bool IsBasePackageDev = true;
#else
        private const bool IsBasePackageDev = false;
#endif

        // The Menu Manager lives in the Tools package, which this window is meant to install in the
        // first place, so this is the one place a plain MenuItem is used instead of DynamicMenuItem.
        private const string MenuPath = "Tools/" + WindowTitle;
        private const int MenuPriority = -15;

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
        private static readonly GUILayoutOption CopyWidth = GUILayout.Width(InstallerTheme.Metrics.CopyButtonWidth);
        private static readonly GUILayoutOption ExpandWidth = GUILayout.ExpandWidth(true);
        private static readonly GUIContent CopiedContent = new(CopiedNotification);
        private static readonly GUIContent InstallDependenciesContent = new(DependenciesLabel, InstallTooltip);
        private static readonly GUIContent UninstallDependenciesContent = new(DependenciesLabel, UninstallTooltip);

        private static readonly string[] ModeLabels =
        {
            "Install / Update",
            "Uninstall"
        };

        private readonly InstallerStyles _styles = new();

        private string _status;
        private bool _hasFailures;
        private bool _resolveDependencies = true;
        private EPackageMode _mode = EPackageMode.Install;
        private string _brokenWarning;
        private Vector2 _scroll;

        private PackageEntry[] _packages;
        private string[] _normalizedUrls;
        private bool[] _selected;
        private bool[] _userSelected;
        private bool[] _appliedSelection;
        private string[] _heldBy;
        private PackageStatus[] _rowStatuses;

        private IReadOnlyDictionary<string, PackageStatus> _statuses = new Dictionary<string, PackageStatus>();
        private bool _statusChecked;

        private PackageOperation _addOperation;
        private PackageOperation _removeOperation;
        private PackageStatusChecker _checker;
        private PackageTableView _table;

        /// <summary>True while either run is in flight, which is what locks the window down.</summary>
        private bool IsBusy => _addOperation.IsRunning || _removeOperation.IsRunning;

#region Unity Callbacks
        private void OnEnable()
        {
            _resolveDependencies = EditorPrefs.GetBool(DependenciesPrefsKey, true);

            RefreshPackages();

            _addOperation ??= new GitPackageOperation();
            _removeOperation ??= new RemovePackageOperation();
            _checker ??= new PackageStatusChecker();
            _table ??= new PackageTableView(_styles);

            Subscribe(_addOperation);
            Subscribe(_removeOperation);
            _checker.OnCompleted += HandleStatusesReady;

            // A package install or removal can trigger a domain reload that re-creates this window
            // and its operations. Both are resumed here, because the window always opens in install
            // mode and an interrupted removal would otherwise be left half done.
            _addOperation.Resume();
            _removeOperation.Resume();

            RefreshStatuses();
        }

        private void OnGUI()
        {
            _styles.EnsureBuilt();

            EditorGUILayout.BeginVertical(_styles.WindowBody);

            DrawHeader();
            EditorGUILayout.Space(InstallerTheme.Metrics.SectionSpacing);

            DrawPackagesSection();

            DrawProjectSetupSection();
            DrawStatusFooter();

            EditorGUILayout.EndVertical();
        }

        private void OnFocus() => RefreshStatuses();

        private void OnDisable()
        {
            Unsubscribe(_addOperation);
            Unsubscribe(_removeOperation);
            _checker.OnCompleted -= HandleStatusesReady;

            _styles.Dispose();
        }
#endregion

        [MenuItem(MenuPath, priority = MenuPriority)]
        private static void ShowWindow() => GetWindow<GitPackageManager>(WindowTitle);

        private static void HandlePackageCompleted(PackageResult result)
            => Debug.Log($"{WindowTitle}: {OperationSummaryFormatter.Describe(result)}");

        private static string GetActionLabel(EInstallAction action) => action switch
        {
            EInstallAction.Install => InstallLabel,
            EInstallAction.Nothing => NothingSelectedLabel,
            EInstallAction.Uninstall => UninstallLabel,
            EInstallAction.Update => UpdateLabel,
            _ => InstallOrUpdateLabel
        };

        private static string Join(IReadOnlyList<string> names)
        {
            StringBuilder builder = new();

            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0)
                    builder.Append(SeparatorText);

                builder.Append(names[i]);
            }

            return builder.ToString();
        }

        private void Subscribe(PackageOperation operation)
        {
            operation.OnPackageStarted += HandlePackageStarted;
            operation.OnPackageCompleted += HandlePackageCompleted;
            operation.OnPackageFailed += HandlePackageFailed;
            operation.OnAllPackagesCompleted += HandleAllPackagesCompleted;
        }

        private void Unsubscribe(PackageOperation operation)
        {
            operation.OnPackageStarted -= HandlePackageStarted;
            operation.OnPackageCompleted -= HandlePackageCompleted;
            operation.OnPackageFailed -= HandlePackageFailed;
            operation.OnAllPackagesCompleted -= HandleAllPackagesCompleted;
        }

        private void DrawHeader()
        {
            GUILayout.Label(WindowTitle, _styles.Title);
            EditorGUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            DrawModeToolbar();
            EditorGUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            GUILayout.Label(_mode == EPackageMode.Install
                ? InstallDescription
                : UninstallDescription, _styles.Description);
        }

        // Switching direction mid-run would leave the status line describing one thing while the
        // buttons offer another, so the choice is frozen while anything is in flight.
        private void DrawModeToolbar()
        {
            EditorGUI.BeginDisabledGroup(IsBusy);

            const float padding = InstallerTheme.Metrics.SegmentTrackPadding;
            Rect track = GUILayoutUtility.GetRect(0f, InstallerTheme.Metrics.SegmentHeight + padding * 2f,
                ExpandWidth);

            if (Event.current.type == EventType.Repaint)
                _styles.SegmentTrack.Draw(track, false, false, false, false);

            // Laid out by rectangle rather than by layout group. Left to itself the layout would
            // give each segment its own text width plus a share of what is left, so the longer
            // label would end up with the wider half and the split would sit off center.
            Rect inner = new(track.x + padding, track.y + padding, track.width - padding * 2f,
                track.height - padding * 2f);

            float width = inner.width / ModeLabels.Length;

            for (int i = 0; i < ModeLabels.Length; i++)
                DrawModeSegment(i, new Rect(inner.x + width * i, inner.y, width, inner.height));

            EditorGUI.EndDisabledGroup();
        }

        private void DrawModeSegment(int index, Rect area)
        {
            bool isSelected = (int)_mode == index;

            GUIStyle style = isSelected
                ? _styles.SegmentSelected
                : _styles.Segment;

            // The active segment is already where it wants to be, so clicking it does nothing
            // rather than resetting the selection out from under the user.
            if (GUI.Button(area, ModeLabels[index], style) && !isSelected)
                SetMode((EPackageMode)index);
        }

        private void SetMode(EPackageMode mode)
        {
            if (_mode == mode)
                return;

            _mode = mode;

            // Install opens with everything ticked and uninstall with nothing. Carrying a full
            // selection across would put the whole stack one click away from being removed.
            ResetSelection();
            ForceApplyDependencies();
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

            _table.Draw(_packages, _selected, _userSelected, _heldBy, _resolveDependencies, _rowStatuses,
                _statusChecked, _mode, ref _scroll);

            EditorGUILayout.Space(InstallerTheme.Metrics.ItemSpacing);
            DrawSelectionButtons();

            DrawBrokenWarning();

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
            GUIContent content = _mode == EPackageMode.Install
                ? InstallDependenciesContent
                : UninstallDependenciesContent;

            EditorGUI.BeginChangeCheck();

            bool resolve = GUILayout.Toggle(_resolveDependencies, content);

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

        // Only ever filled while uninstalling, and only when dependency resolving is off: with it
        // on, everything that would break is pulled into the removal instead.
        private void DrawBrokenWarning()
        {
            if (string.IsNullOrEmpty(_brokenWarning))
                return;

            EditorGUILayout.Space(InstallerTheme.Metrics.ItemSpacing);
            EditorGUILayout.HelpBox(_brokenWarning, MessageType.Warning);
        }

        private void DrawActionButton()
        {
            EInstallAction action = ResolveAction();

            EditorGUI.BeginDisabledGroup(IsBusy
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

            if (GUILayout.Button(CopyLabel, _styles.SecondaryButton, CopyWidth, ToolbarHeight))
                CopyStatus();

            GUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            if (GUILayout.Button(ClearLabel, _styles.SecondaryButton, ClearWidth, ToolbarHeight))
                ClearStatus();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(InstallerTheme.Metrics.TightSpacing);

            EditorGUILayout.HelpBox(_status, GetStatusMessageType());
        }

        private MessageType GetStatusMessageType()
        {
            if (IsBusy)
                return MessageType.Info;

            return _hasFailures
                ? MessageType.Warning
                : MessageType.None;
        }

        private EInstallAction ResolveAction() => _mode == EPackageMode.Install
            ? ResolveInstallAction()
            : ResolveUninstallAction();

        private EInstallAction ResolveInstallAction()
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

        private EInstallAction ResolveUninstallAction()
        {
            for (int i = 0; i < _packages.Length; i++)
            {
                if (!_selected[i])
                    continue;

                // Until the statuses are in nothing reads as installed, so a selection is taken at
                // face value rather than the button claiming there is nothing to do.
                if (!_statusChecked || _rowStatuses[i].IsInstalled)
                    return EInstallAction.Uninstall;
            }

            return EInstallAction.Nothing;
        }

        // The clipboard gives no sign of having been written to, so the window says so itself.
        private void CopyStatus()
        {
            EditorGUIUtility.systemCopyBuffer = _status;

            ShowNotification(CopiedContent, CopiedFadeSeconds);
        }

        private void ClearStatus()
        {
            _status = null;
            _hasFailures = false;

            Repaint();
        }

        // Install starts from everything and uninstall from nothing, which is also the safe
        // default: a fresh uninstall selection can never fire without a deliberate tick.
        private void ResetSelection() => SetAllSelected(_mode == EPackageMode.Install);

        private void SetAllSelected(bool value)
        {
            for (int i = 0; i < _userSelected.Length; i++)
                _userSelected[i] = value;
        }

        // The effective selection is derived from what the user picked rather than edited in
        // place, so releasing a package also releases whatever only it held. Recomputed only
        // when the user's picks actually moved, because OnGUI runs on every repaint and the walk
        // joins a string per held row.
        private void ApplyDependencies()
        {
            if (!HasSelectionChanged())
                return;

            ForceApplyDependencies();
        }

        // The cached copy only tracks the user's picks, so a change to the toggle or the mode has
        // to bypass it.
        private void ForceApplyDependencies()
        {
            PackageDependencyResolver.Resolve(_packages, _userSelected, _selected, _heldBy, _mode,
                _resolveDependencies);

            Array.Copy(_userSelected, _appliedSelection, _userSelected.Length);

            RefreshBrokenWarning();
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

        // What is actually going to be removed: a selected row that is not installed, has nothing
        // to take away, so it is neither run nor counted as breaking anything.
        private bool[] BuildRemovalSet()
        {
            bool[] removing = new bool[_packages.Length];

            for (int i = 0; i < _packages.Length; i++)
                removing[i] = _selected[i] && _rowStatuses[i].IsInstalled;

            return removing;
        }

        private void RefreshBrokenWarning()
        {
            _brokenWarning = null;

            if (_mode != EPackageMode.Uninstall)
                return;

            List<string> broken = PackageDependencyResolver.FindBroken(_packages, BuildRemovalSet(), _rowStatuses);

            if (broken.Count == 0)
                return;

            _brokenWarning = string.Format(BrokenFormat, Join(broken));
        }

        private void StartOperation()
        {
            ForceApplyDependencies();

            if (_mode == EPackageMode.Uninstall)
            {
                StartRemoval();
                return;
            }

            List<PackageRequest> requests = new();

            foreach (int index in PackageDependencyResolver.ResolveOrder(_packages, _selected, _mode))
                requests.Add(new PackageRequest(_packages[index].Name, _packages[index].Url));

            BeginRun(_addOperation, requests);
        }

        private void StartRemoval()
        {
            List<PackageRequest> requests = new();

            foreach (int index in PackageDependencyResolver.ResolveOrder(_packages, _selected, _mode))
            {
                if (!_rowStatuses[index].IsInstalled)
                    continue;

                requests.Add(new PackageRequest(_packages[index].Name, _rowStatuses[index].Name));
            }

            if (requests.Count == 0)
                return;

            if (!ConfirmRemoval(requests))
                return;

            BeginRun(_removeOperation, requests);
        }

        // Removing is the one action here that cannot be undone by pressing the button again, so
        // it is the one that asks first, with whatever it would leave behind broken spelled out.
        private bool ConfirmRemoval(IReadOnlyList<PackageRequest> requests)
        {
            List<string> labels = new();

            foreach (PackageRequest request in requests)
                labels.Add(request.Label);

            StringBuilder builder = new();

            builder.Append(ConfirmIntro);
            builder.Append(Paragraph);
            builder.Append(Join(labels));

            if (!string.IsNullOrEmpty(_brokenWarning))
            {
                builder.Append(Paragraph);
                builder.Append(_brokenWarning);
            }

            return EditorUtility.DisplayDialog(ConfirmTitle, builder.ToString(), ConfirmRemoveLabel, CancelLabel);
        }

        private void BeginRun(PackageOperation operation, IReadOnlyList<PackageRequest> requests)
        {
            _status = null;
            _hasFailures = false;

            operation.Run(requests);
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
            // derives the selection, rather than starting out with a stale heldBy.
            _appliedSelection = new bool[_packages.Length];
            _heldBy = new string[_packages.Length];
            _rowStatuses = new PackageStatus[_packages.Length];

            for (int i = 0; i < _packages.Length; i++)
                _normalizedUrls[i] = PackageStatusChecker.Normalize(_packages[i].Url);

            ResetSelection();
            FillRowStatuses();
        }

        private void RefreshStatuses()
        {
            if (_checker == null || _checker.IsRunning)
                return;

            if (IsBusy)
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

            // The warning reads what is installed, so it only becomes meaningful now.
            RefreshBrokenWarning();

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