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
        private const int CancelChoice = 1;
        private const string CancelLabel = "Cancel";
        private const int CreateChoice = 2;
        private const string CreateLabel = "Create a new one";
        private const string DefaultActionMapName = "Gameplay";
        private const string DialogTitle = "Project Input Setup";
        private const string GenerateWrapperProperty = "generateWrapperCode";
        private const string LegacyPrefix = "m_";
        private const string MoveLabel = "Move it";
        private const string MovePromptFormat = "The project already contains an input action asset at\n{0}\n\n"
            + "Moving it to\n{1}\nrenames it, which also renames its generated actions class. Every script "
            + "referring to that class by name has to be updated by hand afterwards.";
        private const string MultipleAssetsFormat = "The project contains {0} input action assets and none of them "
            + "is at {1}. Move or delete all but one, then run the setup again.";
        private const string ScriptExtension = ".cs";
        private const string WrapperClassProperty = "wrapperClassName";
        private const string WrapperNamespaceProperty = "wrapperCodeNamespace";
        private const string WrapperPathProperty = "wrapperCodePath";

        private static readonly string[] SearchFolders =
        {
            "Assets"
        };

        private static readonly string AssetFilter = $"t:{nameof(InputActionAsset)}";

        /// <summary>
        /// Makes sure an input action asset exists at the given path and generates its wrapper class.
        /// A single asset found elsewhere in the project can be moved instead, after confirmation.
        /// </summary>
        /// <param name="targetPath">The project-relative path the asset should live at.</param>
        /// <param name="wrapperPath">The project-relative path the generated wrapper class is written to.</param>
        /// <param name="codeNamespace">The namespace for the generated wrapper class.</param>
        /// <returns>True if the asset is in place, configured and its wrapper was generated; otherwise false.</returns>
        internal static bool TryEnsureAssetAtPath(string targetPath, string wrapperPath, string codeNamespace)
        {
            string assetPath = FindOrCreateAsset(targetPath);

            if (string.IsNullOrEmpty(assetPath))
                return false;

            return TryConfigureImporter(assetPath, wrapperPath, codeNamespace);
        }

        private static string FindOrCreateAsset(string targetPath)
        {
            // An asset already sitting at the target path is the one to use, whatever else the project
            // happens to contain. Only when that spot is free is moving something else worth offering.
            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(targetPath) != null)
                return targetPath;

            string[] guids = AssetDatabase.FindAssets(AssetFilter, SearchFolders);

            if (guids.Length == 0)
                return CreateNewAsset(targetPath);

            // Picking one of several by index would move whichever the asset database happened to
            // return first, which is not a choice this tool gets to make on its own.
            if (guids.Length > 1)
            {
                Debug.LogError(string.Format(MultipleAssetsFormat, guids.Length, targetPath));

                return null;
            }

            return MoveOrCreate(AssetDatabase.GUIDToAssetPath(guids[0]), targetPath);
        }

        // A move renames the asset and therefore the generated actions class every script refers to
        // by name, so it is not something to do behind the user's back.
        private static string MoveOrCreate(string existingPath, string targetPath)
        {
            int choice = EditorUtility.DisplayDialogComplex(DialogTitle,
                string.Format(MovePromptFormat, existingPath, targetPath), MoveLabel, CancelLabel, CreateLabel);

            if (choice == CancelChoice)
                return null;

            if (choice == CreateChoice)
                return CreateNewAsset(targetPath);

            DeleteGeneratedWrapper(existingPath);

            string moveError = AssetDatabase.MoveAsset(existingPath, targetPath);

            if (string.IsNullOrEmpty(moveError))
                return targetPath;

            Debug.LogError($"Could not move input asset to {targetPath}: {moveError}");

            return null;
        }

        // The wrapper is regenerated at the new location, so the stale one belonging to the old asset
        // would otherwise stay behind and collide with it.
        private static void DeleteGeneratedWrapper(string assetPath)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
                return;

            using SerializedObject so = new(importer);

            SerializedProperty generateProperty = FindImporterProperty(so, GenerateWrapperProperty);
            SerializedProperty pathProperty = FindImporterProperty(so, WrapperPathProperty);

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

        private static bool TryConfigureImporter(string assetPath, string wrapperPath, string codeNamespace)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
            {
                Debug.LogError($"{nameof(InputActionAssetSetup)}: no importer found for {assetPath}.");

                return false;
            }

            using SerializedObject so = new(importer);

            SerializedProperty generateProperty = FindImporterProperty(so, GenerateWrapperProperty);

            // Skipping quietly here would leave the project without the generated actions class, and
            // the service file written afterwards would not compile, so this has to fail loudly.
            if (generateProperty == null)
            {
                Debug.LogError($"{nameof(InputActionAssetSetup)}: the input action importer has no "
                    + $"{GenerateWrapperProperty} field. This input system version is not supported.");

                return false;
            }

            generateProperty.boolValue = true;

            // The class name is stated rather than left empty, so it follows the asset file name
            // instead of the name stored inside the asset, which a move leaves untouched.
            SetString(so, WrapperClassProperty, Path.GetFileNameWithoutExtension(assetPath));
            SetString(so, WrapperNamespaceProperty, codeNamespace);
            SetString(so, WrapperPathProperty, wrapperPath);

            so.ApplyModifiedPropertiesWithoutUndo();

            // Applying only changes the importer in memory. Saving writes the meta file, and the
            // reimport that follows from it is what actually generates the wrapper class.
            importer.SaveAndReimport();

            if (File.Exists(wrapperPath))
                return true;

            Debug.LogError($"{nameof(InputActionAssetSetup)}: {assetPath} was reimported but no wrapper class "
                + $"appeared at {wrapperPath}.");

            return false;
        }

        // The input system dropped the m_ prefix from these fields at some point. Both spellings are
        // tried so neither version ends up configuring nothing at all.
        private static SerializedProperty FindImporterProperty(SerializedObject so, string propertyName)
        {
            SerializedProperty property = so.FindProperty(propertyName);

            if (property != null)
                return property;

            return so.FindProperty(LegacyPrefix + char.ToUpperInvariant(propertyName[0]) + propertyName[1..]);
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = FindImporterProperty(so, propertyName);

            if (property == null)
                return;

            property.stringValue = value;
        }
    }
}