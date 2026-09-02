using Base.PackageInstaller.Data;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Covers how <see cref="PackageDependencyResolver.Resolve"/> turns the rows the user ticked into
    /// the rows a run actually processes, in both directions and with expansion on and off.
    /// </summary>
    public sealed class PackageDependencyResolverSelectionTests
    {
        private const string A = "A";
        private const string B = "B";
        private const string C = "C";
        private const string D = "D";
        private const string Standalone = "E";
        private const string Unknown = "Missing";

        /// <summary>Ticking an entry pulls in everything it needs, all the way down the chain.</summary>
        [Test]
        public void AnInstallPullsInTransitiveDependencies()
        {
            PackageEntry[] packages = TestPackages.Chain();

            bool[] selected = TestPackages.Resolve(packages, TestPackages.Flags(packages, A),
                EPackageMode.Install, expandDependencies: true);

            Assert.That(TestPackages.Names(packages, selected), Is.EqualTo(new[] { A, B, C }));
        }

        /// <summary>
        /// The selection is derived from the picks alone, so dropping a pick releases what it dragged
        /// in rather than leaving it behind.
        /// </summary>
        [Test]
        public void AnInstallDerivesTheSelectionFromThePicksAlone()
        {
            PackageEntry[] packages = TestPackages.Chain();

            bool[] selected = TestPackages.Resolve(packages, TestPackages.Flags(packages, D),
                EPackageMode.Install, expandDependencies: true);

            Assert.That(TestPackages.Names(packages, selected), Is.EqualTo(new[] { C, D }));
        }

        /// <summary>A released entry stays in when another pick still needs it.</summary>
        [Test]
        public void AnInstallKeepsWhatAnotherPickStillNeeds()
        {
            PackageEntry[] packages = TestPackages.Chain();

            bool[] selected = TestPackages.Resolve(packages, TestPackages.Flags(packages, B, D),
                EPackageMode.Install, expandDependencies: true);

            Assert.That(TestPackages.Names(packages, selected), Is.EqualTo(new[] { B, C, D }));
        }

        /// <summary>A dependency naming an entry the registry does not hold is ignored.</summary>
        [Test]
        public void AnInstallIgnoresUnknownDependencies()
        {
            PackageEntry[] packages = { TestPackages.Entry(Standalone, Unknown) };

            bool[] selected = TestPackages.Resolve(packages, TestPackages.Flags(packages, Standalone),
                EPackageMode.Install, expandDependencies: true);

            Assert.That(TestPackages.Names(packages, selected), Is.EqualTo(new[] { Standalone }));
        }

        /// <summary>An entry listing itself neither selects twice nor claims to hold itself.</summary>
        [Test]
        public void AnInstallIgnoresAnEntryListingItself()
        {
            PackageEntry[] packages = { TestPackages.Entry(A, A) };
            string[] heldBy = new string[packages.Length];

            bool[] selected = TestPackages.Resolve(packages, TestPackages.Flags(packages, A),
                EPackageMode.Install, expandDependencies: true, heldBy);

            Assert.That(TestPackages.Names(packages, selected), Is.EqualTo(new[] { A }));
            Assert.That(TestPackages.HeldBy(packages, heldBy, A), Is.Empty);
        }

        /// <summary>Removing an entry pulls in everything that would not compile without it.</summary>
        [Test]
        public void ARemovalPullsInDependents()
        {
            PackageEntry[] packages = TestPackages.Chain();

            bool[] selected = TestPackages.Resolve(packages, TestPackages.Flags(packages, C),
                EPackageMode.Uninstall, expandDependencies: true);

            Assert.That(TestPackages.Names(packages, selected), Is.EqualTo(new[] { A, B, C, D }));
        }

        /// <summary>A removal leaves alone what does not depend on the pick.</summary>
        [Test]
        public void ARemovalLeavesUnrelatedEntriesAlone()
        {
            PackageEntry[] packages = TestPackages.Chain();

            bool[] selected = TestPackages.Resolve(packages, TestPackages.Flags(packages, B),
                EPackageMode.Uninstall, expandDependencies: true);

            Assert.That(TestPackages.Names(packages, selected), Is.EqualTo(new[] { A, B }));
        }

        /// <summary>Removing reverses what the column means, so a row names what it requires.</summary>
        [Test]
        public void ARemovalNamesWhatTheRowRequires()
        {
            PackageEntry[] packages = TestPackages.Chain();
            string[] heldBy = new string[packages.Length];

            TestPackages.Resolve(packages, TestPackages.Flags(packages, C), EPackageMode.Uninstall,
                expandDependencies: true, heldBy);

            Assert.That(TestPackages.HeldBy(packages, heldBy, A), Is.EqualTo(B));
        }

        /// <summary>With expansion off the picks are taken exactly as they are.</summary>
        [Test]
        public void WithoutExpansionThePicksAreTakenExactly()
        {
            PackageEntry[] packages = TestPackages.Chain();

            bool[] selected = TestPackages.Resolve(packages, TestPackages.Flags(packages, A),
                EPackageMode.Install, expandDependencies: false);

            Assert.That(TestPackages.Names(packages, selected), Is.EqualTo(new[] { A }));
        }

        /// <summary>With expansion off the holders are still reported, so the window can still warn.</summary>
        [Test]
        public void WithoutExpansionTheHoldersAreStillReported()
        {
            PackageEntry[] packages = TestPackages.Chain();
            string[] heldBy = new string[packages.Length];

            TestPackages.Resolve(packages, TestPackages.Flags(packages, A), EPackageMode.Install,
                expandDependencies: false, heldBy);

            Assert.That(TestPackages.HeldBy(packages, heldBy, B), Is.EqualTo(A));
        }

        /// <summary>A row held by more than one pick names all of them.</summary>
        [Test]
        public void ARowHeldBySeveralPicksNamesAllOfThem()
        {
            PackageEntry[] packages = TestPackages.Chain();
            string[] heldBy = new string[packages.Length];

            TestPackages.Resolve(packages, TestPackages.Flags(packages, A, D), EPackageMode.Install,
                expandDependencies: true, heldBy);

            Assert.That(TestPackages.HeldBy(packages, heldBy, C), Is.EqualTo($"{B}, {D}"));
        }

        /// <summary>A missing registry is ignored rather than throwing.</summary>
        [Test]
        public void AMissingRegistryIsIgnored() => Assert.DoesNotThrow(() => PackageDependencyResolver.Resolve(
            null, null, null, null, EPackageMode.Install, expandDependencies: true));

    }
}