using System;
using System.Collections.Generic;
using Base.PackageInstaller.Data;
using Base.PackageInstaller.PackageDefaults;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Covers the two halves of how a package is named. The registry matches its entries by name, so
    /// a name that shifts underneath an existing project orphans the old row and adds a second one
    /// beside it. That is the failure this file exists to catch, because it only shows up in projects
    /// that were installed before the change and never in the one making it.
    /// </summary>
    public sealed class PackageNamingTests
    {
        private const string Acronym = "UI";
        private const string LegacyName = "Settings System";
        private const string TwoWordFolder = "ControllerSupport";

        /// <summary>Every default the installer ships. One test case is generated per entry.</summary>
        private static IEnumerable<string> EveryDefaultName()
        {
            foreach (PackageEntry entry in BasePackageDefaults.Create())
                yield return entry.Name;
        }

        /// <summary>A folder name splits into words at its capitals.</summary>
        [Test]
        public void AFolderNameSplitsAtItsCapitals()
            => Assert.That(PackageDisplayNames.Resolve(TwoWordFolder), Is.EqualTo("Controller Support"));

        /// <summary>A run of capitals stays in one piece, so an acronym is not torn apart.</summary>
        [Test]
        public void ARunOfCapitalsStaysInOnePiece()
            => Assert.That(PackageDisplayNames.Resolve(Acronym), Is.EqualTo(Acronym));

        /// <summary>A single word folder is already its own display name.</summary>
        [Test]
        public void ASingleWordFolderIsUnchanged()
            => Assert.That(PackageDisplayNames.Resolve("Core"), Is.EqualTo("Core"));

        /// <summary>No folder name at all resolves to no name, rather than throwing.</summary>
        [Test]
        public void NoFolderNameResolvesToNothing()
        {
            Assert.That(PackageDisplayNames.Resolve(null), Is.Empty);
            Assert.That(PackageDisplayNames.Resolve(string.Empty), Is.Empty);
        }

        /// <summary>A name that was never renamed comes back untouched.</summary>
        [Test]
        public void ANameThatWasNeverRenamedComesBackUntouched()
            => Assert.That(LegacyPackageNames.Resolve(TwoWordFolder), Is.EqualTo(TwoWordFolder));

        /// <summary>An empty name is handed straight back rather than treated as a rename.</summary>
        [Test]
        public void AnEmptyNameIsHandedBack()
            => Assert.That(LegacyPackageNames.Resolve(string.Empty), Is.Empty);

        /// <summary>
        /// Documents that a missing name throws rather than being handed back. Every other entry
        /// point in the installer answers null with a harmless result, so this one is the odd one
        /// out. An entry whose name was never filled in reaches here from the registry sync, which
        /// runs before the validator that would have reported it.
        /// </summary>
        [Test]
        public void AMissingNameCurrentlyThrows()
            => Assert.Throws<ArgumentNullException>(() => LegacyPackageNames.Resolve(null));

        /// <summary>A name the installer used to ship maps onto the name it ships now.</summary>
        [Test]
        public void AnOldNameMapsOntoTheCurrentOne()
            => Assert.That(LegacyPackageNames.Resolve(LegacyName), Is.Not.EqualTo(LegacyName));

        /// <summary>
        /// What an old name maps to is a package the installer actually ships. A rename pointing at
        /// nothing would leave the old row orphaned instead of carrying it forward.
        /// </summary>
        [Test]
        public void AnOldNameMapsOntoAPackageTheInstallerShips()
            => Assert.That(CurrentNames(), Contains.Item(LegacyPackageNames.Resolve(LegacyName)));

        /// <summary>
        /// A name currently in use is never itself renamed. Otherwise the rename would fire on a
        /// project that is already correct and move it off the name the defaults ship.
        /// </summary>
        /// <param name="name">The shipped package name under test.</param>
        [TestCaseSource(nameof(EveryDefaultName))]
        public void ACurrentNameIsNeverRenamedAway(string name)
            => Assert.That(LegacyPackageNames.Resolve(name), Is.EqualTo(name));

        // The names the installer ships today, as the list the membership check reads.
        private static List<string> CurrentNames()
        {
            List<string> names = new();

            foreach (string name in EveryDefaultName())
                names.Add(name);

            return names;
        }
    }
}