using System;
using System.Collections.Generic;
using Base.PackageInstaller.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Checks the shipped defaults against the packages this project actually holds.
    /// <para>
    /// The defaults are generated from a checkout of the packages repository, so everything that goes
    /// wrong before the file is written goes wrong silently: a stale checkout, a root pointing at the
    /// wrong folder, a hand edit, or a fault in the scanner itself. All four produce a file that looks
    /// generated and installs a project that does not compile, which is how the missing Localization
    /// edge and the stale Settings edge both got out.
    /// </para>
    /// <para>
    /// This reads the installed packages instead, so it shares no code with the generator and fails on
    /// exactly the thing that matters: a package that reaches into another one the declared graph never
    /// pulls in. Both halves of reaching are covered. Assembly definitions are one half and break loudly
    /// at compile time; the serialized assets are the other and break quietly, since a prefab holding a
    /// component from a package that was never installed opens with a missing script and loses it the
    /// moment it is saved.
    /// </para>
    /// </summary>
    public sealed class InstalledPackageGraphTests
    {
        private const string AsmdefExtension = ".asmdef";
        private const string AsmdefFilter = "t:AssemblyDefinitionAsset";
        private const string AssetFilter = "t:Object";
        private const string BasePackagePrefix = "com.baseprojectpackages.";
        private const char FolderSeparator = '/';
        private const string GuidPrefix = "GUID:";

        /// <summary>
        /// The file kinds that can hold a reference to another package. All of them are Unity's own
        /// serialization, and everything else in a package is a leaf that points at nothing.
        /// </summary>
        private static readonly HashSet<string> SerializedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".asset",
            ".controller",
            ".mat",
            ".mixer",
            ".prefab",
            ".unity"
        };

        private readonly Dictionary<string, string> _assemblyByGuid = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _packageByAssembly = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _rootByPackage = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> _declaredEdges = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PackageEntry> _entryByPackage = new(StringComparer.Ordinal);
        private readonly HashSet<string> _shippingAssemblies = new(StringComparer.Ordinal);
        private readonly List<RawAsmdef> _shipping = new();

        private string _ownPackage;

        /// <summary>
        /// Reads the installed packages and their assembly definitions once, and maps the shipped
        /// entries onto the package names both of them belong to.
        /// </summary>
        [OneTimeSetUp]
        public void LoadProject()
        {
            ResolveOwnPackage();
            CollectPackages();
            CollectAssemblies();
            MapEntries();
        }

        /// <summary>
        /// Every assertion below is about packages the project holds, so a project holding none would
        /// pass all of them without checking anything. That is reported rather than left quiet.
        /// </summary>
        [Test]
        public void TheProjectHoldsBasePackages() => Assert.That(_rootByPackage, Is.Not.Empty,
            "no base package is installed here, so nothing below means anything");

        /// <summary>
        /// A package the project holds but the list does not name cannot be installed or updated through
        /// the window at all. A renamed folder shows up here first, since the entry still points at the
        /// old one.
        /// </summary>
        [Test]
        public void EveryInstalledPackageHasAnEntry()
        {
            List<string> missing = new();

            foreach (string package in _rootByPackage.Keys)
            {
                if (!_entryByPackage.ContainsKey(package))
                    missing.Add(package);
            }

            Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing));
        }

        /// <summary>
        /// Entries are matched by name and installed by URL, so two entries pointing at one folder means
        /// one of them installs the wrong package under the right name.
        /// </summary>
        [Test]
        public void NoTwoEntriesPointAtTheSameFolder()
        {
            HashSet<string> seen = new(StringComparer.Ordinal);
            List<string> duplicates = new();

            foreach (PackageEntry entry in BasePackageDefaults.Create())
            {
                if (!seen.Add(PackageOfEntry(entry)))
                    duplicates.Add(entry.Name);
            }

            Assert.That(duplicates, Is.Empty, string.Join(Environment.NewLine, duplicates));
        }

        /// <summary>
        /// Installing a package has to bring everything its assemblies reference, which means the
        /// declared graph, walked all the way through, has to reach every package the real references
        /// land in. A gap here is a project that installs and then fails to compile.
        /// </summary>
        [Test]
        public void EveryAssemblyReferenceIsReachableThroughTheDeclaredGraph()
        {
            List<string> failures = new();

            foreach (RawAsmdef data in _shipping)
                InspectAssembly(data, failures);

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// The half no compiler checks. A prefab holding a component from another package needs that
        /// package just as hard as a compiled reference does, and it fails worse: the installation
        /// succeeds, the prefab opens with a missing script, and saving it drops the component for good.
        /// This is also the only check an asset-only package is visible to at all.
        /// </summary>
        [Test]
        public void EveryAssetReferenceIsReachableThroughTheDeclaredGraph()
        {
            List<string> failures = new();

            foreach (KeyValuePair<string, string> package in _rootByPackage)
                InspectAssets(package.Key, package.Value, failures);

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        private static RawAsmdef Read(string path)
        {
            AssemblyDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);

            if (asset == null)
                return null;

            return JsonUtility.FromJson<RawAsmdef>(asset.text);
        }

        private static string PackageOf(string path)
        {
            PackageInfo info = PackageInfo.FindForAssetPath(path);

            if (info == null)
                return null;

            return info.name;
        }

        private static bool IsBasePackage(string package) => !string.IsNullOrEmpty(package)
            && package.StartsWith(BasePackagePrefix, StringComparison.Ordinal);

        private static bool IsSerialized(string path)
        {
            int dot = path.LastIndexOf('.');

            return dot >= 0 && SerializedExtensions.Contains(path[dot..]);
        }

        /// <summary>
        /// An assembly ships when nothing gates it. One constraint covers both cases that must not
        /// count: an optional bridge assembly is absent unless the package it bridges to is there, and a
        /// test assembly never enters a build, so neither may turn into a hard dependency.
        /// </summary>
        private static bool IsShipping(RawAsmdef data)
            => data.defineConstraints == null || data.defineConstraints.Length == 0;

        /// <summary>
        /// Derives the installed package name from the folder an entry's URL ends in. The URL is the
        /// only thing tying an entry to a folder, so reading it back is what catches the two drifting
        /// apart.
        /// </summary>
        private static string PackageOfEntry(PackageEntry entry)
        {
            string url = entry.Url;

            if (string.IsNullOrEmpty(url))
                return string.Empty;

            int start = url.LastIndexOf(FolderSeparator) + 1;

            return BasePackagePrefix + url[start..].ToLowerInvariant();
        }

        /// <summary>
        /// Whether a package is one the shipped list is supposed to name. Every base package is, except
        /// the one this test ships in: the installer is what installs the others and is never installed
        /// by itself, so it has no entry and is not supposed to have one.
        /// </summary>
        /// <param name="package">The package name to test.</param>
        /// <returns>True when the package belongs in the defaults.</returns>
        private bool IsInScope(string package) => IsBasePackage(package)
            && !string.Equals(package, _ownPackage, StringComparison.Ordinal);

        /// <summary>
        /// Reads the package this test assembly ships in, so it can be left out of everything below.
        /// </summary>
        private void ResolveOwnPackage()
        {
            PackageInfo info = PackageInfo.FindForAssembly(typeof(InstalledPackageGraphTests).Assembly);

            _ownPackage = info == null
                ? string.Empty
                : info.name;
        }

        /// <summary>
        /// Records every installed base package and the folder it sits in. Read from the package list
        /// rather than from the assembly definitions, so a package that ships only assets is seen too.
        /// </summary>
        private void CollectPackages()
        {
            foreach (PackageInfo info in PackageInfo.GetAllRegisteredPackages())
            {
                if (IsInScope(info.name))
                    _rootByPackage[info.name] = info.assetPath;
            }
        }

        private void CollectAssemblies()
        {
            List<RawAsmdef> all = new();

            foreach (string guid in AssetDatabase.FindAssets(AsmdefFilter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.EndsWith(AsmdefExtension, StringComparison.Ordinal))
                    continue;

                RawAsmdef data = Read(path);
                string package = PackageOf(path);

                if (data == null || string.IsNullOrEmpty(data.name) || !IsInScope(package))
                    continue;

                all.Add(data);

                _assemblyByGuid[guid] = data.name;
                _packageByAssembly[data.name] = package;
            }

            foreach (RawAsmdef data in all)
            {
                if (!IsShipping(data))
                    continue;

                _shipping.Add(data);
                _shippingAssemblies.Add(data.name);
            }
        }

        private void MapEntries()
        {
            PackageEntry[] entries = BasePackageDefaults.Create();
            Dictionary<string, string> packageByEntryName = new(StringComparer.Ordinal);

            foreach (PackageEntry entry in entries)
            {
                string package = PackageOfEntry(entry);

                packageByEntryName[entry.Name] = package;
                _entryByPackage[package] = entry;
            }

            foreach (PackageEntry entry in entries)
            {
                HashSet<string> targets = new(StringComparer.Ordinal);

                foreach (string dependency in entry.DependsOn)
                {
                    if (packageByEntryName.TryGetValue(dependency, out string target))
                        targets.Add(target);
                }

                _declaredEdges[PackageOfEntry(entry)] = targets;
            }
        }

        private string ResolveReference(string token)
        {
            if (string.IsNullOrEmpty(token)
                || !token.StartsWith(GuidPrefix, StringComparison.Ordinal))
                return token;

            return _assemblyByGuid.GetValueOrDefault(token[GuidPrefix.Length..]);
        }

        /// <summary>
        /// Walks the declared graph from one package. The walk is guarded against revisiting, because a
        /// cycle in the list is a thing the validator reports rather than a thing that may hang here.
        /// </summary>
        private HashSet<string> Closure(string package)
        {
            HashSet<string> reached = new(StringComparer.Ordinal) { package };
            Stack<string> pending = new(reached);

            while (pending.Count > 0)
            {
                if (!_declaredEdges.TryGetValue(pending.Pop(), out HashSet<string> targets))
                    continue;

                foreach (string target in targets)
                {
                    if (reached.Add(target))
                        pending.Push(target);
                }
            }

            return reached;
        }

        private void InspectAssembly(RawAsmdef data, ICollection<string> failures)
        {
            if (data.references == null)
                return;

            string from = _packageByAssembly[data.name];

            if (!_entryByPackage.ContainsKey(from))
                return;

            HashSet<string> reachable = Closure(from);

            foreach (string token in data.references)
            {
                string assembly = ResolveReference(token);

                if (string.IsNullOrEmpty(assembly) || !_shippingAssemblies.Contains(assembly))
                    continue;

                string target = _packageByAssembly[assembly];

                if (string.Equals(target, from, StringComparison.Ordinal) || reachable.Contains(target))
                    continue;

                failures.Add($"{data.name} references {assembly} from {target}, which installing "
                    + $"{_entryByPackage[from].Name} never brings in. Add the dependency to the entry, or "
                    + "drop the reference.");
            }
        }

        private void InspectAssets(string from, string root, ICollection<string> failures)
        {
            if (string.IsNullOrEmpty(root) || !_entryByPackage.ContainsKey(from))
                return;

            HashSet<string> reachable = Closure(from);

            foreach (string guid in AssetDatabase.FindAssets(AssetFilter, new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!IsSerialized(path))
                    continue;

                InspectDependencies(from, path, reachable, failures);
            }
        }

        /// <summary>
        /// Reads what one asset points at through the asset system rather than by parsing the file, so a
        /// reference nested inside a prefab is found the same way Unity finds it.
        /// </summary>
        private void InspectDependencies(string from, string path, ICollection<string> reachable,
            ICollection<string> failures)
        {
            foreach (string dependency in AssetDatabase.GetDependencies(path, false))
            {
                if (string.Equals(dependency, path, StringComparison.Ordinal))
                    continue;

                string target = PackageOf(dependency);

                if (!IsInScope(target)
                    || string.Equals(target, from, StringComparison.Ordinal)
                    || reachable.Contains(target))
                    continue;

                failures.Add($"{path} points at {dependency} from {target}, which installing "
                    + $"{_entryByPackage[from].Name} never brings in. Add the dependency to the entry, or "
                    + "drop the reference.");
            }
        }

        [Serializable]
        private sealed class RawAsmdef
        {
            public string name;
            public string[] references;
            public string[] defineConstraints;
        }
    }
}