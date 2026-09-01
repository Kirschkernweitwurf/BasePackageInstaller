using System;
using System.IO;
using UnityEditor;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Resolves the two paths the generator works with: the packages root that is read and the
    /// generated file that is written.
    /// <para>
    /// Both are remembered per machine and both the window and the automatic run read them from
    /// here, so a path picked by hand in the window is the one the next project open uses.
    /// </para>
    /// </summary>
    internal static class PackageDefaultsPaths
    {
        private const string DefaultRoot =
            @"C:\Users\maxte\GitHub\Unity\Base\BaseProjectPackages\BaseProject\Packages";

        private const string OutputPrefsKey = "Scripts.PackageDefaults.OutputPath";
        private const string PackageCacheFolder = "Library/PackageCache";
        private const char PathSeparator = '/';
        private const string RootPrefsKey = "Scripts.PackageDefaults.PackagesRoot";
        private const string TargetAssetFilter = "BasePackageDefaults t:MonoScript";
        private const string TargetAssetSuffix = "/BasePackageDefaults.cs";
        private const char WindowsSeparator = '\\';

        /// <summary>
        /// Reads the remembered packages root.
        /// </summary>
        /// <returns>The absolute path of the folder holding the package folders.</returns>
        internal static string LoadRoot() => EditorPrefs.GetString(RootPrefsKey, DefaultRoot);

        /// <summary>
        /// Remembers the packages root for this machine.
        /// </summary>
        /// <param name="root">The absolute path of the folder holding the package folders.</param>
        internal static void SaveRoot(string root) => EditorPrefs.SetString(RootPrefsKey, root);

        /// <summary>
        /// Reads the remembered target file.
        /// </summary>
        /// <returns>The absolute path of the generated file, or an empty string when none was picked.</returns>
        internal static string LoadTarget() => EditorPrefs.GetString(OutputPrefsKey, string.Empty);

        /// <summary>
        /// Remembers the target file for this machine.
        /// </summary>
        /// <param name="target">The absolute path of the generated file.</param>
        internal static void SaveTarget(string target) => EditorPrefs.SetString(OutputPrefsKey, target);

        /// <summary>
        /// Looks the generated file up in the project so the target does not have to be picked by
        /// hand on a fresh machine.
        /// </summary>
        /// <remarks>
        /// A copy inside the package cache is ignored on purpose. That folder is rebuilt whenever a
        /// Git package is resolved, so writing there looks like it worked and is gone next import.
        /// Only a checked out or embedded copy is worth offering.
        /// </remarks>
        /// <returns>The absolute path of the file, or an empty string when it was not found.</returns>
        internal static string LocateTarget()
        {
            foreach (string guid in AssetDatabase.FindAssets(TargetAssetFilter))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!assetPath.EndsWith(TargetAssetSuffix, StringComparison.Ordinal))
                    continue;

                string fullPath = Path.GetFullPath(assetPath);

                if (IsInPackageCache(fullPath))
                    continue;

                return fullPath;
            }

            return string.Empty;
        }

        /// <summary>
        /// Tells whether a path points into the resolved package cache, which is rebuilt on every
        /// import and must therefore never be written to.
        /// </summary>
        /// <param name="fullPath">The absolute path to test.</param>
        /// <returns>True when the path lies inside the package cache; otherwise false.</returns>
        internal static bool IsInPackageCache(string fullPath)
            => fullPath.Replace(WindowsSeparator, PathSeparator).Contains(PackageCacheFolder);
    }
}