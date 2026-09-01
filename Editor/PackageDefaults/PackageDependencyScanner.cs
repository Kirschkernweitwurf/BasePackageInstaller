using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Derives the package dependency graph from what is on disk, so the list the installer ships can
    /// be generated from the single source of truth instead of maintained by hand.
    /// <para>
    /// Reads every folder under the packages root that holds a <c>package.json</c>, maps each assembly
    /// to its owning package, resolves the references between them, and removes the edges another edge
    /// already implies so a package lists only what it needs directly.
    /// </para>
    /// <para>
    /// Code is only half of it. A prefab holding a component from another package needs that package
    /// just as hard as a compiled reference does, and it fails worse: the asmdef graph says nothing, the
    /// installation succeeds, and the prefab opens with missing scripts that are stripped the moment it is
    /// saved. So every serialized asset in the package is read as well and the GUIDs in it are mapped
    /// back to the package owning the file each one points at.
    /// </para>
    /// <para>
    /// The two graphs do not have to agree, and where they disagree the asset one wins by being the
    /// one that breaks silently. It can also introduce a cycle the compiler cannot have, because two
    /// packages can hold prefabs pointing at each other while their code only points one way. That is
    /// left in rather than resolved here: <c>PackageRegistryValidator</c> reports it, and a cycle in
    /// this graph is a real thing about the packages, not a fault in the scan.
    /// </para>
    /// </summary>
    internal static class PackageDependencyScanner
    {
        private const string AllFilesSearchPattern = "*";
        private const string AsmdefSearchPattern = "*.asmdef";
        private const string GuidPrefix = "GUID:";
        private const string ManifestFileName = "package.json";
        private const string MetaExtension = ".meta";
        private const string TestAssemblySuffix = ".Tests";

        /// <summary>
        /// Serialized file kinds that can hold a reference to another package. All of them are Unity's
        /// YAML, so one reference pattern reads every one of them.
        /// </summary>
        private static readonly HashSet<string> SerializedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".asset",
            ".controller",
            ".mat",
            ".prefab",
            ".unity"
        };

        private static readonly Regex GuidPattern = new(@"guid:\s*(\w+)", RegexOptions.Compiled);

        /// <summary>Matches a GUID inside a serialized reference, which always carries a fileID with it.</summary>
        private static readonly Regex ReferencePattern = new(@"fileID:\s*-?\d+,\s*guid:\s*([0-9a-fA-F]{32})",
            RegexOptions.Compiled);

        /// <summary>
        /// Scans the given packages root and returns one entry per package, sorted by display name.
        /// </summary>
        /// <param name="packagesRoot">The absolute path of the folder holding the package folders.</param>
        /// <returns>The scanned packages, or an empty array when the root holds none.</returns>
        internal static PackageDependencyInfo[] Scan(string packagesRoot)
        {
            if (!Directory.Exists(packagesRoot))
            {
                Debug.LogWarning($"{nameof(PackageDependencyScanner)}: packages root not found: {packagesRoot}");

                return Array.Empty<PackageDependencyInfo>();
            }

            List<string> folders = new();

            foreach (string folder in Directory.GetDirectories(packagesRoot))
            {
                if (File.Exists(Path.Combine(folder, ManifestFileName)))
                    folders.Add(folder);
            }

            if (folders.Count == 0)
            {
                Debug.LogWarning($"{nameof(PackageDependencyScanner)}: no package folders under {packagesRoot}.");

                return Array.Empty<PackageDependencyInfo>();
            }

            Dictionary<string, string> assemblyToPackage = new();
            Dictionary<string, string> guidToAssembly = new();
            Dictionary<string, string> guidToPackage = new();
            List<KeyValuePair<string, AsmdefContent>> owned = new();

            foreach (string folder in folders)
                Collect(folder, assemblyToPackage, guidToAssembly, guidToPackage, owned);

            Dictionary<string, HashSet<string>> edges = BuildEdges(owned, assemblyToPackage, guidToAssembly);

            // Every GUID in the packages is known by now, which is what lets an asset reference be
            // resolved to a package instead of being read as an unknown one and dropped.
            foreach (string folder in folders)
                CollectAssetEdges(folder, guidToPackage, edges);

            List<PackageDependencyInfo> result = new();

            foreach (string folder in folders)
            {
                string package = Path.GetFileName(folder);
                edges.TryGetValue(package, out HashSet<string> direct);

                result.Add(new PackageDependencyInfo(package, PackageDisplayNames.Resolve(package),
                    Reduce(direct, edges)));
            }

            result.Sort(comparison: static (a, b)
                => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));

            return result.ToArray();
        }

        private static void Collect(string folder, IDictionary<string, string> assemblyToPackage,
            IDictionary<string, string> guidToAssembly, IDictionary<string, string> guidToPackage,
            ICollection<KeyValuePair<string, AsmdefContent>> owned)
        {
            string package = Path.GetFileName(folder);

            IndexGuids(folder, package, guidToPackage);

            foreach (string path in Directory.GetFiles(folder, AsmdefSearchPattern, SearchOption.AllDirectories))
            {
                AsmdefContent content = JsonUtility.FromJson<AsmdefContent>(File.ReadAllText(path));

                if (content == null || string.IsNullOrEmpty(content.Name))
                    continue;

                assemblyToPackage[content.Name] = package;

                string guid = ReadGuid(path + MetaExtension);

                if (!string.IsNullOrEmpty(guid))
                    guidToAssembly[guid] = content.Name;

                // An assembly behind a define constraint is optional by design and a test assembly
                // never ships, so neither may turn into a hard dependency of the owning package.
                if (content.IsOptional || content.Name.EndsWith(TestAssemblySuffix, StringComparison.Ordinal))
                    continue;

                owned.Add(new KeyValuePair<string, AsmdefContent>(package, content));
            }
        }

        /// <summary>
        /// Records which package owns every file in it, keyed by the GUID from its meta. A serialized
        /// reference names nothing but a GUID, so this is the only thing that can turn one back into a
        /// package.
        /// </summary>
        private static void IndexGuids(string folder, string package, IDictionary<string, string> guidToPackage)
        {
            foreach (string path in Directory.GetFiles(folder, AllFilesSearchPattern, SearchOption.AllDirectories))
            {
                if (!path.EndsWith(MetaExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                string guid = ReadGuid(path);

                if (!string.IsNullOrEmpty(guid))
                    guidToPackage[guid] = package;
            }
        }

        /// <summary>
        /// Adds an edge for every package a serialized file in this one points at. A GUID that resolves
        /// to nothing is left alone: it belongs to Unity, to a third party package, or to the consuming
        /// project, and none of those are edges this graph can express.
        /// </summary>
        private static void CollectAssetEdges(string folder, IReadOnlyDictionary<string, string> guidToPackage,
            IDictionary<string, HashSet<string>> edges)
        {
            string package = Path.GetFileName(folder);

            foreach (string path in Directory.GetFiles(folder, AllFilesSearchPattern, SearchOption.AllDirectories))
            {
                if (!SerializedExtensions.Contains(Path.GetExtension(path)))
                    continue;

                foreach (Match match in ReferencePattern.Matches(File.ReadAllText(path)))
                {
                    if (!guidToPackage.TryGetValue(match.Groups[1].Value, out string target) || target == package)
                        continue;

                    if (!edges.TryGetValue(package, out HashSet<string> set))
                    {
                        set = new HashSet<string>();
                        edges[package] = set;
                    }

                    set.Add(target);
                }
            }
        }

        private static Dictionary<string, HashSet<string>> BuildEdges(
            IEnumerable<KeyValuePair<string, AsmdefContent>> owned,
            IReadOnlyDictionary<string, string> assemblyToPackage,
            IReadOnlyDictionary<string, string> guidToAssembly)
        {
            Dictionary<string, HashSet<string>> edges = new();

            foreach (KeyValuePair<string, AsmdefContent> pair in owned)
            {
                foreach (string reference in pair.Value.References)
                {
                    string assembly = Resolve(reference, guidToAssembly);

                    if (assembly == null || !assemblyToPackage.TryGetValue(assembly, out string target))
                        continue;

                    if (target == pair.Key)
                        continue;

                    if (!edges.TryGetValue(pair.Key, out HashSet<string> set))
                    {
                        set = new HashSet<string>();
                        edges[pair.Key] = set;
                    }

                    set.Add(target);
                }
            }

            return edges;
        }

        // An edge is redundant when another edge of the same package already reaches its target.
        private static string[] Reduce(HashSet<string> direct, IReadOnlyDictionary<string, HashSet<string>> edges)
        {
            if (direct == null)
                return Array.Empty<string>();

            List<string> kept = new();

            foreach (string candidate in direct)
            {
                if (!IsImplied(candidate, direct, edges))
                    kept.Add(candidate);
            }

            kept.Sort(StringComparer.Ordinal);

            return kept.ToArray();
        }

        private static bool IsImplied(string candidate, IEnumerable<string> direct,
            IReadOnlyDictionary<string, HashSet<string>> edges)
        {
            foreach (string other in direct)
            {
                if (other == candidate)
                    continue;

                if (Reachable(other, candidate, edges, new HashSet<string>()))
                    return true;
            }

            return false;
        }

        private static bool Reachable(string from, string target,
            IReadOnlyDictionary<string, HashSet<string>> edges, ISet<string> visited)
        {
            if (!visited.Add(from) || !edges.TryGetValue(from, out HashSet<string> next))
                return false;

            if (next.Contains(target))
                return true;

            foreach (string step in next)
            {
                if (Reachable(step, target, edges, visited))
                    return true;
            }

            return false;
        }

        private static string Resolve(string reference, IReadOnlyDictionary<string, string> guidToAssembly)
        {
            if (string.IsNullOrEmpty(reference))
                return null;

            if (!reference.StartsWith(GuidPrefix, StringComparison.Ordinal))
                return reference;

            return guidToAssembly.GetValueOrDefault(reference[GuidPrefix.Length..]);
        }

        private static string ReadGuid(string metaPath)
        {
            if (!File.Exists(metaPath))
                return string.Empty;

            Match match = GuidPattern.Match(File.ReadAllText(metaPath));

            return match.Success
                ? match.Groups[1].Value
                : string.Empty;
        }
    }
}