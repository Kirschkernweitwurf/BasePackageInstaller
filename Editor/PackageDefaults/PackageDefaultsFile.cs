using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Reads and writes the generated defaults file.
    /// <para>
    /// The write goes through the file system rather than the asset pipeline, because the target can
    /// sit in a checked out copy outside the current project. When it does lie inside, the import is
    /// triggered explicitly so the recompile starts right away instead of waiting for the editor to
    /// regain focus.
    /// </para>
    /// </summary>
    internal static class PackageDefaultsFile
    {
        private const char PathSeparator = '/';
        private const char WindowsSeparator = '\\';

        /// <summary>
        /// Reads the current contents of the target file.
        /// </summary>
        /// <param name="path">The absolute path of the file.</param>
        /// <returns>The file contents, or null when the path is empty or nothing exists there.</returns>
        internal static string Read(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            return File.ReadAllText(path);
        }

        /// <summary>
        /// Writes the generated text and imports the result when the file lies inside this project.
        /// </summary>
        /// <param name="path">The absolute path of the file.</param>
        /// <param name="contents">The text to write.</param>
        internal static void Write(string path, string contents)
        {
            File.WriteAllText(path, contents);

            string assetPath = ToAssetPath(path);

            if (string.IsNullOrEmpty(assetPath))
                return;

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        // Unity knows a file only by its path relative to the project folder, so anything that does not
        // sit under that folder cannot be imported and is left to the file system alone.
        private static string ToAssetPath(string fullPath)
        {
            DirectoryInfo projectFolder = Directory.GetParent(Application.dataPath);

            if (projectFolder == null)
                return string.Empty;

            string root = Normalize(projectFolder.FullName) + PathSeparator;
            string normalized = Normalize(Path.GetFullPath(fullPath));

            if (!normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return normalized[root.Length..];
        }

        private static string Normalize(string path) => path.Replace(WindowsSeparator, PathSeparator);
    }
}