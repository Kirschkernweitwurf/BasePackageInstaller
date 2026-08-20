using System;
using System.Collections.Generic;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// Walks the dependency graph declared by <see cref="PackageEntry.DependsOn"/>.
    /// <para>
    /// The window keeps two sets of flags: the rows the user ticked, and the rows that are
    /// actually going to be processed. Only the first is ever edited by hand; the second is
    /// derived here from scratch every time the first changes. That is what makes unticking a
    /// package release the dependencies it pulled in, while leaving alone anything another ticked
    /// package still needs, or that the user ticked themselves.
    /// </para>
    /// </summary>
    internal static class PackageDependencyResolver
    {
        private const string NameSeparator = ", ";

        /// <summary>
        /// Derives the effective selection from what the user ticked, following the graph all the
        /// way down. A dependency naming an entry the registry does not contain is ignored.
        /// </summary>
        /// <param name="packages">The registry entries, in the order the window lists them.</param>
        /// <param name="userSelected">The rows the user ticked. Never modified.</param>
        /// <param name="selected">Filled with the rows that will be processed.</param>
        /// <param name="requiredBy">
        /// Filled with the names of the selected entries that require each package, or <c>null</c>
        /// where nothing requires it. This is what the window shows in the Required By column, and
        /// it is filled either way: with dependencies off it reports what a pick would have pulled
        /// in, which is exactly the information needed to judge whether leaving it out is safe.
        /// </param>
        /// <param name="expandDependencies">
        /// False to take the user's picks exactly as they are. Nothing is added, so a single
        /// package can be updated on its own without its whole chain coming along.
        /// </param>
        internal static void Resolve(PackageEntry[] packages, bool[] userSelected, bool[] selected,
            string[] requiredBy, bool expandDependencies)
        {
            if (packages == null || userSelected == null || selected == null)
                return;

            Array.Copy(userSelected, selected, selected.Length);

            Dictionary<string, int> byName = BuildIndex(packages);

            if (expandDependencies)
                Expand(packages, selected, byName);

            FillRequiredBy(packages, selected, byName, requiredBy);
        }

        // A dependency can itself pull in another one, so the pass repeats until it adds nothing.
        private static void Expand(PackageEntry[] packages, bool[] selected,
            IReadOnlyDictionary<string, int> byName)
        {
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
        /// <para>
        /// Works outwards in layers: everything whose dependencies are already satisfied goes
        /// first, so a run starts at the leaves and climbs the chain. Each package therefore lands
        /// in a project where everything it needs is already present, which is what keeps the
        /// recompile after each install clean instead of erroring until the last one arrives.
        /// </para>
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
            List<int> remaining = new();

            for (int i = 0; i < packages.Length; i++)
            {
                if (selected[i])
                    remaining.Add(i);
            }

            HashSet<int> placed = new();

            while (remaining.Count > 0)
            {
                int next = TakeReady(remaining, packages, byName, placed);

                // Nothing is ready, so whatever is left depends on itself in a loop. The registry
                // page reports that; here the run continues in list order rather than stalling.
                if (next < 0)
                    next = 0;

                int index = remaining[next];

                remaining.RemoveAt(next);
                placed.Add(index);
                ordered.Add(packages[index].Url);
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

        // Every selected package claims the entries it directly needs, so a row can name all of
        // its holders rather than only the first one that happened to reach it.
        private static void FillRequiredBy(PackageEntry[] packages, bool[] selected,
            IReadOnlyDictionary<string, int> byName, string[] requiredBy)
        {
            if (requiredBy == null)
                return;

            Array.Clear(requiredBy, 0, requiredBy.Length);

            for (int i = 0; i < packages.Length; i++)
            {
                if (!selected[i])
                    continue;

                foreach (string dependency in packages[i].DependsOn)
                {
                    if (!byName.TryGetValue(dependency, out int index) || index == i)
                        continue;

                    requiredBy[index] = string.IsNullOrEmpty(requiredBy[index])
                        ? packages[i].Name
                        : requiredBy[index] + NameSeparator + packages[i].Name;
                }
            }
        }

        // The first entry whose selected dependencies have all been placed. The registry is sorted
        // by name, so a whole layer comes out alphabetically before the next one starts.
        private static int TakeReady(IReadOnlyList<int> remaining, PackageEntry[] packages,
            IReadOnlyDictionary<string, int> byName, ICollection<int> placed)
        {
            for (int i = 0; i < remaining.Count; i++)
            {
                if (IsReady(packages[remaining[i]], remaining[i], byName, placed))
                    return i;
            }

            return -1;
        }

        private static bool IsReady(PackageEntry entry, int index, IReadOnlyDictionary<string, int> byName,
            ICollection<int> placed)
        {
            foreach (string dependency in entry.DependsOn)
            {
                if (!byName.TryGetValue(dependency, out int dependencyIndex))
                    continue;

                if (dependencyIndex != index && !placed.Contains(dependencyIndex))
                    return false;
            }

            return true;
        }
    }
}