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
        /// </summary>
        /// <returns><c>true</c> when the registry changed and was saved.</returns>
        internal bool SyncWithDefaults()
        {
            EnsureSeeded();

            bool changed = false;

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

        private static bool Matches(PackageEntry current, PackageEntry defaultEntry)
            => current.Url == defaultEntry.Url
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
    }
}