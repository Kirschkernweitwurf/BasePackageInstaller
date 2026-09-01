using System;
using System.Collections.Generic;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Line based comparison of the generated text against the file on disk.
    /// <para>
    /// A longest common subsequence over the lines, which is what makes the result readable: the
    /// lines both files share stay in place and only the genuine edits show up as added or removed.
    /// A naive line by line walk would instead mark everything after the first inserted line as
    /// changed. The generated file is well under a thousand lines, so the quadratic table is free.
    /// </para>
    /// </summary>
    internal static class TextDiff
    {
        private static readonly string[] LineSeparators =
        {
            "\r\n",
            "\n"
        };

        /// <summary>
        /// Compares generated text against the current contents of the target file.
        /// </summary>
        /// <param name="generated">The freshly rendered text.</param>
        /// <param name="onDisk">The current file contents, or null when the file does not exist.</param>
        /// <param name="hasTarget">False when no target path has been picked yet.</param>
        /// <returns>The comparison, ready to be drawn.</returns>
        internal static DiffResult Compare(string generated, string onDisk, bool hasTarget)
        {
            if (!hasTarget)
                return new DiffResult(EDiffState.NoTarget, 0, 0, Array.Empty<DiffLine>());

            if (onDisk == null)
                return new DiffResult(EDiffState.Missing, 0, 0, Array.Empty<DiffLine>());

            // Compared on content, not on bytes, so a line ending difference alone never counts as a
            // change. The writer always emits CRLF, so that would otherwise be a permanent false diff.
            string[] left = Split(onDisk);
            string[] right = Split(generated);

            List<DiffLine> lines = new();
            int added = 0;
            int removed = 0;

            Walk(left, right, lines, ref added, ref removed);

            EDiffState state = added == 0 && removed == 0
                ? EDiffState.Identical
                : EDiffState.Changed;

            return new DiffResult(state, added, removed, lines);
        }

        private static string[] Split(string value) => value.Split(LineSeparators, StringSplitOptions.None);

        private static void Walk(string[] left, string[] right, List<DiffLine> lines, ref int added,
            ref int removed)
        {
            int[,] table = BuildTable(left, right);

            int i = 0;
            int j = 0;

            while (i < left.Length && j < right.Length)
            {
                if (left[i] == right[j])
                {
                    lines.Add(new DiffLine(EDiffKind.Unchanged, right[j]));
                    i++;
                    j++;
                    continue;
                }

                if (table[i + 1, j] >= table[i, j + 1])
                {
                    lines.Add(new DiffLine(EDiffKind.Removed, left[i]));
                    removed++;
                    i++;
                    continue;
                }

                lines.Add(new DiffLine(EDiffKind.Added, right[j]));
                added++;
                j++;
            }

            while (i < left.Length)
            {
                lines.Add(new DiffLine(EDiffKind.Removed, left[i]));
                removed++;
                i++;
            }

            while (j < right.Length)
            {
                lines.Add(new DiffLine(EDiffKind.Added, right[j]));
                added++;
                j++;
            }
        }

        // table[i, j] is the length of the longest common subsequence of left from i and right from j,
        // filled from the end so the walk above can always pick the direction that keeps the most lines.
        private static int[,] BuildTable(string[] left, string[] right)
        {
            int[,] table = new int[left.Length + 1, right.Length + 1];

            for (int i = left.Length - 1; i >= 0; i--)
            {
                for (int j = right.Length - 1; j >= 0; j--)
                {
                    table[i, j] = left[i] == right[j]
                        ? table[i + 1, j + 1] + 1
                        : Math.Max(table[i + 1, j], table[i, j + 1]);
                }
            }

            return table;
        }
    }
}