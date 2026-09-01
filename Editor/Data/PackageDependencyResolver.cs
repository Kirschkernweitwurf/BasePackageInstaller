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
    /// package release what it pulled in, while leaving alone anything another ticked package
    /// still needs, or that the user ticked themselves.
    /// </para>
    /// <para>
    /// Every edge is walked in the direction the mode calls for. Installing follows it downwards,
    /// so a pick brings its dependencies along. Removing follows it upwards, so a pick brings
    /// along the packages that would not compile without it.
    /// </para>
    /// </summary>
    internal static class PackageDependencyResolver
    {
        private const string NameSeparator = ", ";

        /// <summary>
        /// Derives the effective selection from what the user ticked, following the graph all the
        /// way along. A dependency naming an entry the registry does not contain is ignored.
        /// </summary>
        /// <param name="packages">The registry entries, in the order the window lists them.</param>
        /// <param name="userSelected">The rows the user ticked. Never modified.</param>
        /// <param name="selected">Filled with the rows that will be processed.</param>
        /// <param name="heldBy">
        /// Filled with the names of the selected entries that drag each row into the run, or
        /// <c>null</c> where nothing does. Installing, those are the packages that require the
        /// row; removing, they are the ones the row requires. This is what the window shows in
        /// its third column, and it is filled either way: with expansion off it reports what a
        /// pick would have pulled in, which is exactly the information needed to judge whether
        /// leaving it out is safe.
        /// </param>
        /// <param name="mode">The direction the graph is walked in.</param>
        /// <param name="expandDependencies">
        /// False to take the user's picks exactly as they are. Nothing is added, so a single
        /// package can be updated or removed on its own without its whole chain coming along.
        /// </param>
        internal static void Resolve(PackageEntry[] packages, bool[] userSelected, bool[] selected,
            string[] heldBy, EPackageMode mode, bool expandDependencies)
        {
            if (packages == null || userSelected == null || selected == null)
                return;

            Array.Copy(userSelected, selected, selected.Length);

            Dictionary<string, int> byName = BuildIndex(packages);

            if (expandDependencies)
                Expand(packages, selected, byName, mode);

            FillHeldBy(packages, selected, byName, heldBy, mode);
        }

        /// <summary>
        /// Orders the selected entries so nothing lands in a project that cannot compile.
        /// <para>
        /// Works outwards in layers: everything whose dependencies are already satisfied goes
        /// first, so an install starts at the leaves and climbs the chain and each package finds
        /// everything it needs already present. A removal is that same order reversed, so a
        /// package only goes once nothing that needs it is left.
        /// </para>
        /// </summary>
        /// <param name="packages">The registry entries, in the order the window lists them.</param>
        /// <param name="selected">The per-entry selection flags.</param>
        /// <param name="mode">The direction the run works in.</param>
        /// <returns>The indices to process, in the order they have to be processed in.</returns>
        internal static List<int> ResolveOrder(PackageEntry[] packages, bool[] selected, EPackageMode mode)
        {
            List<int> ordered = new();

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
                int next = TakeReady(remaining, packages, byName, placed, selected);

                // Nothing is ready, so whatever is left depends on itself in a loop. The registry
                // page reports that; here the run continues in list order rather than stalling.
                if (next < 0)
                    next = 0;

                int index = remaining[next];

                remaining.RemoveAt(next);
                placed.Add(index);
                ordered.Add(index);
            }

            if (mode == EPackageMode.Uninstall)
                ordered.Reverse();

            return ordered;
        }

        /// <summary>
        /// The installed packages that depend on the removal set without being part of it, whether
        /// directly or through another package. Those are the ones left behind in a project that
        /// no longer compiles, which is what the window warns about before a removal runs.
        /// </summary>
        /// <param name="packages">The registry entries, in the order the window lists them.</param>
        /// <param name="removing">Per entry, whether it is actually going to be removed.</param>
        /// <param name="statuses">The per-entry install status. Only installed entries can break.</param>
        /// <returns>The names of the packages left behind broken, or an empty list.</returns>
        internal static List<string> FindBroken(PackageEntry[] packages, bool[] removing, PackageStatus[] statuses)
        {
            List<string> broken = new();

            if (packages == null || removing == null || statuses == null)
                return broken;

            Dictionary<string, int> byName = BuildIndex(packages);
            bool[] doomed = new bool[packages.Length];

            Array.Copy(removing, doomed, doomed.Length);

            // A package broken by the removal breaks everything that in turn depends on it, so the
            // pass repeats until it marks nothing new.
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int i = 0; i < packages.Length; i++)
                {
                    if (doomed[i] || !statuses[i].IsInstalled)
                        continue;

                    if (!DependsOnDoomed(packages[i], byName, doomed))
                        continue;

                    doomed[i] = true;
                    changed = true;
                }
            }

            for (int i = 0; i < packages.Length; i++)
            {
                if (doomed[i] && !removing[i])
                    broken.Add(packages[i].Name);
            }

            return broken;
        }

        private static Dictionary<string, int> BuildIndex(PackageEntry[] packages)
        {
            Dictionary<string, int> byName = new();

            for (int i = 0; i < packages.Length; i++)
                byName[packages[i].Name] = i;

            return byName;
        }

        // A dependency can itself pull in another one, so the pass repeats until it adds nothing.
        private static void Expand(PackageEntry[] packages, bool[] selected,
            IReadOnlyDictionary<string, int> byName, EPackageMode mode)
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int i = 0; i < packages.Length; i++)
                {
                    foreach (string dependency in packages[i].DependsOn)
                    {
                        if (!byName.TryGetValue(dependency, out int index))
                            continue;

                        int holder = Holder(i, index, mode);
                        int held = Held(i, index, mode);

                        if (!selected[holder] || selected[held])
                            continue;

                        selected[held] = true;
                        changed = true;
                    }
                }
            }
        }

        // Every selected package claims the entries it drags in, so a row can name all of its
        // holders rather than only the first one that happened to reach it.
        private static void FillHeldBy(PackageEntry[] packages, bool[] selected,
            IReadOnlyDictionary<string, int> byName, string[] heldBy, EPackageMode mode)
        {
            if (heldBy == null)
                return;

            Array.Clear(heldBy, 0, heldBy.Length);

            for (int i = 0; i < packages.Length; i++)
            {
                foreach (string dependency in packages[i].DependsOn)
                {
                    if (!byName.TryGetValue(dependency, out int index) || index == i)
                        continue;

                    int holder = Holder(i, index, mode);
                    int held = Held(i, index, mode);

                    if (!selected[holder])
                        continue;

                    heldBy[held] = string.IsNullOrEmpty(heldBy[held])
                        ? packages[holder].Name
                        : heldBy[held] + NameSeparator + packages[holder].Name;
                }
            }
        }

        // Along the edge "entry needs dependency", the entry is what drags the dependency into an
        // install, and the dependency is what drags the entry into a removal.
        private static int Holder(int entry, int dependency, EPackageMode mode) => mode == EPackageMode.Install
            ? entry
            : dependency;

        private static int Held(int entry, int dependency, EPackageMode mode) => mode == EPackageMode.Install
            ? dependency
            : entry;

        private static bool DependsOnDoomed(PackageEntry entry, IReadOnlyDictionary<string, int> byName,
            bool[] doomed)
        {
            foreach (string dependency in entry.DependsOn)
            {
                if (byName.TryGetValue(dependency, out int index) && doomed[index])
                    return true;
            }

            return false;
        }

        // The first entry whose selected dependencies have all been placed. The registry is sorted
        // by name, so a whole layer comes out alphabetically before the next one starts.
        private static int TakeReady(IReadOnlyList<int> remaining, PackageEntry[] packages,
            IReadOnlyDictionary<string, int> byName, ICollection<int> placed, bool[] selected)
        {
            for (int i = 0; i < remaining.Count; i++)
            {
                if (IsReady(packages[remaining[i]], remaining[i], byName, placed, selected))
                    return i;
            }

            return -1;
        }

        // Only dependencies that are part of this run can hold an entry back. One outside it is
        // either already installed or was deliberately left out, and waiting for a package that is
        // never going to be placed would stall the walk and drop the rest into plain list order.
        private static bool IsReady(PackageEntry entry, int index, IReadOnlyDictionary<string, int> byName,
            ICollection<int> placed, bool[] selected)
        {
            foreach (string dependency in entry.DependsOn)
            {
                if (!byName.TryGetValue(dependency, out int dependencyIndex))
                    continue;

                if (dependencyIndex == index || !selected[dependencyIndex])
                    continue;

                if (!placed.Contains(dependencyIndex))
                    return false;
            }

            return true;
        }
    }
}