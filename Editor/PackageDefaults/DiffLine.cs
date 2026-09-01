namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// One line of a rendered diff, together with how it differs from the target file.
    /// </summary>
    internal readonly struct DiffLine
    {
        /// <summary>How the line compares to the target file.</summary>
        internal EDiffKind Kind { get; }

        /// <summary>The line text, without its line break.</summary>
        internal string Text { get; }

        /// <summary>Creates a single rendered diff line.</summary>
        /// <param name="kind">How the line compares to the target file.</param>
        /// <param name="text">The line text.</param>
        internal DiffLine(EDiffKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }
}