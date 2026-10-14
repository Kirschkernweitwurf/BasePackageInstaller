using Base.PackageInstaller.Data;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Checks the fingerprint the registry uses to notice that the shipped defaults moved on. If it
    /// missed a change, projects would keep listing an old set of packages until someone pressed
    /// Refresh by hand.
    /// </summary>
    public sealed class RegistryFingerprintTests
    {
        private const string Name = "Core";
        private const string OtherName = "Utility";
        private const string Url = "https://example.com/Packages.git?path=Core";
        private const string OtherUrl = "https://example.com/Packages.git?path=Utility";

        /// <summary>Two lists with the same entries condense to the same value.</summary>
        [Test]
        public void EqualListsShareAFingerprint() => Assert.That(
            BasePackageRegistry.Fingerprint(new[] { new PackageEntry(Name, Url, OtherName) }),
            Is.EqualTo(BasePackageRegistry.Fingerprint(new[] { new PackageEntry(Name, Url, OtherName) })));

        /// <summary>A new default changes the fingerprint, which is what brings it into old projects.</summary>
        [Test]
        public void AnAddedEntryChangesTheFingerprint() => Assert.That(
            BasePackageRegistry.Fingerprint(new[] { new PackageEntry(Name, Url) }),
            Is.Not.EqualTo(BasePackageRegistry.Fingerprint(new[]
            {
                new PackageEntry(Name, Url),
                new PackageEntry(OtherName, OtherUrl)
            })));

        /// <summary>A moved URL changes the fingerprint.</summary>
        [Test]
        public void AChangedUrlChangesTheFingerprint() => Assert.That(
            BasePackageRegistry.Fingerprint(new[] { new PackageEntry(Name, Url) }),
            Is.Not.EqualTo(BasePackageRegistry.Fingerprint(new[] { new PackageEntry(Name, OtherUrl) })));

        /// <summary>A new dependency changes the fingerprint.</summary>
        [Test]
        public void AChangedDependencyChangesTheFingerprint() => Assert.That(
            BasePackageRegistry.Fingerprint(new[] { new PackageEntry(Name, Url) }),
            Is.Not.EqualTo(BasePackageRegistry.Fingerprint(new[] { new PackageEntry(Name, Url, OtherName) })));

        /// <summary>The shipped defaults always condense to the same value, so a sync is not repeated.</summary>
        [Test]
        public void TheShippedDefaultsHaveAStableFingerprint() => Assert.That(
            BasePackageRegistry.Fingerprint(BasePackageDefaults.Create()),
            Is.EqualTo(BasePackageRegistry.Fingerprint(BasePackageDefaults.Create())));
    }
}
