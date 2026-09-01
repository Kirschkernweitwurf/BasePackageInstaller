using System.Collections.Generic;
using System.Text;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Renders the scanned graph as the <c>BasePackageDefaults</c> source file the installer ships.
    /// Pure text, so the result can be previewed and copied without touching disk.
    /// </summary>
    internal static class PackageDefaultsWriter
    {
        private const string BaseUrl =
            "https://github.com/Kirschkernweitwurf/BaseProjectPackages.git?path=BaseProject/Packages/";

        private const string Indent = "            ";
        private const string LineBreak = "\r\n";

        /// <summary>
        /// Renders the whole file.
        /// </summary>
        /// <param name="packages">The scanned packages, already sorted by display name.</param>
        /// <returns>The source text of the generated defaults class.</returns>
        internal static string Render(IReadOnlyList<PackageDependencyInfo> packages)
        {
            StringBuilder builder = new();

            builder.Append("namespace Base.PackageInstaller.Data").Append(LineBreak);
            builder.Append("{").Append(LineBreak);
            builder.Append("    /// <summary>").Append(LineBreak);
            builder.Append("    /// The default base packages seeded into a fresh <see cref=\"BasePackageRegistry\"/>.")
                .Append(LineBreak);

            builder.Append("    /// Generated from the assembly definitions by the Package Defaults window; edit that")
                .Append(LineBreak);

            builder.Append("    /// tool rather than this file.").Append(LineBreak);
            builder.Append("    /// </summary>").Append(LineBreak);
            builder.Append("    internal static class BasePackageDefaults").Append(LineBreak);
            builder.Append("    {").Append(LineBreak);

            AppendNameConstants(builder, packages);

            builder.Append(LineBreak);
            builder.Append("        private const string BaseUrl =").Append(LineBreak);
            builder.Append("            \"").Append(BaseUrl).Append("\";").Append(LineBreak);
            builder.Append(LineBreak);
            builder.Append("        /// <summary>").Append(LineBreak);
            builder.Append("        /// Creates a fresh copy of the default entries.").Append(LineBreak);
            builder.Append("        /// </summary>").Append(LineBreak);
            builder.Append("        /// <returns>The default package entries.</returns>").Append(LineBreak);
            builder.Append("        internal static PackageEntry[] Create() => new[]").Append(LineBreak);
            builder.Append("        {").Append(LineBreak);

            AppendEntries(builder, packages);

            builder.Append("        };").Append(LineBreak);
            builder.Append("    }").Append(LineBreak);
            builder.Append("}");

            return builder.ToString();
        }

        private static void AppendNameConstants(StringBuilder builder,
            IReadOnlyList<PackageDependencyInfo> packages)
        {
            foreach (PackageDependencyInfo package in packages)
            {
                builder.Append("        private const string ")
                    .Append(Identifier(package.FolderName))
                    .Append(" = \"")
                    .Append(package.DisplayName)
                    .Append("\";")
                    .Append(LineBreak);
            }
        }

        private static void AppendEntries(StringBuilder builder, IReadOnlyList<PackageDependencyInfo> packages)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                PackageDependencyInfo package = packages[i];

                builder.Append(Indent)
                    .Append("new PackageEntry(")
                    .Append(Identifier(package.FolderName))
                    .Append(", $\"{BaseUrl}")
                    .Append(package.FolderName)
                    .Append("\"");

                foreach (string dependency in package.DirectDependencies)
                    builder.Append(", ").Append(Identifier(dependency));

                builder.Append(")");

                if (i < packages.Count - 1)
                    builder.Append(",");

                builder.Append(LineBreak);
            }
        }

        // The folder name is already PascalCase, so it doubles as the constant name.
        private static string Identifier(string folderName) => folderName;
    }
}