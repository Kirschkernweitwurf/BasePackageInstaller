using System;
using UnityEngine;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// A package entry with a friendly name and a Git URL.
    /// Serializable so it can be edited in the package registry inspector.
    /// </summary>
    [Serializable]
    internal sealed class PackageEntry
    {
        [SerializeField] private string name;
        [SerializeField] private string url;

        /// <summary>The friendly name shown in the window.</summary>
        internal string Name => name;

        /// <summary>The Git URL the package is added from.</summary>
        internal string Url => url;

        /// <summary>Creates an entry for a single Git package.</summary>
        /// <param name="name">The friendly name shown in the window.</param>
        /// <param name="url">The Git URL the package is added from.</param>
        internal PackageEntry(string name, string url)
        {
            this.name = name;
            this.url = url;
        }
    }
}