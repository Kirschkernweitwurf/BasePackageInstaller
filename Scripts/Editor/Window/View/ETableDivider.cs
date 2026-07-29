namespace Base.PackageInstaller.Window.View
{
    /// <summary>
    /// The draggable dividers between the resizable columns of the package table.
    /// </summary>
    internal enum ETableDivider : byte
    {
        /// <summary>No divider is currently being dragged.</summary>
        None = 0,

        /// <summary>The divider between the Package and Status columns.</summary>
        NameStatus = 1,

        /// <summary>The divider between the Status and Version columns.</summary>
        StatusVersion = 2
    }
}