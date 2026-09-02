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
        public void Defaults_PassValidation() =>
            Assert.That(PackageRegistryValidator.Validate(BasePackageDefaults.Create()), Is.Empty);

        /// <summary>Every default names a dependency the list actually holds.</summary>
        [Test]
        public void Defaults_OnlyDependOnEntriesInTheList()
        {
            PackageEntry[] packages = BasePackageDefaults.Create();
            HashSet<string> names = new();

            foreach (PackageEntry entry in packages)
                names.Add(entry.Name);

            foreach (PackageEntry entry in packages)
            {
                foreach (string dependency in entry.DependsOn)
                    Assert.That(names, Does.Contain(dependency));
            }
        }

        /// <summary>Installing the whole set lands every package after everything it needs.</summary>
        [Test]
        public void Defaults_InstallInAnOrderThatSatisfiesEveryDependency()
        {
            PackageEntry[] packages = BasePackageDefaults.Create();

            List<int> ordered = PackageDependencyResolver.ResolveOrder(packages, TestPackages.AllFlags(packages),
                EPackageMode.Install);

            Assert.That(ordered, Has.Count.EqualTo(packages.Length));

            Dictionary<string, int> position = new();

            for (int i = 0; i < ordered.Count; i++)
                position[packages[ordered[i]].Name] = i;

            foreach (PackageEntry entry in packages)
            {
                foreach (string dependency in entry.DependsOn)
                    Assert.That(position[dependency], Is.LessThan(position[entry.Name]));
            }
        }
    }
}