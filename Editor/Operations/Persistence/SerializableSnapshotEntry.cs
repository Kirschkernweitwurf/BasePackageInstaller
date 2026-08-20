using System;
using Base.PackageInstaller.Data;
using UnityEngine;

namespace Base.PackageInstaller.Operations.Persistence
{
    /// <summary>
    /// Serializable mirror of an <see cref="InstalledPackage"/> together with its package name.
    /// </summary>
    [Serializable]
    internal struct SerializableSnapshotEntry
    {
        [SerializeField] private string name;
        [SerializeField] private string version;
        [SerializeField] private string hash;

        /// <summary>The package name the snapshot entry belongs to.</summary>
        internal string Name => name;

        /// <summary>Creates the serializable mirror of a single snapshot entry.</summary>
        /// <param name="name">The package name.</param>
        /// <param name="package">The installed package to mirror.</param>
        internal SerializableSnapshotEntry(string name, InstalledPackage package)
        {
            this.name = name;
            version = package.Version;
            hash = package.Hash;
        }

        /// <summary>Rebuilds the immutable installed package this entry was created from.</summary>
        /// <returns>The restored installed package.</returns>
        internal InstalledPackage ToInstalledPackage() => new(version, hash);
    }
}