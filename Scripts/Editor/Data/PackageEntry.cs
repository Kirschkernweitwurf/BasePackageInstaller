using System;
using UnityEngine;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// A package entry with a friendly name, a Git URL and the entries it needs installed with it.
    /// Serializable so it can be edited in the package registry inspector.
    /// </summary>
    [Serializable]
    internal sealed class PackageEntry
    {
        [SerializeField] private string name;
        [SerializeField] private string url;
        [SerializeField] private string[] dependsOn;

        /// <summary>The friendly name shown in the window.</summary>
        internal string Name => name;

        /// <summary>The Git URL the package is added from.</summary>
        internal string Url => url;

        /// <summary>
        /// The names of the entries this package needs. Only direct dependencies are listed; the
        /// rest of the graph is walked by <see cref="PackageDependencyResolver"/>.
        /// </summary>
        /// <remarks>
        /// Base packages cannot declare each other in their <c>package.json</c>, because UPM would
        /// try to resolve them from a registry and fail. The graph lives here instead.
        /// </remarks>
        internal string[] DependsOn => dependsOn ?? Array.Empty<string>();

        /// <summary>Creates an entry for a single Git package.</summary>
        /// <param name="name">The friendly name shown in the window.</param>
        /// <param name="url">The Git URL the package is added from.</param>
        /// <param name="dependsOn">The names of the entries this package directly needs.</param>
        internal PackageEntry(string name, string url, params string[] dependsOn)
        {
            this.name = name;
            this.url = url;
            this.dependsOn = dependsOn;
        }
    }
}