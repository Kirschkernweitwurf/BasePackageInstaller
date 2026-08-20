namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// The default base packages seeded into a fresh <see cref="BasePackageRegistry"/>.
    /// Generated from the assembly definitions by the Package Defaults window; edit that
    /// tool rather than this file.
    /// </summary>
    internal static class BasePackageDefaults
    {
        private const string Attributes = "Attributes";
        private const string ControllerSupport = "Controller Support";
        private const string Core = "Core";
        private const string EditorUi = "Editor UI";
        private const string Localization = "Localization";
        private const string MemoryProfiler = "Memory Profiler";
        private const string SaveSystem = "Save System";
        private const string Services = "Services";
        private const string Settings = "Settings System";
        private const string Tools = "Tools";
        private const string Tweening = "Tweening";
        private const string UI = "UI";
        private const string Utility = "Utility";

        private const string BaseUrl =
            "https://github.com/Kirschkernweitwurf/BaseProjectPackages.git?path=BaseProject/Packages/";

        /// <summary>
        /// Creates a fresh copy of the default entries.
        /// </summary>
        /// <returns>The default package entries.</returns>
        internal static PackageEntry[] Create() => new[]
        {
            new PackageEntry(Attributes, $"{BaseUrl}Attributes", EditorUi, Utility),
            new PackageEntry(ControllerSupport, $"{BaseUrl}ControllerSupport", Core),
            new PackageEntry(Core, $"{BaseUrl}Core", Tweening),
            new PackageEntry(EditorUi, $"{BaseUrl}EditorUi"),
            new PackageEntry(Localization, $"{BaseUrl}Localization", Utility),
            new PackageEntry(MemoryProfiler, $"{BaseUrl}MemoryProfiler", Core),
            new PackageEntry(SaveSystem, $"{BaseUrl}SaveSystem", Services),
            new PackageEntry(Services, $"{BaseUrl}Services", Attributes),
            new PackageEntry(Settings, $"{BaseUrl}Settings", Core),
            new PackageEntry(Tools, $"{BaseUrl}Tools", Attributes),
            new PackageEntry(Tweening, $"{BaseUrl}Tweening", Services),
            new PackageEntry(UI, $"{BaseUrl}UI", Core),
            new PackageEntry(Utility, $"{BaseUrl}Utility")
        };
    }
}