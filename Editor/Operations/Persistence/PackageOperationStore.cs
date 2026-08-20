using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Operations.Persistence
{
    /// <summary>
    /// Persists <see cref="PackageOperationState"/> in <see cref="SessionState"/>.
    /// <para>
    /// <see cref="SessionState"/> survives editor domain reloads (recompiles) but is
    /// cleared when the editor is closed, which is exactly the lifetime a single
    /// install or update run needs.
    /// </para>
    /// </summary>
    internal static class PackageOperationStore
    {
        private const string KeyPrefix = "Base.PackageInstaller.Operation.";

        /// <summary>
        /// Saves the given state under the supplied key, overwriting any previous value.
        /// </summary>
        /// <param name="key">A key that uniquely identifies the operation (e.g. its type name).</param>
        /// <param name="state">The state to persist.</param>
        internal static void Save(string key, PackageOperationState state)
            => SessionState.SetString(Resolve(key), JsonUtility.ToJson(state));

        /// <summary>
        /// Tries to load a previously saved state for the given key.
        /// </summary>
        /// <param name="key">The key the state was saved under.</param>
        /// <param name="state">The restored state, or null if none was found.</param>
        /// <returns>True if a state was found and restored; otherwise false.</returns>
        internal static bool TryLoad(string key, out PackageOperationState state)
        {
            string json = SessionState.GetString(Resolve(key), string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                state = null;
                return false;
            }

            state = JsonUtility.FromJson<PackageOperationState>(json);

            return state != null;
        }

        /// <summary>
        /// Removes any saved state for the given key.
        /// </summary>
        /// <param name="key">The key to clear.</param>
        internal static void Clear(string key) => SessionState.EraseString(Resolve(key));

        private static string Resolve(string key) => KeyPrefix + key;
    }
}