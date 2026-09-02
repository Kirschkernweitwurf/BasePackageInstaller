using System.Collections.Generic;
using Base.PackageInstaller.Data;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Covers <see cref="PackageDependencyResolver.FindBroken"/>, which names the installed packages a
    /// removal would leave behind in a project that no longer compiles.
    /// </summary>
    public sealed class PackageDependencyResolverBrokenTests
    {
        private const string A = "A";
        private const string B = "B";
        private const string C = "C";
        private const string D = "D";

        /// <summary>Everything still installed that reaches the removal, directly or not, is named.</summary>
        [Test]
        public void NamesEveryInstalledDependentLeftBehind()
        {
            PackageEntry[] packages = Registry();

            List<string> broken = PackageDependencyResolver.FindBroken(packages,
                TestPackages.Flags(packages, C), TestPackages.AllStatuses(packages));

            Assert.That(broken, Is.EqualTo(new[] { A, B, D }));
        }

        /// <summary>A package that is not installed cannot break, so it is not named.</summary>
        [Test]
        public void IgnoresPackagesThatAreNotInstalled()
        {
            PackageEntry[] packages = Registry();

            List<string> broken = PackageDependencyResolver.FindBroken(packages,
                TestPackages.Flags(packages, C), TestPackages.Statuses(packages, B, C));

            Assert.That(broken, Is.EqualTo(new[] { B }));
        }

        /// <summary>Taking the whole chain out leaves nothing behind to break.</summary>
        [Test]
        public void ReportsNothingWhenTheWholeChainGoes()
        {
            PackageEntry[] packages = Registry();

            List<string> broken = PackageDependencyResolver.FindBroken(packages, TestPackages.AllFlags(packages),
                TestPackages.AllStatuses(packages));

            Assert.That(broken, Is.Empty);
        }

        /// <summary>Removing a package nothing depends on breaks nothing.</summary>
        [Test]
        public void ReportsNothingWhenNothingDependsOnTheRemoval()
        {
            PackageEntry[] packages = Registry();

            List<string> broken = PackageDependencyResolver.FindBroken(packages,
                TestPackages.Flags(packages, A), TestPackages.AllStatuses(packages));

            Assert.That(broken, Is.Empty);
        }

        /// <summary>A missing registry reports nothing rather than throwing.</summary>
        [Test]
        public void NullRegistry_ReportsNothing() =>
            Assert.That(PackageDependencyResolver.FindBroken(null, null, null), Is.Empty);

        // A needs B, B needs C, and D needs C as well, so removing C reaches three packages by two
        // routes, one of them only through another package.
        private static PackageEntry[] Registry() => new[]
        {
            TestPackages.Entry(A, B),
            TestPackages.Entry(B, C),
            TestPackages.Entry(C),
            TestPackages.Entry(D, C)
        };
    }
}