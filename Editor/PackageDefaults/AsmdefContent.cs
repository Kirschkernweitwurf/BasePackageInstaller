using System;
using UnityEngine;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Serializable mirror of the three fields of an assembly definition the scanner reads.
    /// The field names have to match the JSON keys, so they are not renamed to fit the usual style.
    /// </summary>
    [Serializable]
    internal sealed class AsmdefContent
    {
        [SerializeField] private string name;
        [SerializeField] private string[] references;
        [SerializeField] private string[] defineConstraints;

        /// <summary>The assembly name.</summary>
        internal string Name => name;

        /// <summary>The declared references, either as a GUID or as a plain assembly name.</summary>
        internal string[] References => references ?? Array.Empty<string>();

        /// <summary>
        /// True when the assembly only compiles under a define, which makes it an optional
        /// integration rather than something the owning package always needs.
        /// </summary>
        internal bool IsOptional => defineConstraints is
        {
            Length: > 0
        };
    }
}