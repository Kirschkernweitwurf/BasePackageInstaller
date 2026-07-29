namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// The action the primary button performs for the current selection, derived from how many
    /// selected packages are already installed. Drives the button label.
    /// </summary>
    internal enum EInstallAction : byte
    {
        /// <summary>The selection mixes installed and missing packages, or status is still loading.</summary>
        InstallOrUpdate = 0,

        /// <summary>Every selected package is missing.</summary>
        Install = 1,

        /// <summary>Every selected package is already installed.</summary>
        Update = 2
    }
}