namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// The action the primary button performs for the current selection, derived from the window
    /// mode and from how many selected packages are already installed. Drives the button label.
    /// </summary>
    internal enum EInstallAction : byte
    {
        /// <summary>Every selected package is missing.</summary>
        Install = 0,

        /// <summary>The selection mixes installed and missing packages, or status is still loading.</summary>
        InstallOrUpdate = 1,

        /// <summary>There is nothing selected the current mode could act on.</summary>
        Nothing = 2,

        /// <summary>The selection is removed from the project.</summary>
        Uninstall = 3,

        /// <summary>Every selected package is already installed.</summary>
        Update = 4
    }
}