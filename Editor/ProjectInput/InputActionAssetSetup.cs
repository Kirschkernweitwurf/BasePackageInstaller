using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Base.PackageInstaller.ProjectInput
{
    /// <summary>
    /// Finds, creates and configures the project's <see cref="InputActionAsset"/>.
    /// </summary>
    internal static class InputActionAssetSetup
    {
        private const string DefaultActionMapName = "Gameplay";
        private const string GenerateWrapperProperty = "m_GenerateWrapperCode";
        private const string ScriptExtension = ".cs";
        private const string WrapperClassProperty = "m_WrapperClassName";
        private const string WrapperNamespaceProperty = "m_WrapperCodeNamespace";
        private const string WrapperPathProperty = "m_WrapperCodePath";

        private static readonly string[] SearchFolders =
        {
            "Assets"
        };

        private static readonly string AssetFilter = $"t:{nameof(InputActionAsset)}";

        /// <summary>
        /// Makes sure an input action asset exists at the given path and generates its wrapper class.
        /// An asset found elsewhere in the project is moved instead of a second one being created.
        /// </summary>
        /// <param name="targetPath">The project-relative path the asset should live at.</param>
        /// <param name="codeNamespace">The namespace for the generated wrapper class.</param>
        /// <returns>True if the asset is in place and configured; otherwise false.</returns>
        internal static bool TryEnsureAssetAtPath(string targetPath, string codeNamespace)
        {
            string assetPath = FindOrCreateAsset(targetPath);

            if (string.IsNullOrEmpty(assetPath))
                return false;

            ConfigureImporter(assetPath, codeNamespace);

            return true;
        }

        private static string FindOrCreateAsset(string targetPath)
        {
            string[] existing = AssetDatabase.FindAssets(AssetFilter, SearchFolders);

            if (existing.Length == 0)
                return CreateNewAsset(targetPath);

            string existingPath = AssetDatabase.GUIDToAssetPath(existing[0]);

            if (existingPath == targetPath)
                return targetPath;

            DeleteGeneratedWrapper(existingPath);

            string moveError = AssetDatabase.MoveAsset(existingPath, targetPath);

            if (string.IsNullOrEmpty(moveError))
                return targetPath;

            Debug.LogError($"Could not move input asset to {targetPath}: {moveError}");

            return null;
        }

        // The wrapper is regenerated at the new location, so the stale one next to the old asset
        // would otherwise stay behind and collide with it.
        private static void DeleteGeneratedWrapper(string assetPath)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
                return;

            SerializedObject so = new(importer);

            SerializedProperty generateProperty = so.FindProperty(GenerateWrapperProperty);
            SerializedProperty pathProperty = so.FindProperty(WrapperPathProperty);

            if (generateProperty == null || !generateProperty.boolValue)
                return;

            string wrapperPath = pathProperty != null
                ? pathProperty.stringValue
                : string.Empty;

            if (string.IsNullOrEmpty(wrapperPath))
                wrapperPath = Path.ChangeExtension(assetPath, ScriptExtension);

            if (!File.Exists(wrapperPath))
                return;

            AssetDatabase.DeleteAsset(wrapperPath);
        }

        private static string CreateNewAsset(string targetPath)
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();

            asset.name = Path.GetFileNameWithoutExtension(targetPath);
            asset.AddActionMap(DefaultActionMapName);

            File.WriteAllText(targetPath, asset.ToJson());
            Object.DestroyImmediate(asset);

            AssetDatabase.ImportAsset(targetPath);

            return targetPath;
        }

        private static void ConfigureImporter(string assetPath, string codeNamespace)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
                return;

            SerializedObject so = new(importer);

            SetBool(so, GenerateWrapperProperty, true);

            // Empty path and class name make the input system derive both from the asset itself.
            SetString(so, WrapperPathProperty, string.Empty);
            SetString(so, WrapperClassProperty, string.Empty);
            SetString(so, WrapperNamespaceProperty, codeNamespace);

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);

            if (property == null)
                return;

            property.boolValue = value;
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);

            if (property == null)
                return;

            property.stringValue = value;
        }
    }
}