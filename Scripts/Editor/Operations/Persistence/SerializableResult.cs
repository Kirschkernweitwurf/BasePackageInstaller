using System;
using Base.PackageInstaller.Data;

namespace Base.PackageInstaller.Operations.Persistence
{
    /// <summary>
    /// Serializable mirror of <see cref="PackageResult"/>.
    /// </summary>
    [Serializable]
    public struct SerializableResult
    {
        public string label;
        public string name;
        public string version;
        public string previousVersion;
        public bool changed;
        public bool success;
        public string error;
    }
}