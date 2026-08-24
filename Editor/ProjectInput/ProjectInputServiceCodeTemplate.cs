using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.ProjectInput
{
    /// <summary>
    /// Loads the <c>ProjectInputService.cs</c> template shipped next to this file as a text asset.
    /// <para>
    /// The template is a verbatim copy of the working service for the default folder layout, so it
    /// can be replaced by dropping the real file in without any editing. Only when the layout moves
    /// do the namespace and actions class name in it get substituted. It is located by asset GUID so
    /// renaming or moving it does not break the lookup.
    /// </para>
    /// </summary>
    internal static class ProjectInputServiceCodeTemplate
    {
        private const string DefaultActionsClassName = "PlayerInputActions";
        private const string DefaultServiceNamespace = "Generated.Input";
        private const string TemplateGuid = "4c9b1f7a2e6d4b3c8a5f0d1e7b26c934";

        /// <summary>
        /// Loads the template source with the namespace and actions class name applied.
        /// </summary>
        /// <param name="serviceNamespace">The namespace the written service lives in.</param>
        /// <param name="actionsClassName">The name of the generated actions class the service owns.</param>
        /// <param name="code">The template source, or null if the asset could not be loaded.</param>
        /// <returns>True if the template was found; otherwise false.</returns>
        internal static bool TryLoad(string serviceNamespace, string actionsClassName, out string code)
        {
            code = null;

            string path = AssetDatabase.GUIDToAssetPath(TemplateGuid);
            TextAsset template = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            if (template == null)
            {
                Debug.LogError($"{nameof(ProjectInputServiceCodeTemplate)}: template {TemplateGuid} was not found.");
                return false;
            }

            code = template.text
                .Replace(DefaultServiceNamespace, serviceNamespace)
                .Replace(DefaultActionsClassName, actionsClassName);

            return true;
        }
    }
}