using System;
using Base.PackageInstaller.Data;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Covers <see cref="PackageRegistryValidator"/>, which reports the hand-editing mistakes the
    /// resolver answers by quietly doing nothing.
    /// </summary>
    public sealed class PackageRegistryValidatorTests
    {
        private const string A = "A";
        private const string B = "B";
        private const string C = "C";
        private const string Unknown = "Missing";

        /// <summary>A sound registry reports nothing.</summary>
        [Test]
        public void SoundRegistry_ReportsNothing()
        {
            PackageEntry[] packages =
            {
                TestPackages.Entry(A, B),
                TestPackages.Entry(B)
            };

            Assert.That(PackageRegistryValidator.Validate(packages), Is.Empty);
        }

        /// <summary>A nameless entry is reported once, however many of them there are.</summary>
        [Test]
        public void MissingName_IsReportedOnce()
        {
            PackageEntry[] packages =
            {
                new(string.Empty, TestPackages.Url(A)),
                new(string.Empty, TestPackages.Url(B))
            };

            Assert.That(PackageRegistryValidator.Validate(packages), Has.Length.EqualTo(1));
        }

        /// <summary>An entry without a URL is reported.</summary>
        [Test]
        public void MissingUrl_IsReported()
        {
            PackageEntry[] packages = { new(A, string.Empty) };

            Assert.That(PackageRegistryValidator.Validate(packages), Has.Length.EqualTo(1));
        }

        /// <summary>Two entries sharing a name are reported, because only one of them is ever used.</summary>
        [Test]
        public void DuplicateName_IsReported()
        {
            PackageEntry[] packages =
            {
                TestPackages.Entry(A),
                TestPackages.Entry(A)
            };

            Assert.That(PackageRegistryValidator.Validate(packages), Has.Length.EqualTo(1));
        }

        /// <summary>An entry listing itself is reported once, and not a second time as a cycle.</summary>
        [Test]
        public void SelfDependency_IsReportedOnce()
        {
            PackageEntry[] packages = { TestPackages.Entry(A, A) };

            Assert.That(PackageRegistryValidator.Validate(packages), Has.Length.EqualTo(1));
        }

        /// <summary>A dependency naming an entry that does not exist is reported.</summary>
        [Test]
        public void UnknownDependency_IsReported()
        {
            PackageEntry[] packages = { TestPackages.Entry(A, Unknown) };

            Assert.That(PackageRegistryValidator.Validate(packages), Has.Length.EqualTo(1));
        }

        /// <summary>Two entries depending on each other are reported once, not once per direction.</summary>
        [Test]
        public void TwoPackageCycle_IsReportedOnce()
        {
            PackageEntry[] packages =
            {
                TestPackages.Entry(A, B),
                TestPackages.Entry(B, A)
            };

            Assert.That(PackageRegistryValidator.Validate(packages), Has.Length.EqualTo(1));
        }

        /// <summary>A cycle running through a third entry is reported once as well.</summary>
        [Test]
        public void LongerCycle_IsReportedOnce()
        {
            PackageEntry[] packages =
            {
                TestPackages.Entry(A, B),
                TestPackages.Entry(B, C),
                TestPackages.Entry(C, A)
            };

            Assert.That(PackageRegistryValidator.Validate(packages), Has.Length.EqualTo(1));
        }

        /// <summary>An empty registry reports nothing.</summary>
        [Test]
        public void EmptyRegistry_ReportsNothing() =>
            Assert.That(PackageRegistryValidator.Validate(Array.Empty<PackageEntry>()), Is.Empty);

        /// <summary>A missing registry reports nothing rather than throwing.</summary>
        [Test]
        public void NullRegistry_ReportsNothing() => Assert.That(PackageRegistryValidator.Validate(null), Is.Empty);
    }
}