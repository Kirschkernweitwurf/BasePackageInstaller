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
    /// </summary>
    [FilePath("ProjectSettings/BasePackageRegistry.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class BasePackageRegistry : ScriptableSingleton<BasePackageRegistry>
    {
        /// <summary>The serialized name of the package list, for the settings provider to bind against.</summary>
        internal const string PackagesPropertyName = nameof(packages);

        [SerializeField] private bool seeded;
        [SerializeField] private List<PackageEntry> packages = new();

        /// <summary>The registered packages sorted alphabetically by name.</summary>
        internal PackageEntry[] SortedPackages
        {
            get
            {
                EnsureSeeded();

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
        /// <returns><c>true</c> when the registry changed and was saved.</returns>
        internal bool SyncWithDefaults()
        {
            EnsureSeeded();

            bool changed = ApplyLegacyRenames();

            foreach (PackageEntry defaultEntry in BasePackageDefaults.Create())
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

            if (changed)
                Save(true);

            return changed;
        }

        private static bool Matches(PackageEntry current, PackageEntry defaultEntry) => current.Url == defaultEntry.Url
            && current.DependsOn.SequenceEqual(defaultEntry.DependsOn);

        private void EnsureSeeded()
        {
            if (seeded)
                return;

            if (packages.Count == 0)
                packages.AddRange(BasePackageDefaults.Create());

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