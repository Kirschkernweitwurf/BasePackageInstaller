using System.Collections.Generic;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// Walks the dependency graph declared by <see cref="PackageEntry.DependsOn"/>.
    /// <para>
    /// Two jobs: making sure a selection is complete, so a package is never installed without what
    /// it needs, and putting the selection in an order that installs dependencies first, so the
    /// project compiles at every step of a run rather than only at the end.
    /// </para>
    /// </summary>
    internal static class PackageDependencyResolver
    {
        /// <summary>
        /// Selects every entry the current selection depends on, following the graph all the way
        /// down. An entry that names a dependency the registry does not contain is left alone.
        /// </summary>
        /// <param name="packages">The registry entries, in the order the window lists them.</param>
        /// <param name="selected">The per-entry selection flags, extended in place.</param>
        internal static void ExpandSelection(PackageEntry[] packages, bool[] selected)
        {
            if (packages == null || selected == null)
                return;

            Dictionary<string, int> byName = BuildIndex(packages);

            // A dependency can itself pull in another one, so the pass repeats until it adds nothing.
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int i = 0; i < packages.Length; i++)
                {
                    if (!selected[i])
                        continue;

                    foreach (string dependency in packages[i].DependsOn)
                    {
                        if (!byName.TryGetValue(dependency, out int index) || selected[index])
                            continue;

                        selected[index] = true;
                        changed = true;
                    }
                }
            }
        }

        /// <summary>
        /// Orders the selected entries so every package comes after the ones it depends on.
        /// </summary>
        /// <param name="packages">The registry entries, in the order the window lists them.</param>
        /// <param name="selected">The per-entry selection flags.</param>
        /// <returns>The URLs to process, dependencies first.</returns>
        internal static List<string> ResolveOrder(PackageEntry[] packages, bool[] selected)
        {
            List<string> ordered = new();

            if (packages == null || selected == null)
                return ordered;

            Dictionary<string, int> byName = BuildIndex(packages);
            HashSet<int> emitted = new();
            HashSet<int> visiting = new();

            for (int i = 0; i < packages.Length; i++)
            {
                if (selected[i])
                    Emit(i, packages, selected, byName, emitted, visiting, ordered);
            }

            return ordered;
        }

        private static Dictionary<string, int> BuildIndex(PackageEntry[] packages)
        {
            Dictionary<string, int> byName = new();

            for (int i = 0; i < packages.Length; i++)
                byName[packages[i].Name] = i;

            return byName;
        }

        // Depth-first post-order: a package is appended only once everything below it has been.
        private static void Emit(int index, PackageEntry[] packages, bool[] selected,
            IReadOnlyDictionary<string, int> byName, ISet<int> emitted, ISet<int> visiting, List<string> ordered)
        {
            if (emitted.Contains(index))
                return;

            // A cycle would recurse forever. The graph is hand-written, so treat one as an authoring
            // mistake and fall back to the order the entry was reached in.
            if (!visiting.Add(index))
                return;

            foreach (string dependency in packages[index].DependsOn)
            {
                if (byName.TryGetValue(dependency, out int dependencyIndex)
                    && selected[dependencyIndex])
                    Emit(dependencyIndex, packages, selected, byName, emitted, visiting, ordered);
            }

            visiting.Remove(index);
            emitted.Add(index);

            ordered.Add(packages[index].Url);
        }
    }
}