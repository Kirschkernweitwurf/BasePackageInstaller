using System;
using UnityEngine;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// One package for an operation to process: the identifier handed to the package manager,
    /// plus the friendly label reported back to the user.
    /// </summary>
    /// <remarks>
    /// The identifier differs per operation. Adding a package takes a Git URL, removing one takes
    /// the resolved package name, and neither reads well in a log line, so the label travels with
    /// it instead of being parsed back out. Serializable because a run has to survive the domain
    /// reload an install or a removal triggers.
    /// </remarks>
    [Serializable]
    internal struct PackageRequest
    {
        [SerializeField] private string label;
        [SerializeField] private string id;

        /// <summary>The friendly name shown in the window and the console.</summary>
        internal string Label => label;

        /// <summary>The Git URL or package name handed to the package manager.</summary>
        internal string Id => id;

        /// <summary>Creates a request for a single package.</summary>
        /// <param name="label">The friendly name shown in the window and the console.</param>
        /// <param name="id">The Git URL or package name handed to the package manager.</param>
        internal PackageRequest(string label, string id)
        {
            this.label = label;
            this.id = id;
        }
    }
}