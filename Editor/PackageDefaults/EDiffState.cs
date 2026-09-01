namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// The overall outcome of comparing the generated file against the target file on disk.
    /// </summary>
    internal enum EDiffState : byte
    {
        /// <summary>No target file has been picked yet.</summary>
        NoTarget = 0,

        /// <summary>A target is set but nothing exists at that path.</summary>
        Missing = 1,

        /// <summary>The generated file matches the file on disk exactly.</summary>
        Identical = 2,

        /// <summary>The two differ, so writing would change the file.</summary>
        Changed = 3
    }
}