#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Base.PackageInstaller.Editor
{
    /// <summary>
    /// Updates packages sequentially by re-adding Git dependencies.
    /// </summary>
    public sealed class PackageUpdater : PackageOperation
    {
        /// <inheritdoc/>
        protected override Request CreateRequest(string url) => Client.Add(url);

        /// <inheritdoc/>
        protected override string GetPackageName()
        {
            return CurrentRequest is AddRequest request
                ? request.Result.name
                : "Package";
        }
    }
}
#endif