using System.Runtime.CompilerServices;

// The installer keeps its whole surface internal, so the test assembly is named here rather than
// widening types to public for the sake of being testable.
[assembly: InternalsVisibleTo("Base.PackageInstaller.Tests")]