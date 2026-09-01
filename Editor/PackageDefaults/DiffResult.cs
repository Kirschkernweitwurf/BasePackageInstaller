using System.Collections.Generic;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// The comparison between the generated text and the target file: the overall state, the counts
    /// shown in the status line and the rendered lines the diff view draws.
    /// </summary>
    internal readonly struct DiffResult
    {
        /// <summary>The overall outcome of the comparison.</summary>
        internal EDiffState State { get; }

        /// <summary>The number of lines only present in the generated file.</summary>
        internal int AddedCount { get; }

        /// <summary>The number of lines only present in the file on disk.</summary>
        internal int RemovedCount { get; }

        /// <summary>Every line of the comparison, in reading order.</summary>
        internal IReadOnlyList<DiffLine> Lines { get; }

        /// <summary>Creates the result of a comparison.</summary>
        /// <param name="state">The overall outcome.</param>
        /// <param name="addedCount">The number of added lines.</param>
        /// <param name="removedCount">The number of removed lines.</param>
        /// <param name="lines">Every line of the comparison.</param>
        internal DiffResult(EDiffState state, int addedCount, int removedCount, IReadOnlyList<DiffLine> lines)
        {
            State = state;
            AddedCount = addedCount;
            RemovedCount = removedCount;
            Lines = lines;
        }
    }
}