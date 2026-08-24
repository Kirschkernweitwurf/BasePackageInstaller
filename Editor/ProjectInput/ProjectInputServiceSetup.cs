using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.ProjectInput
{
    /// <summary>
    /// Sets up the project's input service: configures the input action asset, has its actions
    /// wrapper generated and writes <c>ProjectInputService.cs</c> into the project.
    /// </summary>
    internal static class ProjectInputServiceSetup
    {
        private const string AssetExtension = ".inputactions";
        private const string AssetFolder = "Assets/Input";
        private const string AssetName = "PlayerInputActions";
        private const string AssetsPrefix = "Assets/";
        private const char FolderSeparator = '/';
        private const string MissingBaseFormat = "The written service derives from {0}, which ships with the Core "
            + "package. Install Core first, then run this setup again.";
        private const char NamespaceSeparator = '.';
        private const string OkLabel = "OK";
        private const string ScriptExtension = ".cs";
        private const string ServiceBaseTypeName = "Base.CorePackage.Input.ProjectInputServiceBase";
        private const string ServiceFileName = "ProjectInputService.cs";
        private const string ServiceFolder = "Assets/Generated/Input";
        private const string SetupTitle = "Project Input Setup";

        private static readonly string AssetPath = $"{AssetFolder}/{AssetName}{AssetExtension}";
        private static readonly string ServicePath = $"{ServiceFolder}/{ServiceFileName}";

        // The wrapper is generated next to the service rather than next to the asset, so its folder
        // matches the namespace both files share and no top level Input namespace is introduced. One
        // of those would shadow UnityEngine.Input for every script in the project.
        private static readonly string WrapperPath = $"{ServiceFolder}/{AssetName}{ScriptExtension}";
        private static readonly string ServiceNamespace = FolderToNamespace(ServiceFolder);

        /// <summary>True once the action asset, its generated wrapper and the service file all exist.</summary>
        internal static bool IsSetUp => File.Exists(AssetPath)
            && File.Exists(WrapperPath)
            && File.Exists(ServicePath);

        /// <summary>
        /// Creates the input action asset, its generated actions wrapper and the project input
        /// service if they are missing. Existing files are left untouched.
        /// </summary>
        internal static void Run()
        {
            if (!HasServiceBaseType())
            {
                EditorUtility.DisplayDialog(SetupTitle,
                    string.Format(MissingBaseFormat, ShortTypeName(ServiceBaseTypeName)), OkLabel);

                return;
            }

            EnsureFolder(AssetFolder);
            EnsureFolder(ServiceFolder);

            if (!InputActionAssetSetup.TryEnsureAssetAtPath(AssetPath, WrapperPath, ServiceNamespace))
                return;

            if (!File.Exists(ServicePath)
                && !TryWriteServiceFile())
                return;

            AssetDatabase.Refresh();

            Debug.Log($"{Path.GetFileNameWithoutExtension(ServiceFileName)} setup complete.");
        }

        // The installer cannot reference the Core package, so the base class the written file derives
        // from can only be looked up by name. Writing the file without it would leave the project with
        // a compile error that blocks everything until the file is deleted by hand.
        private static bool HasServiceBaseType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(ServiceBaseTypeName, false) != null)
                    return true;
            }

            return false;
        }

        private static string ShortTypeName(string fullName)
            => fullName[(fullName.LastIndexOf(NamespaceSeparator) + 1)..];

        private static bool TryWriteServiceFile()
        {
            if (!ProjectInputServiceCodeTemplate.TryLoad(ServiceNamespace, AssetName, out string code))
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