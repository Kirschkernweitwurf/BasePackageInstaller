using System;
using Base.PackageInstaller.Data;

namespace Base.PackageInstaller.Operations.Persistence
{
    /// <summary>
    /// Serializable mirror of an <see cref="InstalledPackage"/> together with its package name.
    /// </summary>
    [Serializable]
    public struct SerializableSnapshotEntry
    {
        public string name;
        public string version;
        public string hash;
    }
}