#if UNITY_EDITOR
namespace Base.PackageInstaller.ProjectInput
{
    /// <summary>
    /// Template for the generated <c>ProjectInputService.cs</c> file.
    /// </summary>
    internal static class ProjectInputServiceCodeTemplate
    {
        private const string NamespaceToken = "__NAMESPACE__";

        private const string Template = @"using System.Collections.Generic;
using Base.AttributePackage.References;
using Base.CorePackage.Services;
using Input;
using UnityEngine.InputSystem;

namespace __NAMESPACE__
{
    /// <summary>
    /// Project-specific input service. Owns the generated <see cref=""PlayerInputActions""/>
    /// instance and exposes it through <see cref=""Actions""/>.
    /// </summary>
    public class ProjectInputService : GameServiceBehaviour
    {
        /// <summary>
        /// The instance of the generated project specific input actions class.
        /// </summary>
        public PlayerInputActions Actions { get; private set; }

        private readonly Dictionary<InputActionMap, int> _enabledMapCounts = new();

#region Unity Callbacks
        protected override void Awake()
        {
            base.Awake();

            Actions = new PlayerInputActions();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Actions?.Dispose();
        }
#endregion

        /// <summary>
        /// Enables the given map. Reference counted, so it stays enabled until every caller
        /// that enabled it has disabled it again.
        /// </summary>
        public void EnableMap(InputActionMap map)
        {
            if (map == null)
                return;

            _enabledMapCounts.TryGetValue(map, out int count);
            _enabledMapCounts[map] = count + 1;

            if (count == 0)
                map.Enable();
        }

        /// <summary>
        /// Releases the given map. Only actually disables it once every caller that enabled
        /// it has released it again.
        /// </summary>
        public void DisableMap(InputActionMap map)
        {
            if (map == null || !_enabledMapCounts.TryGetValue(map, out int count))
                return;

            count--;

            if (count > 0)
            {
                _enabledMapCounts[map] = count;
                return;
            }

            _enabledMapCounts.Remove(map);
            map.Disable();
        }

        /// <summary>
        /// Resolves a map against the package's runtime actions clone, so callers enable the
        /// exact instance they subscribe to via <see cref=""BaseInputActions""/>.
        /// </summary>

        // ReSharper disable once MemberCanBePrivate.Global
        public InputActionMap ResolveBaseMap(InputActionMapReference reference)
            => Actions.asset.FindActionMap(reference.MapId);

        /// <summary>
        /// Tries to resolve a map against the package's runtime actions clone, so callers enable the
        /// exact instance they subscribe to via <see cref=""BaseInputActions""/>.
        /// </summary>
        /// <returns><c>>true</c> if the reference was valid and the map was resolved; otherwise, <c>false</c>.</returns>
        public bool TryResolveBaseMap(InputActionMapReference reference, out InputActionMap map)
        {
            map = ResolveBaseMap(reference);
            return map != null;
        }
    }
}";

        /// <summary>
        /// Renders the template, replacing tokens with the provided namespaces.
        /// </summary>
        /// <param name="serviceNamespace">The namespace for the generated service class.</param>
        /// <returns>The rendered code.</returns>
        public static string Render(string serviceNamespace) => Template
            .Replace(NamespaceToken, serviceNamespace);
    }
}
#endif