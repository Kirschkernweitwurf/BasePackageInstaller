using System.IO;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.ProjectInput
{
    /// <summary>
    /// Sets up the project's input service: configures the input action asset
    /// and writes <c>ProjectInputService.cs</c> into the project.
    /// </summary>
    internal static class ProjectInputServiceSetup
    {
        private const string AssetExtension = ".inputactions";

        // The template declares the namespaces these two folders resolve to, so changing either
        // folder means the template has to be updated with it.
        private const string AssetFolder = "Assets/Input";
        private const string AssetName = "PlayerInputActions";
        private const string AssetsPrefix = "Assets/";
        private const char FolderSeparator = '/';
        private const char NamespaceSeparator = '.';
        private const string ServiceFileName = "ProjectInputService.cs";
        private const string ServiceFolder = "Assets/Generated/Input";

        /// <summary>True once both the input action asset and the service file exist.</summary>
        internal static bool IsSetUp => File.Exists(AssetPath) && File.Exists(ServicePath);

        private static readonly string AssetPath = $"{AssetFolder}/{AssetName}{AssetExtension}";
        private static readonly string ServicePath = $"{ServiceFolder}/{ServiceFileName}";

        /// <summary>
        /// Creates the input action asset and the project input service if they are missing.
        /// Existing files are left untouched.
        /// </summary>
        internal static void Run()
        {
            EnsureFolder(AssetFolder);
            EnsureFolder(ServiceFolder);

            string inputNamespace = FolderToNamespace(AssetFolder);

            if (!InputActionAssetSetup.TryEnsureAssetAtPath(AssetPath, inputNamespace))
                return;

            if (!File.Exists(ServicePath)
                && !TryWriteServiceFile())
                return;

            AssetDatabase.Refresh();

            Debug.Log($"{Path.GetFileNameWithoutExtension(ServiceFileName)} setup complete.");
        }

        private static bool TryWriteServiceFile()
        {
            if (!ProjectInputServiceCodeTemplate.TryLoad(out string code))
                return false;

            File.WriteAllText(ServicePath, code);
            AssetDatabase.ImportAsset(ServicePath);

            return true;
        }

        private static string FolderToNamespace(string folder)
        {
            if (folder.StartsWith(AssetsPrefix))
                folder = folder[AssetsPrefix.Length..];

            return folder.Replace(FolderSeparator, NamespaceSeparator);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split(FolderSeparator);
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}{FolderSeparator}{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }
}