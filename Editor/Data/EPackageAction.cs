namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// What an operation does to the packages it processes. Travels with every result, because
    /// once a run is finished the wording of its report is the only thing left that differs.
    /// </summary>
    internal enum EPackageAction : byte
    {
        /// <summary>The package is added as a Git dependency, installing or updating it.</summary>
        Add = 0,

        /// <summary>The package is removed from the project.</summary>
        Remove = 1
    }
}