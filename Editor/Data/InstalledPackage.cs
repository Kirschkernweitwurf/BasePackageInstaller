namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// A package installed in the project, with the version and Git hash it resolved to.
    /// </summary>
    internal readonly struct InstalledPackage
    {
        /// <summary>The installed version, or null if the package was not installed.</summary>
        internal string Version { get; }

        /// <summary>The installed Git commit hash, or null for packages from another source.</summary>
        internal string Hash { get; }

        /// <summary>Creates an entry describing an installed package.</summary>
        /// <param name="version">The installed version.</param>
        /// <param name="hash">The installed Git commit hash.</param>
        internal InstalledPackage(string version, string hash)
        {
            Version = version;
            Hash = hash;
        }
    }
}