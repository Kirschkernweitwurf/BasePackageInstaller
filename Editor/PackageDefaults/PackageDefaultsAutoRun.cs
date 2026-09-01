using System.IO;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Regenerates the defaults file once per editor session, so the list the installer ships never
    /// drifts from the assembly definitions it is derived from.
    /// <para>
    /// Opening the project is the one moment the packages on disk and the generated file are
    /// guaranteed to be compared, without anyone having to remember the window exists. The run is
    /// silent unless the file actually changed.
    /// </para>
    /// <para>
    /// It does nothing outside the packages repository. A consuming project has no packages root to
    /// read, and its copy of the installer sits in the package cache, which is rebuilt on every
    /// import. Both cases return before anything is scanned, written or logged.
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    internal static class PackageDefaultsAutoRun
    {
        private const string EnabledPrefsKey = "Scripts.PackageDefaults.AutoRun";
        private const string SessionKey = "Base.PackageInstaller.PackageDefaults.AutoRun";

        /// <summary>
        /// Whether the defaults file is regenerated when the project is opened. Remembered per machine.
        /// </summary>
        internal static bool IsEnabled
        {
            get => EditorPrefs.GetBool(EnabledPrefsKey, true);
            set => EditorPrefs.SetBool(EnabledPrefsKey, value);
        }

        static PackageDefaultsAutoRun() => EditorApplication.delayCall += Run;

        private static void Run()
        {
            // Once per session, not once per domain reload. The scan reads every file in every package,
            // which is far too much to repeat after each recompile.
            if (!IsEnabled || SessionState.GetBool(SessionKey, false))
                return;

            // The asset database is still settling while the project opens, and the target is looked up
            // through it. Waiting a frame longer costs nothing here.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }

            // Marked before the work rather than after: writing the file triggers a recompile and a
            // second load, by which point this run has already happened.
            SessionState.SetBool(SessionKey, true);

            string root = PackageDefaultsPaths.LoadRoot();

            // Silent, not a warning. A consuming project is never going to have the packages root, and
            // this runs unprompted, so a missing root is the normal case rather than a problem.
            if (!Directory.Exists(root))
                return;

            string target = ResolveTarget();

            if (string.IsNullOrEmpty(target))
                return;

            Generate(root, target);
        }

        private static string ResolveTarget()
        {
            string target = PackageDefaultsPaths.LoadTarget();

            if (string.IsNullOrEmpty(target))
                target = PackageDefaultsPaths.LocateTarget();

            if (string.IsNullOrEmpty(target) || PackageDefaultsPaths.IsInPackageCache(target))
                return string.Empty;

            return target;
        }

        private static void Generate(string root, string target)
        {
            PackageDependencyInfo[] packages = PackageDependencyScanner.Scan(root);

            // The scanner has already reported why it found nothing, so there is nothing to add here.
            if (packages.Length == 0)
                return;

            string generated = PackageDefaultsWriter.Render(packages);
            DiffResult diff = TextDiff.Compare(generated, PackageDefaultsFile.Read(target), hasTarget: true);

            if (diff.State == EDiffState.Identical)
                return;

            PackageDefaultsFile.Write(target, generated);

            // A file that did not exist yet has no counts to report, because there was nothing to
            // compare it against.
            string counts = diff.State == EDiffState.Missing
                ? string.Empty
                : $" ({diff.AddedCount} added, {diff.RemovedCount} removed)";

            Debug.Log($"{nameof(PackageDefaultsAutoRun)}: wrote {Path.GetFileName(target)} "
                + $"from {packages.Length} packages{counts}.");
        }
    }
}