namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// Outcome of a single package operation.
    /// </summary>
    internal readonly struct PackageResult
    {
        /// <summary>The friendly label taken from the package URL.</summary>
        internal string Label { get; }

        /// <summary>The resolved package name (falls back to the label if unknown).</summary>
        internal string Name { get; }

        /// <summary>The resolved package version (empty if unknown).</summary>
        internal string Version { get; }

        /// <summary>The version installed before the operation (empty if it was not installed before).</summary>
        internal string PreviousVersion { get; }

        /// <summary>True if the installed content changed (a new install or a new version or commit).</summary>
        internal bool Changed { get; }

        /// <summary>True if the operation succeeded.</summary>
        internal bool Success { get; }

        /// <summary>The error message if the operation failed; otherwise null.</summary>
        internal string Error { get; }

        /// <summary>Creates the outcome of a single package operation.</summary>
        /// <param name="label">The friendly label taken from the package URL.</param>
        /// <param name="name">The resolved package name.</param>
        /// <param name="version">The resolved package version.</param>
        /// <param name="previousVersion">The version installed before the operation.</param>
        /// <param name="changed">Whether the installed content changed.</param>
        /// <param name="success">Whether the operation succeeded.</param>
        /// <param name="error">The error message if the operation failed.</param>
        internal PackageResult(string label, string name, string version, string previousVersion,
            bool changed, bool success, string error)
        {
            Label = label;
            Name = name;
            Version = version;
            PreviousVersion = previousVersion;
            Changed = changed;
            Success = success;
            Error = error;
        }
    }
}