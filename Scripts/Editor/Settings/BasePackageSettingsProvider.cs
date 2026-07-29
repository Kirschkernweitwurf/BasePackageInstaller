using System.Collections.Generic;
using Base.PackageInstaller.Data;
using Base.PackageInstaller.Window;
using Base.PackageInstaller.Window.Theme;
using UnityEditor;

namespace Base.PackageInstaller.Settings
{
    /// <summary>
    /// Exposes the <see cref="BasePackageRegistry"/> in the project settings
    /// so packages can be added, removed or edited per project.
    /// </summary>
    internal static class BasePackageSettingsProvider
    {
        private const string PageLabel = "Git Packages";
        private const string SettingsPath = "Project/Custom Tools/Git Packages";

        /// <summary>
        /// The settings path used to open this page programmatically.
        /// </summary>
        internal static string Path => SettingsPath;

        private static SerializedObject _serializedObject;
        private static SerializedProperty _packagesProperty;

        [SettingsProvider]
        private static SettingsProvider Create() => new(SettingsPath, SettingsScope.Project)
        {
            label = PageLabel,
            keywords = new HashSet<string>
            {
                "package",
                "git",
                "installer",
                "updater",
                "base"
            },

            // Created lazily so the registry singleton is not loaded and seeded on every
            // domain reload; it is only touched once this settings page is actually opened.
            activateHandler = (_, _) =>
            {
                _serializedObject = new SerializedObject(BasePackageRegistry.instance);
                _packagesProperty = _serializedObject.FindProperty(BasePackageRegistry.PackagesPropertyName);
            },
            deactivateHandler = () =>
            {
                _serializedObject?.Dispose();
                _serializedObject = null;
                _packagesProperty = null;
            },
            guiHandler = _ => DrawGui()
        };

        private static void DrawGui()
        {
            if (_serializedObject == null)
                return;

            _serializedObject.Update();

            EditorGUILayout.HelpBox($"Packages available in the {GitPackageManager.WindowTitle} window. "
                + "Name is the label shown; URL is the Git dependency to add.",
                MessageType.Info);

            EditorGUILayout.Space(InstallerTheme.Metrics.SettingsPageSpacing);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_packagesProperty, true);

            if (!EditorGUI.EndChangeCheck())
                return;

            _serializedObject.ApplyModifiedProperties();
            BasePackageRegistry.instance.Persist();
        }
    }
}