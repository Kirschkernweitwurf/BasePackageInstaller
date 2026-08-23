using Base.PackageInstaller.Data;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Base.PackageInstaller.Operations
{
    /// <summary>
    /// Adds packages as Git dependencies. <see cref="Client.Add(string)"/> installs a package
    /// if missing and re-resolves it to the latest remote version if already present, so this
    /// single operation covers both installing and updating.
    /// </summary>
    internal sealed class GitPackageOperation : PackageOperation
    {
        /// <inheritdoc/>
        protected override EPackageAction Action => EPackageAction.Add;

        /// <inheritdoc/>
        protected override Request CreateRequest(string id) => Client.Add(id);
    }
}