using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// Registry of base packages, persisted per project in
    /// <c>ProjectSettings/BasePackageRegistry.asset</c> so it can be version controlled.
    /// <para>
    /// Seeded with <see cref="BasePackageDefaults"/> on first creation; consumers can then
    /// add, remove or edit entries via the Git Packages page in the project settings.
    /// </para>
    /// <para>
    /// Kept in step with the defaults on its own: whenever the defaults the installer ships differ
    /// from the ones the registry last synced with, the next read pulls them in. A project that was
    /// seeded long ago and never refreshed by hand would otherwise keep listing the old set forever.
    /// </para>
    /// </summary>
    [FilePath("ProjectSettings/BasePackageRegistry.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class BasePackageRegistry : ScriptableSingleton<BasePackageRegistry>
    {
        private const string EntrySeparator = "\n";
        private const char FieldSeparator = '|';

        /// <summary>The serialized name of the package list, for the settings provider to bind against.</summary>
        internal const string PackagesPropertyName = nameof(packages);

        [SerializeField] private bool seeded;
        [SerializeField] private string syncedDefaults;
        [SerializeField] private List<PackageEntry> packages = new();

        /// <summary>The registered packages sorted alphabetically by name.</summary>
        internal PackageEntry[] SortedPackages
        {
            get
            {
                EnsureCurrent();

                PackageEntry[] sorted = packages.ToArray();
                Array.Sort(sorted,
                    comparison: static (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

                return sorted;
            }
        }

        /// <summary>Writes the registry back to disk after edits.</summary>
        internal void Persist() => Save(true);

        /// <summary>
        /// Re-applies <see cref="BasePackageDefaults"/> onto the registry so newly added or
        /// changed defaults appear without discarding project-specific entries. Matches by
        /// name: adds any missing default and replaces an existing default whose URL or
        /// dependencies changed.
        /// <para>
        /// Names stored under a name the installer has since renamed are carried forward first,
        /// through <see cref="LegacyPackageNames"/>, so the rename does not read as a new package.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> when the package list changed.</returns>
        internal bool SyncWithDefaults()
        {
            EnsureSeeded();

            PackageEntry[] defaults = BasePackageDefaults.Create();
            string fingerprint = Fingerprint(defaults);
            bool isNewFingerprint = syncedDefaults != fingerprint;
            bool changed = ApplyLegacyRenames();

            syncedDefaults = fingerprint;

            foreach (PackageEntry defaultEntry in defaults)
            {
                int index = packages.FindIndex(entry => entry.Name == defaultEntry.Name);

                if (index < 0)
                {
                    packages.Add(defaultEntry);
                    changed = true;
                    continue;
                }

                if (Matches(packages[index], defaultEntry))
                    continue;

                packages[index] = defaultEntry;
                changed = true;
            }

            if (changed
                || isNewFingerprint)
                Save(true);

            return changed;
        }

        /// <summary>
        /// Condenses a package list into a short value that changes whenever any name, URL or
        /// dependency in it does, so the registry can tell whether the shipped defaults moved on.
        /// </summary>
        /// <param name="entries">The entries to condense, in their shipped order.</param>
        /// <returns>The fingerprint of the list.</returns>
        internal static string Fingerprint(IEnumerable<PackageEntry> entries)
        {
            IEnumerable<string> lines = entries.Select(static entry => string.Join(FieldSeparator,
                entry.Name, entry.Url, string.Join(FieldSeparator, entry.DependsOn)));

            return Hash128.Compute(string.Join(EntrySeparator, lines)).ToString();
        }

        private static bool Matches(PackageEntry current, PackageEntry defaultEntry) => current.Url == defaultEntry.Url
            && current.DependsOn.SequenceEqual(defaultEntry.DependsOn);

        // Only syncs when the shipped defaults changed since the last sync, not on every read. A default
        // the project removed on purpose then stays removed until there is something new to bring in.
        private void EnsureCurrent()
        {
            EnsureSeeded();

            if (syncedDefaults == Fingerprint(BasePackageDefaults.Create()))
                return;

            SyncWithDefaults();
        }

        private void EnsureSeeded()
        {
            if (seeded)
                return;

            if (packages.Count == 0)
            {
                PackageEntry[] defaults = BasePackageDefaults.Create();

                packages.AddRange(defaults);
                syncedDefaults = Fingerprint(defaults);
            }

            seeded = true;
            Save(true);
        }

        /// <summary>
        /// Rewrites every entry and every dependency still stored under a renamed name, so a
        /// renamed default updates the row a project already holds instead of adding a second one.
        /// </summary>
        /// <returns><c>true</c> when at least one entry was rewritten or dropped.</returns>
        private bool ApplyLegacyRenames()
        {
            bool changed = false;

            for (int i = 0; i < packages.Count; i++)
            {
                PackageEntry entry = packages[i];
                string name = LegacyPackageNames.Resolve(entry.Name);
                string[] dependsOn = entry.DependsOn;
                string[] renamed = new string[dependsOn.Length];

                for (int j = 0; j < dependsOn.Length; j++)
                    renamed[j] = LegacyPackageNames.Resolve(dependsOn[j]);

                if (name == entry.Name
                    && renamed.SequenceEqual(dependsOn))
                    continue;

                packages[i] = new PackageEntry(name, entry.Url, renamed);
                changed = true;
            }

            bool removed = RemoveDuplicateEntries();

            return changed || removed;
        }

        /// <summary>
        /// Drops entries a rename left sharing a name with an earlier one, keeping the first. Only
        /// reachable in a project that had already added an entry under the new name by hand.
        /// </summary>
        /// <returns><c>true</c> when at least one entry was dropped.</returns>
        private bool RemoveDuplicateEntries()
        {
            HashSet<string> seen = new();
            bool changed = false;

            for (int i = 0; i < packages.Count; i++)
            {
                if (seen.Add(packages[i].Name))
                    continue;

                packages.RemoveAt(i);
                i--;
                changed = true;
            }

            return changed;
        }
    }
}