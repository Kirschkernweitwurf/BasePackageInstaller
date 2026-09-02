using System.Text;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Turns a package folder name into the name the installer window lists it under.
    /// <para>
    /// Splitting the folder name on its capitals covers every package, so nothing is listed by hand.
    /// The names have to stay stable: the installer registry matches its entries by name, so
    /// renaming one orphans the entry in every existing project unless
    /// <see cref="Data.LegacyPackageNames"/> carries the old name forward.
    /// </para>
    /// </summary>
    internal static class PackageDisplayNames
    {
        private const int ExtraCapacity = 4;
        private const char Space = ' ';

        /// <summary>
        /// Resolves the installer display name for a package folder.
        /// </summary>
        /// <param name="folderName">The folder name under the packages root.</param>
        /// <returns>The name the installer lists the package under.</returns>
        internal static string Resolve(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                return string.Empty;

            StringBuilder builder = new(folderName.Length + ExtraCapacity);

            // A capital only starts a new word when the character before it is lower case, so an
            // acronym such as UI stays in one piece.
            for (int i = 0; i < folderName.Length; i++)
            {
                if (i > 0
                    && char.IsUpper(folderName[i])
                    && char.IsLower(folderName[i - 1]))
                    builder.Append(Space);

                builder.Append(folderName[i]);
            }

            return builder.ToString();
        }
    }
}