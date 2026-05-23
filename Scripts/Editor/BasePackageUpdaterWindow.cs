#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Editor
{
    /// <summary>
    /// Editor window for updating installed base packages.
    /// </summary>
    public class BasePackageUpdaterWindow : EditorWindow
    {
        private PackageUpdater _updater;

        private string _status;

        [MenuItem("Tools/Base Package Installer/Updater")]
        public static void ShowWindow() => GetWindow<BasePackageUpdaterWindow>("Base Package Updater");

        private void OnEnable()
        {
            _updater ??= new PackageUpdater();

            _updater.OnPackageStarted += HandlePackageStarted;
            _updater.OnPackageCompleted += HandlePackageUpdated;
            _updater.OnPackageFailed += HandlePackageFailed;
            _updater.OnAllPackagesCompleted += HandleAllPackagesUpdated;
        }

        private void OnDisable()
        {
            _updater.OnPackageStarted -= HandlePackageStarted;
            _updater.OnPackageCompleted -= HandlePackageUpdated;
            _updater.OnPackageFailed -= HandlePackageFailed;
            _updater.OnAllPackagesCompleted -= HandleAllPackagesUpdated;
        }

        private void OnGUI()
        {
            DrawNavigation();

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Base Package Updater", EditorStyles.boldLabel);

            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Re-imports all registered Git packages to fetch the latest remote versions.",
                MessageType.Info);

            EditorGUILayout.Space(8);

            EditorGUI.BeginDisabledGroup(_updater.IsRunning);

            if (GUILayout.Button("Update All Packages", GUILayout.Height(30)))
                StartUpdate();

            EditorGUI.EndDisabledGroup();

            if (string.IsNullOrEmpty(_status))
                return;

            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(_status, _updater.IsRunning
                ? MessageType.Info
                : MessageType.None);
        }

        private static void DrawNavigation()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Installer", GUILayout.Width(140)))
                BasePackageInstallerWindow.ShowWindow();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        private void StartUpdate()
        {
            _updater.Run(BasePackageRegistry.Packages.Select(x => x.Url));
        }

        private void HandlePackageStarted(string url)
        {
            _status = $"Updating: {url.Split('/').Last()}...";
            Repaint();
        }

        private static void HandlePackageUpdated(string packageName)
        {
            Debug.Log($"{nameof(BasePackageUpdaterWindow)}: Updated {packageName} successfully.", null);
        }

        private void HandlePackageFailed(string error)
        {
            _status = $"Failed: {error}";

            Debug.LogError($"{nameof(BasePackageUpdaterWindow)}: {_status}", null);

            Repaint();
        }

        private void HandleAllPackagesUpdated()
        {
            _status = "All packages updated successfully.";
            Repaint();
        }
    }
}
#endif