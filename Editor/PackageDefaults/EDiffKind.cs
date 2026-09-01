namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// How a single line of the generated file compares to the line in the target file.
    /// </summary>
    internal enum EDiffKind : byte
    {
        /// <summary>The line is the same in both files.</summary>
        Unchanged = 0,

        /// <summary>The line only exists in the generated file.</summary>
        Added = 1,

        /// <summary>The line only exists in the file on disk.</summary>
        Removed = 2
    }
}