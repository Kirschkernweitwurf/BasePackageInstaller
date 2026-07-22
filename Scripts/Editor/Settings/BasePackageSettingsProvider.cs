#if UNITY_EDITOR
using System.Collections.Generic;
using Base.PackageInstaller.Data;
using UnityEditor;

namespace Base.PackageInstaller.Settings
{
    /// <summary>
    /// Exposes the <see cref="BasePackageRegistry"/> under Project Settings → "Base Packages"
    /// so packages can be added, removed or edited per project.
    /// </summary>
    internal static class BasePackageSettingsProvider
    {
        private const string PackagesProperty = "packages";
        private const string SettingsPath = "Project/Custom Tools/Git Packages";

        /// <summary>
        /// The settings path used to open this page programmatically.
        /// </summary>
        public static string Path => SettingsPath;

        private static SerializedObject _serializedObject;
        private static SerializedProperty _packagesProperty;

        [SettingsProvider]
        private static SettingsProvider Create() => new(SettingsPath, SettingsScope.Project)
        {
            label = "Git Packages",
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
                _packagesProperty = _serializedObject.FindProperty(PackagesProperty);
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

            EditorGUILayout.HelpBox("Packages available in the Base Package Manager window. "
                + "Name is the label shown; URL is the Git dependency to add.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_packagesProperty, true);

            if (!EditorGUI.EndChangeCheck())
                return;

            _serializedObject.ApplyModifiedProperties();
            BasePackageRegistry.instance.Persist();
        }
    }
}
#endif