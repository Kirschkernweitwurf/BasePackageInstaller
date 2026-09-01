using System.Collections.Generic;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// One package as the generator sees it: the folder it lives in, the name the installer lists it
    /// under, and the packages it directly depends on after the redundant edges have been removed.
    /// </summary>
    internal readonly struct PackageDependencyInfo
    {
        /// <summary>The folder name under the packages root, which is also the Git URL path segment.</summary>
        internal string FolderName { get; }

        /// <summary>The friendly name the installer window lists the package under.</summary>
        internal string DisplayName { get; }

        /// <summary>The folder names this package directly depends on, sorted alphabetically.</summary>
        internal IReadOnlyList<string> DirectDependencies { get; }

        /// <summary>Creates the description of a single scanned package.</summary>
        /// <param name="folderName">The folder name under the packages root.</param>
        /// <param name="displayName">The friendly name the installer lists the package under.</param>
        /// <param name="directDependencies">The folder names this package directly depends on.</param>
        internal PackageDependencyInfo(string folderName, string displayName,
            IReadOnlyList<string> directDependencies)
        {
            FolderName = folderName;
            DisplayName = displayName;
            DirectDependencies = directDependencies;
        }
    }
}