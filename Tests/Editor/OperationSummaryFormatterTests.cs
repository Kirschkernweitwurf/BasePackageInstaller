using Base.PackageInstaller.Data;
using Base.PackageInstaller.Window.Format;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Covers the wording of the report a finished run leaves behind. It is the only account the user
    /// gets of what the installer just did to their project, so an install that reads as an update, or
    /// a failure that reads as a success, is a real problem even though nothing threw.
    /// </summary>
    public sealed class OperationSummaryFormatterTests
    {
        private const string FailureReason = "the remote refused the connection";

        /// <summary>A fresh install names the package and the version it landed on.</summary>
        [Test]
        public void AFreshInstallNamesTheVersionItLandedOn()
        {
            string line = OperationSummaryFormatter.Describe(TestResults.Installed());

            Assert.That(line, Does.Contain(TestResults.Name));
            Assert.That(line, Does.Contain(TestResults.NewVersion));
            Assert.That(line, Does.Not.Contain(TestResults.OldVersion));
        }

        /// <summary>An install with no version known still names the package.</summary>
        [Test]
        public void AnInstallWithoutAVersionStillNamesThePackage()
            => Assert.That(OperationSummaryFormatter.Describe(TestResults.Installed(string.Empty)),
                Does.Contain(TestResults.Name));

        /// <summary>An update names both versions, so the user can see what moved.</summary>
        [Test]
        public void AnUpdateNamesBothVersions()
        {
            string line = OperationSummaryFormatter.Describe(TestResults.Updated());

            Assert.That(line, Does.Contain(TestResults.OldVersion));
            Assert.That(line, Does.Contain(TestResults.NewVersion));
        }

        /// <summary>
        /// A package already at the target version reads as up to date rather than as an install, so a
        /// run that changed nothing does not look like one that did.
        /// </summary>
        [Test]
        public void APackageAlreadyAtItsVersionReadsAsUpToDate()
        {
            string line = OperationSummaryFormatter.Describe(TestResults.Unchanged());

            Assert.That(line, Does.Contain("up to date"));
            Assert.That(line, Does.Contain(TestResults.NewVersion));
        }

        /// <summary>A removal names the version that went, not a version that arrived.</summary>
        [Test]
        public void ARemovalNamesTheVersionThatWent()
        {
            string line = OperationSummaryFormatter.Describe(TestResults.Removed());

            Assert.That(line, Does.Contain(TestResults.Name));
            Assert.That(line, Does.Contain(TestResults.OldVersion));
        }

        /// <summary>A removal of something with no known version still names the package.</summary>
        [Test]
        public void ARemovalWithoutAVersionStillNamesThePackage()
            => Assert.That(OperationSummaryFormatter.Describe(TestResults.Removed(string.Empty)),
                Does.Contain(TestResults.Name));

        /// <summary>
        /// A failure carries the reason it failed. Without it the user is told only that something
        /// went wrong, which is the one case where the report has to be specific.
        /// </summary>
        [Test]
        public void AFailureCarriesTheReason()
        {
            string line = OperationSummaryFormatter.Describe(TestResults.Failed(FailureReason));

            Assert.That(line, Does.Contain(FailureReason));
            Assert.That(line, Does.Contain(TestResults.Label));
        }

        /// <summary>A failure is described by its label, since the package name may never have resolved.</summary>
        [Test]
        public void AFailureFallsBackToTheLabel()
            => Assert.That(OperationSummaryFormatter.Describe(TestResults.Failed(FailureReason)),
                Does.Contain(TestResults.Label));

        /// <summary>An install headline carries the counts that make it readable.</summary>
        [Test]
        public void AnInstallHeadlineCarriesItsCounts()
        {
            string report = OperationSummaryFormatter.BuildSummary(
                TestResults.Summary(EPackageAction.Add, 3, 0, 2, 1, TestResults.Installed()));

            Assert.That(report, Does.Contain("3 ok"));
            Assert.That(report, Does.Contain("2 changed"));
            Assert.That(report, Does.Contain("1 unchanged"));
        }

        /// <summary>A count of nothing is left out rather than reported as zero.</summary>
        [Test]
        public void ACountOfNothingIsLeftOut()
        {
            string report = OperationSummaryFormatter.BuildSummary(
                TestResults.Summary(EPackageAction.Add, 1, 0, 0, 0, TestResults.Installed()));

            Assert.That(report, Does.Not.Contain("0 changed"));
            Assert.That(report, Does.Not.Contain("0 unchanged"));
            Assert.That(report, Does.Not.Contain("failed"));
        }

        /// <summary>
        /// A removal headline leaves out the version counts, which would only ever be noise on a run
        /// that has nothing to say about versions.
        /// </summary>
        [Test]
        public void ARemovalHeadlineLeavesOutTheVersionCounts()
        {
            string report = OperationSummaryFormatter.BuildSummary(
                TestResults.Summary(EPackageAction.Remove, 2, 0, 0, 0, TestResults.Removed()));

            Assert.That(report, Does.Contain("2 removed"));
            Assert.That(report, Does.Not.Contain("ok"));
            Assert.That(report, Does.Not.Contain("changed"));
        }

        /// <summary>A failure count is reported alongside the successes.</summary>
        [Test]
        public void AFailureCountIsReportedAlongsideTheSuccesses()
        {
            string report = OperationSummaryFormatter.BuildSummary(TestResults.Summary(EPackageAction.Add,
                1, 1, 1, 0, TestResults.Installed(), TestResults.Failed(FailureReason)));

            Assert.That(report, Does.Contain("1 ok"));
            Assert.That(report, Does.Contain("1 failed"));
        }

        /// <summary>The report carries one line per package on top of the headline.</summary>
        [Test]
        public void TheReportCarriesOneLinePerPackage()
        {
            string report = OperationSummaryFormatter.BuildSummary(TestResults.Summary(EPackageAction.Add,
                2, 0, 2, 0, TestResults.Installed(), TestResults.Updated()));

            Assert.That(report.Split('\n'), Has.Length.EqualTo(3), "a headline and one line per package");
        }

        /// <summary>A run over nothing still produces a headline rather than an empty report.</summary>
        [Test]
        public void ARunOverNothingStillProducesAHeadline()
        {
            string report = OperationSummaryFormatter.BuildSummary(
                TestResults.Summary(EPackageAction.Add, 0, 0, 0, 0));

            Assert.That(report, Is.Not.Empty);
            Assert.That(report, Does.Contain("0 ok"));
        }
    }
}