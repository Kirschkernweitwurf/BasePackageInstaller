using System.Collections.Generic;
using Base.PackageInstaller.PackageDefaults;
using NUnit.Framework;

namespace Base.PackageInstaller.Tests
{
    /// <summary>
    /// Covers the comparison the defaults window shows before it overwrites a generated file. The
    /// point of the longest common subsequence is that one inserted line shows up as one added line
    /// rather than marking everything after it as changed, which is what makes the preview readable
    /// enough to trust.
    /// </summary>
    public sealed class TextDiffTests
    {
        private const string First = "one";
        private const string Inserted = "inserted";
        private const string Second = "two";
        private const string Third = "three";

        /// <summary>With no target picked there is nothing to compare against.</summary>
        [Test]
        public void WithoutATargetThereIsNothingToCompare()
        {
            DiffResult result = TextDiff.Compare(Text(First), Text(First), hasTarget: false);

            Assert.That(result.State, Is.EqualTo(EDiffState.NoTarget));
            Assert.That(result.Lines, Is.Empty);
        }

        /// <summary>A target that does not exist yet reads as missing rather than as a change.</summary>
        [Test]
        public void AFileThatDoesNotExistReadsAsMissing()
        {
            DiffResult result = TextDiff.Compare(Text(First), null, hasTarget: true);

            Assert.That(result.State, Is.EqualTo(EDiffState.Missing));
            Assert.That(result.Lines, Is.Empty);
        }

        /// <summary>Identical text reports no edits at all.</summary>
        [Test]
        public void IdenticalTextReportsNoEdits()
        {
            DiffResult result = Compare(Text(First, Second), Text(First, Second));

            Assert.That(result.State, Is.EqualTo(EDiffState.Identical));
            Assert.That(result.AddedCount, Is.EqualTo(0));
            Assert.That(result.RemovedCount, Is.EqualTo(0));
        }

        /// <summary>
        /// A line ending difference alone is not a change. The writer always emits CRLF, so comparing
        /// on bytes would leave a file on disk permanently reported as different.
        /// </summary>
        [Test]
        public void ALineEndingDifferenceIsNotAChange()
        {
            DiffResult result = Compare($"{First}\r\n{Second}", $"{First}\n{Second}");

            Assert.That(result.State, Is.EqualTo(EDiffState.Identical));
        }

        /// <summary>
        /// One inserted line is one added line, and everything after it stays unchanged. A naive walk
        /// would report the whole remainder of the file instead.
        /// </summary>
        [Test]
        public void AnInsertedLineDoesNotDisturbTheLinesBelowIt()
        {
            DiffResult result = Compare(Text(First, Inserted, Second, Third), Text(First, Second, Third));

            Assert.That(result.AddedCount, Is.EqualTo(1));
            Assert.That(result.RemovedCount, Is.EqualTo(0));
            Assert.That(Kinds(result), Is.EqualTo(new[]
            {
                EDiffKind.Unchanged, EDiffKind.Added, EDiffKind.Unchanged, EDiffKind.Unchanged
            }));
        }

        /// <summary>A deleted line is one removed line, with the rest left in place.</summary>
        [Test]
        public void ADeletedLineIsReportedOnItsOwn()
        {
            DiffResult result = Compare(Text(First, Third), Text(First, Second, Third));

            Assert.That(result.RemovedCount, Is.EqualTo(1));
            Assert.That(result.AddedCount, Is.EqualTo(0));
            Assert.That(TextOf(result, EDiffKind.Removed), Is.EqualTo(new[] { Second }));
        }

        /// <summary>A replaced line reads as one line out and one line in.</summary>
        [Test]
        public void AReplacedLineIsBothAnAdditionAndARemoval()
        {
            DiffResult result = Compare(Text(First, Inserted), Text(First, Second));

            Assert.That(result.State, Is.EqualTo(EDiffState.Changed));
            Assert.That(result.AddedCount, Is.EqualTo(1));
            Assert.That(result.RemovedCount, Is.EqualTo(1));
        }

        /// <summary>Every line of a new file is an addition.</summary>
        [Test]
        public void EveryLineOfAnEmptyTargetIsAnAddition()
        {
            DiffResult result = Compare(Text(First, Second), string.Empty);

            Assert.That(result.AddedCount, Is.EqualTo(2));
            Assert.That(TextOf(result, EDiffKind.Added), Is.EqualTo(new[] { First, Second }));
        }

        /// <summary>Generating nothing over an existing file removes every line of it.</summary>
        [Test]
        public void EveryLineOfAnEmptyGenerationIsARemoval()
        {
            DiffResult result = Compare(string.Empty, Text(First, Second));

            Assert.That(result.RemovedCount, Is.EqualTo(2));
        }

        /// <summary>The rendered lines carry the whole file, not only the edits.</summary>
        [Test]
        public void TheRenderedLinesCarryTheWholeFile()
        {
            DiffResult result = Compare(Text(First, Inserted, Second), Text(First, Second));

            Assert.That(result.Lines, Has.Count.EqualTo(3));
        }

        private static DiffResult Compare(string generated, string onDisk)
            => TextDiff.Compare(generated, onDisk, hasTarget: true);

        private static string Text(params string[] lines) => string.Join("\r\n", lines);

        private static List<EDiffKind> Kinds(DiffResult result)
        {
            List<EDiffKind> kinds = new();

            foreach (DiffLine line in result.Lines)
                kinds.Add(line.Kind);

            return kinds;
        }

        private static List<string> TextOf(DiffResult result, EDiffKind kind)
        {
            List<string> text = new();

            foreach (DiffLine line in result.Lines)
            {
                if (line.Kind == kind)
                    text.Add(line.Text);
            }

            return text;
        }
    }
}