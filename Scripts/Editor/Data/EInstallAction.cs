#if UNITY_EDITOR
namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// The action the primary button performs for the current selection, derived from how many
    /// selected packages are already installed. Drives the button label.
    /// </summary>
    public enum EInstallAction : byte
    {
        /// <summary>The selection mixes installed and missing packages, or status is still loading.</summary>
        InstallOrUpdate,

        /// <summary>Every selected package is missing.</summary>
        Install,

        /// <summary>Every selected package is already installed.</summary>
        Update
    }
}
#endif
