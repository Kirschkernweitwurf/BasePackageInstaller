namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// The direction the window works in. The two modes walk the same dependency graph opposite
    /// ways: installing a package pulls in what it needs, removing one pulls in what needs it.
    /// </summary>
    internal enum EPackageMode : byte
    {
        /// <summary>Packages are installed or updated, and a tick pulls in its dependencies.</summary>
        Install = 0,

        /// <summary>Packages are removed, and a tick pulls in the packages that depend on it.</summary>
        Uninstall = 1
    }
}