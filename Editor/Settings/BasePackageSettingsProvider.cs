using System.Collections.Generic;
using System.Text;
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
        private const char LineBreak = '\n';
        private const string PageLabel = "Git Packages";
        private const string SettingsPath = "Project/Custom Tools/Git Packages";

        /// <summary>
        /// The settings path used to open this page programmatically.
        /// </summary>
        internal static string Path => SettingsPath;

        private static SerializedObject _serializedObject;
        private static SerializedProperty _packagesProperty;
        private static string _problems;

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

                Revalidate();
            },
            deactivateHandler = () =>
            {
                _serializedObject?.Dispose();
                _serializedObject = null;
                _packagesProperty = null;
                _problems = null;
            },
            guiHandler = _ => DrawGui()
        };

        // Cached rather than rebuilt every repaint: the dependency graph only changes when the
        // list below is edited, and the walk allocates a string per problem.
        private static void Revalidate()
        {
            string[] found = PackageRegistryValidator.Validate(BasePackageRegistry.instance.SortedPackages);

            if (found.Length == 0)
            {
                _problems = null;
                return;
            }

            StringBuilder builder = new();

            for (int i = 0; i < found.Length; i++)
            {
                if (i > 0)
                    builder.Append(LineBreak);

                builder.Append(found[i]);
            }

            _problems = builder.ToString();
        }

        private static void DrawGui()
        {
            if (_serializedObject == null)
                return;

            _serializedObject.Update();

            EditorGUILayout.HelpBox($"Packages available in the {GitPackageManager.WindowTitle} window. "
                + "Name is the label shown; URL is the Git dependency to add; Depends On lists the names of the "
                + "packages that have to be installed with it.",
                MessageType.Info);

            if (!string.IsNullOrEmpty(_problems))
            {
                EditorGUILayout.Space(InstallerTheme.Metrics.SettingsPageSpacing);
                EditorGUILayout.HelpBox(_problems, MessageType.Warning);
            }

            EditorGUILayout.Space(InstallerTheme.Metrics.SettingsPageSpacing);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_packagesProperty, true);

            if (!EditorGUI.EndChangeCheck())
                return;

            _serializedObject.ApplyModifiedProperties();
            BasePackageRegistry.instance.Persist();

            Revalidate();
        }
    }
}