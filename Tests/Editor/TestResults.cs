using Base.PackageInstaller.Data;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Builds the package results the report formatter is tested against, so each test names only
    /// the one or two fields that decide the wording it is checking.
    /// </summary>
    internal static class TestResults
    {
        /// <summary>The label a result carries when nothing else identifies it.</summary>
        internal const string Label = "Utility";

        /// <summary>The resolved package name.</summary>
        internal const string Name = "com.base.utility";

        /// <summary>The version an install lands on.</summary>
        internal const string NewVersion = "1.2.0";

        /// <summary>The version already installed before a run.</summary>
        internal const string OldVersion = "1.1.0";

        /// <summary>A package that was installed fresh, with no version known before.</summary>
        /// <param name="version">The version it landed on.</param>
        /// <returns>The result.</returns>
        internal static PackageResult Installed(string version = NewVersion)
            => new(Label, Name, version, string.Empty, true, true, null, EPackageAction.Add);

        /// <summary>A package that moved from one version to another.</summary>
        /// <returns>The result.</returns>
        internal static PackageResult Updated()
            => new(Label, Name, NewVersion, OldVersion, true, true, null, EPackageAction.Add);

        /// <summary>A package that was already at the version the run would have installed.</summary>
        /// <returns>The result.</returns>
        internal static PackageResult Unchanged()
            => new(Label, Name, NewVersion, NewVersion, false, true, null, EPackageAction.Add);

        /// <summary>A package that was taken out of the project.</summary>
        /// <param name="previousVersion">The version it held before, or empty when unknown.</param>
        /// <returns>The result.</returns>
        internal static PackageResult Removed(string previousVersion = OldVersion)
            => new(Label, Name, string.Empty, previousVersion, true, true, null, EPackageAction.Remove);

        /// <summary>A package the run could not process.</summary>
        /// <param name="error">The reason it failed.</param>
        /// <returns>The result.</returns>
        internal static PackageResult Failed(string error)
            => new(Label, Name, string.Empty, string.Empty, false, false, error, EPackageAction.Add);

        /// <summary>Wraps results into the summary a finished run reports.</summary>
        /// <param name="action">What the run did to the packages.</param>
        /// <param name="successCount">The number that succeeded.</param>
        /// <param name="failedCount">The number that failed.</param>
        /// <param name="changedCount">The number whose content changed.</param>
        /// <param name="unchangedCount">The number that were already up to date.</param>
        /// <param name="results">The individual results.</param>
        /// <returns>The summary.</returns>
        internal static OperationSummary Summary(EPackageAction action, int successCount, int failedCount,
            int changedCount, int unchangedCount, params PackageResult[] results)
            => new(results, action, successCount, failedCount, changedCount, unchangedCount);
    }
}