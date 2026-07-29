using System.Collections.Generic;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// Summary of a completed run over multiple packages.
    /// </summary>
    internal readonly struct OperationSummary
    {
        /// <summary>The result of every package that was processed.</summary>
        internal IReadOnlyList<PackageResult> Results { get; }

        /// <summary>The number of packages that succeeded.</summary>
        internal int SuccessCount { get; }

        /// <summary>The number of packages that failed.</summary>
        internal int FailedCount { get; }

        /// <summary>The number of packages whose installed content changed.</summary>
        internal int ChangedCount { get; }

        /// <summary>The number of packages that were already up to date.</summary>
        internal int UnchangedCount { get; }

        /// <summary>True if at least one package failed.</summary>
        internal bool HasFailures => FailedCount > 0;

        /// <summary>Creates the summary of a completed run.</summary>
        /// <param name="results">The result of every package that was processed.</param>
        /// <param name="successCount">The number of packages that succeeded.</param>
        /// <param name="failedCount">The number of packages that failed.</param>
        /// <param name="changedCount">The number of packages whose installed content changed.</param>
        /// <param name="unchangedCount">The number of packages that were already up to date.</param>
        internal OperationSummary(IReadOnlyList<PackageResult> results, int successCount,
            int failedCount, int changedCount, int unchangedCount)
        {
            Results = results;
            SuccessCount = successCount;
            FailedCount = failedCount;
            ChangedCount = changedCount;
            UnchangedCount = unchangedCount;
        }
    }
}