using System;
using System.Collections.Generic;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// Checks a registry for the mistakes that are easy to make by hand and impossible to see
    /// afterwards, because <see cref="PackageDependencyResolver"/> answers all of them by quietly
    /// doing nothing: a dependency naming an entry that does not exist, two entries sharing a name,
    /// an entry depending on itself, and a cycle.
    /// </summary>
    internal static class PackageRegistryValidator
    {
        private const string CycleFormat = "\"{0}\" and \"{1}\" depend on each other, so neither can be "
            + "installed first.";
        private const string DuplicateFormat = "\"{0}\" is listed more than once. Entries are matched by name, so "
            + "only one of them is ever used.";
        private const string MissingNameMessage = "An entry has no name. It cannot be referred to as a dependency.";
        private const string MissingUrlFormat = "\"{0}\" has no URL.";
        private const string SelfFormat = "\"{0}\" lists itself as a dependency.";
        private const string UnknownFormat = "\"{0}\" depends on \"{1}\", which is not in this list. The "
            + "dependency is ignored.";

        /// <summary>
        /// Validates the given entries.
        /// </summary>
        /// <param name="packages">The registry entries to check.</param>
        /// <returns>One message per problem found, or an empty array when the registry is sound.</returns>
        internal static string[] Validate(PackageEntry[] packages)
        {
            List<string> problems = new();

            if (packages == null || packages.Length == 0)
                return problems.ToArray();

            HashSet<string> names = new();
            Dictionary<string, PackageEntry> byName = new();

            foreach (PackageEntry entry in packages)
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    if (!problems.Contains(MissingNameMessage))
                        problems.Add(MissingNameMessage);

                    continue;
                }

                if (!names.Add(entry.Name))
                    problems.Add(string.Format(DuplicateFormat, entry.Name));

                byName[entry.Name] = entry;

                if (string.IsNullOrWhiteSpace(entry.Url))
                    problems.Add(string.Format(MissingUrlFormat, entry.Name));
            }

            foreach (PackageEntry entry in packages)
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                    continue;

                foreach (string dependency in entry.DependsOn)
                {
                    if (string.Equals(dependency, entry.Name, StringComparison.Ordinal))
                    {
                        problems.Add(string.Format(SelfFormat, entry.Name));
                        continue;
                    }

                    if (!byName.ContainsKey(dependency))
                        problems.Add(string.Format(UnknownFormat, entry.Name, dependency));
                }
            }

            AddCycles(byName, problems);

            return problems.ToArray();
        }

        // Reports the pair that closes each cycle rather than the whole loop, which is enough to
        // find the edge that has to go and keeps the message readable.
        private static void AddCycles(IReadOnlyDictionary<string, PackageEntry> byName, ICollection<string> problems)
        {
            HashSet<string> settled = new();
            HashSet<string> reported = new();

            foreach (string name in byName.Keys)
            {
                if (!settled.Contains(name))
                    Walk(name, byName, settled, new HashSet<string>(), reported, problems);
            }
        }

        private static void Walk(string name, IReadOnlyDictionary<string, PackageEntry> byName,
            ISet<string> settled, ISet<string> path, ISet<string> reported, ICollection<string> problems)
        {
            if (!path.Add(name))
                return;

            foreach (string dependency in byName[name].DependsOn)
            {
                // A self dependency is already reported on its own, and following it here would
                // report the same entry as a two-package cycle with itself.
                if (!byName.ContainsKey(dependency)
                    || string.Equals(dependency, name, StringComparison.Ordinal))
                    continue;

                if (path.Contains(dependency))
                {
                    string key = string.CompareOrdinal(name, dependency) < 0
                        ? name + dependency
                        : dependency + name;

                    if (reported.Add(key))
                        problems.Add(string.Format(CycleFormat, name, dependency));

                    continue;
                }

                if (!settled.Contains(dependency))
                    Walk(dependency, byName, settled, path, reported, problems);
            }

            path.Remove(name);
            settled.Add(name);
        }
    }
}