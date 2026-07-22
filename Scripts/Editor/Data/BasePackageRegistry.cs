#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Base.PackageInstaller.Data
{
    /// <summary>
    /// Editor-only registry of base packages, persisted per project in
    /// <c>ProjectSettings/BasePackageRegistry.asset</c> so it can be version controlled.
    /// <para>
    /// Seeded with <see cref="BasePackageDefaults"/> on first creation; consumers can then
    /// add, remove or edit entries via Project Settings → "Base Packages".
    /// </para>
    /// </summary>
    [FilePath("ProjectSettings/BasePackageRegistry.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class BasePackageRegistry : ScriptableSingleton<BasePackageRegistry>
    {
        [SerializeField] private bool seeded;
        [SerializeField] private List<PackageEntry> packages = new();

        /// <summary>The registered packages sorted alphabetically by name.</summary>
        public PackageEntry[] SortedPackages
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
        public void Persist() => Save(true);

        /// <summary>
        /// Re-applies <see cref="BasePackageDefaults"/> onto the registry so newly added or
        /// changed defaults appear without discarding project-specific entries. Matches by
        /// name: adds any missing default and updates the URL of an existing default that changed.
        /// </summary>
        /// <returns><c>true</c> when the registry changed and was saved.</returns>
        public bool SyncWithDefaults()
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

                if (packages[index].Url == defaultEntry.Url)
                    continue;

                packages[index] = defaultEntry;
                changed = true;
            }

            if (changed)
                Save(true);

            return changed;
        }

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
#endif