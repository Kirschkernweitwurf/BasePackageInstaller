using System.Collections.Generic;
using Base.PackageInstaller.Data;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Checks the list the installer ships. <see cref="BasePackageDefaults"/> is generated from the
    /// assembly definitions rather than written by hand, so this is where a bad generation run or a
    /// hand edit is caught, instead of in a project that has already installed it.
    /// </summary>
    public sealed class BasePackageDefaultsTests
    {
        /// <summary>The shipped defaults hold no missing name, no duplicate and no cycle.</summary>
        [Test]
        public void TheShippedDefaultsPassValidation()
            => Assert.That(PackageRegistryValidator.Validate(BasePackageDefaults.Create()), Is.Empty);

        /// <summary>The shipped list is not empty, so a failed generation run cannot pass silently.</summary>
        [Test]
        public void TheShippedDefaultsAreNotEmpty()
            => Assert.That(BasePackageDefaults.Create(), Is.Not.Empty);

        /// <summary>Every default carries a name and a URL to install from.</summary>
        [Test]
        public void EveryDefaultCarriesANameAndAUrl()
        {
            PackageEntry[] packages = BasePackageDefaults.Create();

            Assert.That(Names(packages), Is.All.Matches<string>(IsFilledIn));
            Assert.That(Urls(packages), Is.All.Matches<string>(IsFilledIn));
        }

        /// <summary>No two defaults share a name, since the registry matches its entries by name.</summary>
        [Test]
        public void NoTwoDefaultsShareAName()
            => Assert.That(Names(BasePackageDefaults.Create()), Is.Unique);

        /// <summary>Every default names a dependency the list actually holds.</summary>
        [Test]
        public void EveryDefaultDependsOnlyOnEntriesInTheList()
        {
            PackageEntry[] packages = BasePackageDefaults.Create();

            Assert.That(Dependencies(packages), Is.SubsetOf(Names(packages)));
        }

        /// <summary>Installing the whole set lands every package after everything it needs.</summary>
        [Test]
        public void TheDefaultsInstallInAnOrderThatSatisfiesEveryDependency()
        {
            PackageEntry[] packages = BasePackageDefaults.Create();

            List<int> ordered = PackageDependencyResolver.ResolveOrder(packages, TestPackages.AllFlags(packages),
                EPackageMode.Install);

            Assert.That(ordered, Has.Count.EqualTo(packages.Length));
            Assert.That(DependenciesInstalledTooLate(packages, ordered), Is.Empty);
        }

        private static bool IsFilledIn(string value) => !string.IsNullOrEmpty(value);

        // The names of every default, in list order.
        private static List<string> Names(PackageEntry[] packages)
        {
            List<string> names = new();

            foreach (PackageEntry entry in packages)
                names.Add(entry.Name);

            return names;
        }

        private static List<string> Urls(PackageEntry[] packages)
        {
            List<string> urls = new();

            foreach (PackageEntry entry in packages)
                urls.Add(entry.Url);

            return urls;
        }

        // Every name any default lists, with the duplicates collapsed so the subset check reads as
        // membership rather than as a count.
        private static HashSet<string> Dependencies(PackageEntry[] packages)
        {
            HashSet<string> dependencies = new();

            foreach (PackageEntry entry in packages)
            {
                foreach (string dependency in entry.DependsOn)
                    dependencies.Add(dependency);
            }

            return dependencies;
        }

        // Gathering the offenders lets the assertion state one fact and name every pair that broke it.
        private static List<string> DependenciesInstalledTooLate(PackageEntry[] packages, List<int> ordered)
        {
            Dictionary<string, int> position = new();

            for (int index = 0; index < ordered.Count; index++)
                position[packages[ordered[index]].Name] = index;

            List<string> late = new();

            foreach (PackageEntry entry in packages)
            {
                foreach (string dependency in entry.DependsOn)
                {
                    if (position[dependency] > position[entry.Name])
                        late.Add($"{dependency} installs after {entry.Name}");
                }
            }

            return late;
        }
    }
}