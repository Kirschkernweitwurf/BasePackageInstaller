namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// The default base packages seeded into a fresh <see cref="BasePackageRegistry"/>.
    /// Other projects can edit the registry afterward; these are only the starting set.
    /// </summary>
    internal static class BasePackageDefaults
    {
        private const string BaseUrl =
            "https://github.com/Kirschkernweitwurf/BaseProjectPackages.git?path=BaseProject/Packages/";

        /// <summary>
        /// Creates a fresh copy of the default entries.
        /// </summary>
        /// <returns>The default package entries.</returns>
        internal static PackageEntry[] Create() => new[]
        {
            new PackageEntry("Attributes", $"{BaseUrl}Attributes"),
            new PackageEntry("Controller Support", $"{BaseUrl}ControllerSupport"),
            new PackageEntry("Core", $"{BaseUrl}Core"),
            new PackageEntry("Localization", $"{BaseUrl}Localization"),
            new PackageEntry("Memory Profiler", $"{BaseUrl}MemoryProfiler"),
            new PackageEntry("Save System", $"{BaseUrl}SaveSystem"),
            new PackageEntry("Settings System", $"{BaseUrl}Settings"),
            new PackageEntry("Tools", $"{BaseUrl}Tools"),
            new PackageEntry("UI", $"{BaseUrl}UI"),
            new PackageEntry("Utility", $"{BaseUrl}Utility")
        };
    }
}