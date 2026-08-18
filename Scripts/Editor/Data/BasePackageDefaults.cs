namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// The default base packages seeded into a fresh <see cref="BasePackageRegistry"/>.
    /// Other projects can edit the registry afterward; these are only the starting set.
    /// <para>
    /// Each entry lists only its direct dependencies. The rest of the graph is walked by
    /// <see cref="PackageDependencyResolver"/>, so a package installed on its own still brings
    /// everything it needs, in an order that compiles.
    /// </para>
    /// </summary>
    internal static class BasePackageDefaults
    {
        private const string Attributes = "Attributes";
        private const string BaseUrl =
            "https://github.com/Kirschkernweitwurf/BaseProjectPackages.git?path=BaseProject/Packages/";
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
        private const string Ui = "UI";
        private const string Utility = "Utility";

        /// <summary>
        /// Creates a fresh copy of the default entries.
        /// </summary>
        /// <returns>The default package entries.</returns>
        internal static PackageEntry[] Create() => new[]
        {
            new PackageEntry(Attributes, $"{BaseUrl}Attributes", Utility, EditorUi),
            new PackageEntry(ControllerSupport, $"{BaseUrl}ControllerSupport", Core, EditorUi),
            new PackageEntry(Core, $"{BaseUrl}Core", Tweening),
            new PackageEntry(EditorUi, $"{BaseUrl}EditorUi"),
            new PackageEntry(Localization, $"{BaseUrl}Localization", Attributes),
            new PackageEntry(MemoryProfiler, $"{BaseUrl}MemoryProfiler", Core),
            new PackageEntry(SaveSystem, $"{BaseUrl}SaveSystem", Services),
            new PackageEntry(Services, $"{BaseUrl}Services", Attributes),
            new PackageEntry(Settings, $"{BaseUrl}Settings", Core),
            new PackageEntry(Tools, $"{BaseUrl}Tools", Attributes, EditorUi),
            new PackageEntry(Tweening, $"{BaseUrl}Tweening", Services),
            new PackageEntry(Ui, $"{BaseUrl}UI", Core),
            new PackageEntry(Utility, $"{BaseUrl}Utility")
        };
    }
}