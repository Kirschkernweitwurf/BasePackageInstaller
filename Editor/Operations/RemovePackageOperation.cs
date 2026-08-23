using System.Collections.Generic;
using Base.PackageInstaller.Data;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Base.PackageInstaller.Operations
{
    /// <summary>
    /// Removes packages from the project. <see cref="Client.Remove(string)"/> is given the
    /// resolved package name rather than the Git URL it was installed from, which is why a
    /// <see cref="PackageRequest"/> carries the friendly label separately.
    /// </summary>
    internal sealed class RemovePackageOperation : PackageOperation
    {
        /// <inheritdoc/>
        protected override EPackageAction Action => EPackageAction.Remove;

        /// <inheritdoc/>
        protected override Request CreateRequest(string id) => Client.Remove(id);

        /// <inheritdoc/>
        /// <remarks>
        /// A removal cannot be asked for twice. The package manager reports the second attempt as
        /// the package not being in the manifest, which is the outcome the request wanted, so a
        /// package that is no longer there counts as removed however many times it was asked for.
        /// </remarks>
        protected override bool IsAlreadySettled(PackageRequest request, ISet<string> installedNames)
            => !installedNames.Contains(request.Id);
    }
}