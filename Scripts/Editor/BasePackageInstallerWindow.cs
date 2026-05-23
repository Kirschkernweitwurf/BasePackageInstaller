#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Editor
{
    /// <summary>
    /// Editor window for installing base packages.
    /// </summary>
    public class BasePackageInstallerWindow : EditorWindow
    {
        private readonly bool[] _selected = Enumerable.Repeat(true, BasePackageRegistry.Packages.Length).ToArray();

        private string _status;
        private Vector2 _scroll;
        private PackageInstaller _installer;

        [MenuItem("Tools/Base Package Installer/Installer")]
        public static void ShowWindow() => GetWindow<BasePackageInstallerWindow>("Base Package Installer");

        private void OnEnable()
        {
            _installer ??= new PackageInstaller();

            _installer.OnPackageStarted += HandlePackageStarted;
            _installer.OnPackageCompleted += HandlePackageInstalled;
            _installer.OnPackageFailed += HandlePackageFailed;
            _installer.OnAllPackagesCompleted += HandleAllPackagesInstalled;
        }

        private void OnDisable()
        {
            _installer.OnPackageStarted -= HandlePackageStarted;
            _installer.OnPackageCompleted -= HandlePackageInstalled;
            _installer.OnPackageFailed -= HandlePackageFailed;
            _installer.OnAllPackagesCompleted -= HandleAllPackagesInstalled;
        }

        private void OnGUI()
        {
            DrawNavigation();

            DrawPackagesSection();

            EditorGUILayout.Space(12);

            DrawProjectSetupSection();

            if (string.IsNullOrEmpty(_status))
                return;

            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(_status, _installer.IsRunning
                ? MessageType.Info
                : MessageType.None);
        }

        private static void DrawNavigation()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Updater", GUILayout.Width(140)))
                BasePackageUpdaterWindow.ShowWindow();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        private void DrawPackagesSection()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Base Packages", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < BasePackageRegistry.Packages.Length; i++)
                _selected[i] = EditorGUILayout.ToggleLeft(BasePackageRegistry.Packages[i].Name, _selected[i]);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All"))
                SetAllSelected(true);

            if (GUILayout.Button("Deselect All"))
                SetAllSelected(false);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(_installer.IsRunning);

            if (GUILayout.Button("Install Selected", GUILayout.Height(30)))
                StartInstall();

            EditorGUI.EndDisabledGroup();
        }

        private static void DrawProjectSetupSection()
        {
            EditorGUILayout.LabelField("Project Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            bool alreadySetUp = ProjectInputServiceSetup.IsSetUp;

            string label = alreadySetUp
                ? "ProjectInputService — already set up"
                : "Create ProjectInputService";

            EditorGUI.BeginDisabledGroup(alreadySetUp);

            if (GUILayout.Button(label, GUILayout.Height(30)))
                ProjectInputServiceSetup.Run();

            EditorGUI.EndDisabledGroup();
        }

        private void SetAllSelected(bool value)
        {
            for (int i = 0; i < _selected.Length; i++)
                _selected[i] = value;
        }

        private void StartInstall()
        {
            List<string> urls = new();

            for (int i = 0; i < BasePackageRegistry.Packages.Length; i++)
                if (_selected[i])
                    urls.Add(BasePackageRegistry.Packages[i].Url);

            _installer.Run(urls);
        }

        private void HandlePackageStarted(string url)
        {
            _status = $"Installing: {url.Split('/').Last()}...";
            Repaint();
        }

        private static void HandlePackageInstalled(string packageName)
        {
            Debug.Log($"{nameof(BasePackageInstallerWindow)}: Installed {packageName} successfully.", null);
        }

        private void HandlePackageFailed(string error)
        {
            _status = $"Failed: {error}";

            Debug.LogError($"{nameof(BasePackageInstallerWindow)}: {_status}", null);

            Repaint();
        }

        private void HandleAllPackagesInstalled()
        {
            _status = "All packages installed successfully.";
            Repaint();
        }
    }
}
#endif