namespace Base.PackageInstaller.Window.View
{
    /// <summary>
    /// The draggable dividers between the resizable columns of the package table.
    /// </summary>
    internal enum ETableDivider : byte
    {
        /// <summary>No divider is currently being dragged.</summary>
        None = 0,

        /// <summary>The divider between the Package and Required By columns.</summary>
        NameRequired = 1,

        /// <summary>The divider between the Required By and Status columns.</summary>
        RequiredStatus = 2,

        /// <summary>The divider between the Status and Version columns.</summary>
        StatusVersion = 3
    }
}