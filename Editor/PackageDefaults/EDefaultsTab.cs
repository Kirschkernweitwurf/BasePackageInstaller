namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// The pages of the Package Defaults window, shown as a toolbar above the content area.
    /// </summary>
    internal enum EDefaultsTab : byte
    {
        /// <summary>The resolved dependency graph, one row per package.</summary>
        Graph = 0,

        /// <summary>The generated file exactly as it would be written.</summary>
        Preview = 1,

        /// <summary>The line by line comparison against the target file.</summary>
        Diff = 2
    }
}