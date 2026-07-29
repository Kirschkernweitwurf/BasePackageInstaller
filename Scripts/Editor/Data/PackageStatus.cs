namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// The current install status of a registry package in the project.
    /// </summary>
    internal readonly struct PackageStatus
    {
        /// <summary>True if the package is currently installed.</summary>
        internal bool IsInstalled { get; }

        /// <summary>The installed version, or empty if not installed.</summary>
        internal string Version { get; }

        /// <summary>Creates a status for a single registry package.</summary>
        /// <param name="isInstalled">Whether the package is installed.</param>
        /// <param name="version">The installed version.</param>
        internal PackageStatus(bool isInstalled, string version)
        {
            IsInstalled = isInstalled;
            Version = version;
        }
    }
}