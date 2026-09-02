using System.Collections.Generic;
using Base.PackageInstaller.Data;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Covers the order <see cref="PackageDependencyResolver.ResolveOrder"/> hands the selection to
    /// the package manager in, which is what keeps the project compiling at every step of a run.
    /// </summary>
    public sealed class PackageDependencyResolverOrderTests
    {
        private const string A = "A";
        private const string B = "B";
        private const string C = "C";
        private const string D = "D";

        /// <summary>An install starts at the leaves, so every package finds what it needs present.</summary>
        [Test]
        public void Install_PutsDependenciesBeforeDependents()
        {
            PackageEntry[] packages = Registry();

            List<int> ordered = PackageDependencyResolver.ResolveOrder(packages,
                TestPackages.Flags(packages, A, B, C), EPackageMode.Install);

            Assert.That(TestPackages.Names(packages, ordered), Is.EqualTo(new[] { C, B, A }));
        }

        /// <summary>A removal is the install order reversed, so nothing goes while it is still needed.</summary>
        [Test]
        public void Uninstall_ReversesTheInstallOrder()
        {
            PackageEntry[] packages = Registry();

            List<int> ordered = PackageDependencyResolver.ResolveOrder(packages,
                TestPackages.Flags(packages, A, B, C), EPackageMode.Uninstall);

            Assert.That(TestPackages.Names(packages, ordered), Is.EqualTo(new[] { A, B, C }));
        }

        /// <summary>
        /// A dependency left out of the run is either already installed or deliberately skipped, so it
        /// never holds an entry back.
        /// </summary>
        [Test]
        public void OnlyDependenciesInsideTheRunHoldAnEntryBack()
        {
            PackageEntry[] packages = Registry();

            List<int> ordered = PackageDependencyResolver.ResolveOrder(packages,
                TestPackages.Flags(packages, A), EPackageMode.Install);

            Assert.That(TestPackages.Names(packages, ordered), Is.EqualTo(new[] { A }));
        }

        /// <summary>A cycle cannot be ordered, so the run falls back to list order instead of stalling.</summary>
        [Test]
        public void Cycle_FallsBackToListOrder()
        {
            PackageEntry[] packages =
            {
                TestPackages.Entry(A, B),
                TestPackages.Entry(B, A)
            };

            List<int> ordered = PackageDependencyResolver.ResolveOrder(packages,
                TestPackages.Flags(packages, A, B), EPackageMode.Install);

            Assert.That(TestPackages.Names(packages, ordered), Is.EqualTo(new[] { A, B }));
        }

        /// <summary>An entry listing itself does not wait for itself.</summary>
        [Test]
        public void SelfDependency_DoesNotStall()
        {
            PackageEntry[] packages = { TestPackages.Entry(A, A) };

            List<int> ordered = PackageDependencyResolver.ResolveOrder(packages,
                TestPackages.Flags(packages, A), EPackageMode.Install);

            Assert.That(TestPackages.Names(packages, ordered), Is.EqualTo(new[] { A }));
        }

        /// <summary>An empty selection produces an empty run.</summary>
        [Test]
        public void EmptySelection_ProducesNothing()
        {
            PackageEntry[] packages = Registry();

            List<int> ordered = PackageDependencyResolver.ResolveOrder(packages, new bool[packages.Length],
                EPackageMode.Install);

            Assert.That(ordered, Is.Empty);
        }

        /// <summary>A missing registry produces an empty run rather than throwing.</summary>
        [Test]
        public void NullRegistry_ProducesNothing() =>
            Assert.That(PackageDependencyResolver.ResolveOrder(null, null, EPackageMode.Install), Is.Empty);

        // A needs B, B needs C, and D needs C as well.
        private static PackageEntry[] Registry() => new[]
        {
            TestPackages.Entry(A, B),
            TestPackages.Entry(B, C),
            TestPackages.Entry(C),
            TestPackages.Entry(D, C)
        };
    }
}