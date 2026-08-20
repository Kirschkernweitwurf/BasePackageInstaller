using System.Text;
using Base.PackageInstaller.Data;

namespace Base.PackageInstaller.Window.Format
{
    /// <summary>
    /// Turns package results into the human-readable text shown in the window and the console.
    /// Pure and free of any UI, so the wording lives in one place and can be tested on its own.
    /// </summary>
    internal static class OperationSummaryFormatter
    {
        private const char LineBreak = '\n';
        private const char SentenceEnd = '.';
        private const string UnchangedPhrase = "is already up to date";
        private const string VersionArrow = "->";

        /// <summary>One line describing the outcome of a single package.</summary>
        /// <param name="result">The result to describe.</param>
        /// <returns>The description shown to the user.</returns>
        internal static string Describe(PackageResult result)
        {
            if (!result.Success)
                return $"{result.Label} failed: {result.Error}";

            string resultName = string.IsNullOrEmpty(result.Name)
                ? result.Label
                : result.Name;

            if (string.IsNullOrEmpty(result.Version))
                return $"Installed {resultName}.";

            if (!result.Changed || result.PreviousVersion == result.Version)
                return $"{resultName} {UnchangedPhrase} ({result.Version}).";

            if (string.IsNullOrEmpty(result.PreviousVersion))
                return $"Installed {resultName} {result.Version}.";

            return $"Updated {resultName} {result.PreviousVersion} {VersionArrow} {result.Version}.";
        }

        /// <summary>A counts headline followed by one line per package.</summary>
        /// <param name="summary">The summary of the finished run.</param>
        /// <returns>The multi-line report shown to the user.</returns>
        internal static string BuildSummary(OperationSummary summary)
        {
            StringBuilder builder = new();

            builder.Append($"Done. {summary.SuccessCount} ok");

            if (summary.ChangedCount > 0)
                builder.Append($", {summary.ChangedCount} changed");

            if (summary.UnchangedCount > 0)
                builder.Append($", {summary.UnchangedCount} unchanged");

            if (summary.FailedCount > 0)
                builder.Append($", {summary.FailedCount} failed");

            builder.Append(SentenceEnd);

            foreach (PackageResult result in summary.Results)
            {
                builder.Append(LineBreak);
                builder.Append(Describe(result));
            }

            return builder.ToString();
        }
    }
}