using System.Collections.Generic;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// Names the installer used to list packages under, mapped to the names it lists them under now.
    /// <para>
    /// The registry matches its entries by name, so a renamed default would be added a second time
    /// and the old row left behind in every project that already holds one.
    /// <see cref="BasePackageRegistry.SyncWithDefaults"/> runs every entry and every dependency
    /// through here first, which turns that into a rename instead of a duplicate.
    /// </para>
    /// <para>
    /// Entries stay here for good. A project that has not been opened since the rename still holds
    /// the old name, and there is no way to know that from inside the project that has.
    /// </para>
    /// </summary>
    internal static class LegacyPackageNames
    {
        private static readonly Dictionary<string, string> Renames = new()
        {
            ["Settings System"] = "Settings"
        };

        /// <summary>
        /// Maps a stored package name onto the name currently in use.
        /// </summary>
        /// <param name="name">The name read from the registry.</param>
        /// <returns>The current name, or the given name when it was never renamed.</returns>
        internal static string Resolve(string name) => Renames.TryGetValue(name, out string renamed)
            ? renamed
            : name;
    }
}