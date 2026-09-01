using System.Collections.Generic;
using System.Text;

namespace Base.PackageInstaller.PackageDefaults
{
    /// <summary>
    /// Turns a package folder name into the name the installer window lists it under.
    /// <para>
    /// Splitting the folder name on its capitals gets nearly every package right, so only the two
    /// that disagree are listed by hand. The names have to stay stable: the installer registry
    /// matches its entries by name, so renaming one orphans the entry in every existing project.
    /// </para>
    /// </summary>
    internal static class PackageDisplayNames
    {
        private const char Space = ' ';

        private static readonly Dictionary<string, string> Overrides = new()
        {
            ["EditorUi"] = "Editor UI",
            ["Settings"] = "Settings System"
        };

        /// <summary>
        /// Resolves the installer display name for a package folder.
        /// </summary>
        /// <param name="folderName">The folder name under the packages root.</param>
        /// <returns>The name the installer lists the package under.</returns>
        internal static string Resolve(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                return string.Empty;

            return Overrides.TryGetValue(folderName, out string overridden)
                ? overridden
                : SplitOnCapitals(folderName);
        }

        // A capital only starts a new word when the character before it is lower case, so an
        // acronym such as UI stays in one piece.
        private static string SplitOnCapitals(string value)
        {
            StringBuilder builder = new(value.Length + 4);

            for (int i = 0; i < value.Length; i++)
            {
                if (i > 0
                    && char.IsUpper(value[i])
                    && char.IsLower(value[i - 1]))
                    builder.Append(Space);

                builder.Append(value[i]);
            }

            return builder.ToString();
        }
    }
}