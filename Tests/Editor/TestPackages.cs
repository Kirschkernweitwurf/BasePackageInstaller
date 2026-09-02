using System.Collections.Generic;
using Base.PackageInstaller.Data;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Builds the small registries the resolver and validator tests run against, and translates the
    /// index-based arrays they work with back into names, so the assertions read as package names
    /// rather than as positions in an array.
    /// </summary>
    internal static class TestPackages
    {
        private const string UrlPrefix = "https://example.com/";
        private const string Version = "1.0.0";

        /// <summary>Creates an entry with a URL generated from its name.</summary>
        /// <param name="name">The entry name.</param>
        /// <param name="dependsOn">The names of the entries it directly depends on.</param>
        /// <returns>The entry.</returns>
        internal static PackageEntry Entry(string name, params string[] dependsOn) => new(name, Url(name), dependsOn);

        /// <summary>The URL an entry of the given name is created with.</summary>
        /// <param name="name">The entry name.</param>
        /// <returns>The generated URL.</returns>
        internal static string Url(string name) => UrlPrefix + name;

        /// <summary>Creates a flag array with the named entries set and everything else clear.</summary>
        /// <param name="packages">The registry the flags line up with.</param>
        /// <param name="names">The names to set.</param>
        /// <returns>The flags.</returns>
        internal static bool[] Flags(PackageEntry[] packages, params string[] names)
        {
            bool[] flags = new bool[packages.Length];

            foreach (string name in names)
                flags[IndexOf(packages, name)] = true;

            return flags;
        }

        /// <summary>Creates a flag array with every entry set.</summary>
        /// <param name="packages">The registry the flags line up with.</param>
        /// <returns>The flags.</returns>
        internal static bool[] AllFlags(PackageEntry[] packages)
        {
            bool[] flags = new bool[packages.Length];

            for (int i = 0; i < flags.Length; i++)
                flags[i] = true;

            return flags;
        }

        /// <summary>Creates install statuses with the named entries marked installed.</summary>
        /// <param name="packages">The registry the statuses line up with.</param>
        /// <param name="installed">The names to mark installed.</param>
        /// <returns>The statuses.</returns>
        internal static PackageStatus[] Statuses(PackageEntry[] packages, params string[] installed) =>
            Build(packages, Flags(packages, installed));

        /// <summary>Creates install statuses with every entry marked installed.</summary>
        /// <param name="packages">The registry the statuses line up with.</param>
        /// <returns>The statuses.</returns>
        internal static PackageStatus[] AllStatuses(PackageEntry[] packages) => Build(packages, AllFlags(packages));

        /// <summary>Runs the resolver without collecting the holders.</summary>
        /// <param name="packages">The registry entries.</param>
        /// <param name="userSelected">The rows the user ticked.</param>
        /// <param name="mode">The direction the graph is walked in.</param>
        /// <param name="expandDependencies">Whether the picks pull anything in.</param>
        /// <returns>The effective selection.</returns>
        internal static bool[] Resolve(PackageEntry[] packages, bool[] userSelected, EPackageMode mode,
            bool expandDependencies) => Resolve(packages, userSelected, mode, expandDependencies, null);

        /// <summary>Runs the resolver and fills the given holder array.</summary>
        /// <param name="packages">The registry entries.</param>
        /// <param name="userSelected">The rows the user ticked.</param>
        /// <param name="mode">The direction the graph is walked in.</param>
        /// <param name="expandDependencies">Whether the picks pull anything in.</param>
        /// <param name="heldBy">Filled with the names holding each row, or null to skip that.</param>
        /// <returns>The effective selection.</returns>
        internal static bool[] Resolve(PackageEntry[] packages, bool[] userSelected, EPackageMode mode,
            bool expandDependencies, string[] heldBy)
        {
            bool[] selected = new bool[packages.Length];

            PackageDependencyResolver.Resolve(packages, userSelected, selected, heldBy, mode, expandDependencies);

            return selected;
        }

        /// <summary>The names of the flagged entries, in registry order.</summary>
        /// <param name="packages">The registry entries.</param>
        /// <param name="flags">The flags to read.</param>
        /// <returns>The names.</returns>
        internal static string[] Names(PackageEntry[] packages, bool[] flags)
        {
            List<string> names = new();

            for (int i = 0; i < packages.Length; i++)
            {
                if (flags[i])
                    names.Add(packages[i].Name);
            }

            return names.ToArray();
        }

        /// <summary>The names of the entries at the given indices, in the order they are given in.</summary>
        /// <param name="packages">The registry entries.</param>
        /// <param name="indices">The indices to read.</param>
        /// <returns>The names.</returns>
        internal static string[] Names(PackageEntry[] packages, IEnumerable<int> indices)
        {
            List<string> names = new();

            foreach (int index in indices)
                names.Add(packages[index].Name);

            return names.ToArray();
        }

        /// <summary>The entries holding the named row, or an empty string when nothing holds it.</summary>
        /// <param name="packages">The registry entries.</param>
        /// <param name="heldBy">The holder array the resolver filled.</param>
        /// <param name="name">The row to read.</param>
        /// <returns>The holder names, joined the way the resolver joins them.</returns>
        internal static string HeldBy(PackageEntry[] packages, string[] heldBy, string name) =>
            heldBy[IndexOf(packages, name)] ?? string.Empty;

        private static PackageStatus[] Build(PackageEntry[] packages, bool[] installed)
        {
            PackageStatus[] statuses = new PackageStatus[packages.Length];

            for (int i = 0; i < packages.Length; i++)
            {
                statuses[i] = installed[i]
                    ? new PackageStatus(true, packages[i].Name, Version)
                    : default;
            }

            return statuses;
        }

        private static int IndexOf(PackageEntry[] packages, string name)
        {
            for (int i = 0; i < packages.Length; i++)
            {
                if (packages[i].Name == name)
                    return i;
            }

            throw new KeyNotFoundException($"The test registry has no entry named {name}.");
        }
    }
}