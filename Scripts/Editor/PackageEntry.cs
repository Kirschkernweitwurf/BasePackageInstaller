#if UNITY_EDITOR
namespace Base.PackageInstaller.Editor
{
    /// <summary>
    /// Represents a package entry with a name, package id and Git URL.
    /// </summary>
    public readonly struct PackageEntry
    {
        public readonly string Name;
        public readonly string PackageId;
        public readonly string Url;

        public PackageEntry(string name, string url)
        {
            Name = name;
            Url = url;
            PackageId = ExtractPackageId(url);
        }

        private static string ExtractPackageId(string url)
        {
            int index = url.LastIndexOf('/');

            if (index < 0 || index >= url.Length - 1)
                return url;

            return url[(index + 1)..];
        }
    }
}
#endif