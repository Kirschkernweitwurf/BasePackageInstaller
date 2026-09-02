using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Base.PackageInstaller.Shared
{
    /// <summary>
    /// Reads the Editor UI package's palette, table colors and metrics when that package is
    /// installed, so the two installer windows follow the theme every other Base window follows.
    /// Falls back to the installer's own values when it is not.
    /// </summary>
    /// <remarks>
    /// The installer cannot reference the Editor UI package. It has to compile in a project where no
    /// Base package exists yet, which is the whole point of it, so the only way to read that package
    /// is to look for it by name once the editor has loaded it.
    /// <para>
    /// Member names are string literals here, which is the one place in these repositories that
    /// happens. There is nothing to write <c>nameof</c> against, because the type may genuinely not
    /// exist. A name that stops resolving while the package is installed is reported once per domain
    /// reload rather than falling back silently forever, so a rename on the other side shows up in
    /// the console instead of as a window that quietly stopped following the theme.
    /// </para>
    /// <para>
    /// Every resolved getter becomes a delegate rather than being invoked through
    /// <see cref="PropertyInfo"/>. A palette color is read per row per repaint, and reflection with
    /// boxing on that path is the kind of cost that only shows up on someone else's machine.
    /// </para>
    /// </remarks>
    internal static class EditorUIBridge
    {
        private const string Assembly = ", Base.EditorUIPackage.Editor";
        private const string MetricsTypeName = "Base.EditorUIPackage.Editor.EditorMetrics" + Assembly;
        private const string MissingMemberFormat
            = "{0}: the Editor UI package has no {1}.{2}, so the installer is using its own value. "
            + "The package is installed but its palette moved, which means this bridge needs updating.";

        private const string PaletteTypeName = "Base.EditorUIPackage.Editor.EditorPalette" + Assembly;
        private const string ProviderTypeName = "Base.EditorUIPackage.Editor.EditorThemeProvider" + Assembly;
        private const string RevisionMemberName = "Revision";
        private const string TableTypeName = "Base.EditorUIPackage.Editor.EditorTableStyles" + Assembly;

        private static readonly Dictionary<string, Func<Color>> ColorGetters = new();
        private static readonly Dictionary<string, Func<float>> FloatGetters = new();
        private static readonly Dictionary<string, Func<int>> IntGetters = new();
        private static readonly HashSet<string> Reported = new();

        private static readonly Type MetricsType = Type.GetType(MetricsTypeName, false);
        private static readonly Type PaletteType = Type.GetType(PaletteTypeName, false);
        private static readonly Type ProviderType = Type.GetType(ProviderTypeName, false);
        private static readonly Type TableType = Type.GetType(TableTypeName, false);

        private static Func<int> _revision;

        /// <summary>True when the Editor UI package is installed and its palette could be found.</summary>
        internal static bool IsAvailable => PaletteType != null;

        /// <summary>
        /// Counts up whenever the active theme changes, so a style cache knows to rebuild. Stays at
        /// zero while the package is absent, which is also the right answer: nothing can change.
        /// </summary>
        internal static int Revision
        {
            get
            {
                if (ProviderType == null)
                    return 0;

                _revision ??= Resolve<int>(ProviderType, RevisionMemberName);

                return _revision != null
                    ? _revision()
                    : 0;
            }
        }

        /// <summary>Reads a color from the shared palette.</summary>
        /// <param name="member">Name of the property on the Editor UI palette.</param>
        /// <param name="fallback">The installer's own value, used when the package is absent.</param>
        /// <returns>The themed color, or the fallback.</returns>
        internal static Color PaletteColor(string member, Color fallback)
            => Read(ColorGetters, PaletteType, member, fallback);

        /// <summary>Reads a badge fill from the shared table styles.</summary>
        /// <param name="member">Name of the property on the Editor UI table styles.</param>
        /// <param name="fallback">The installer's own value, used when the package is absent.</param>
        /// <returns>The themed color, or the fallback.</returns>
        internal static Color TableColor(string member, Color fallback)
            => Read(ColorGetters, TableType, member, fallback);

        /// <summary>Reads a size or a spacing from the shared metrics.</summary>
        /// <param name="member">Name of the property on the Editor UI metrics.</param>
        /// <param name="fallback">The installer's own value, used when the package is absent.</param>
        /// <returns>The themed value, or the fallback.</returns>
        internal static float Metric(string member, float fallback)
            => Read(FloatGetters, MetricsType, member, fallback);

        /// <summary>Reads a corner radius or a font size from the shared metrics.</summary>
        /// <param name="member">Name of the property on the Editor UI metrics.</param>
        /// <param name="fallback">The installer's own value, used when the package is absent.</param>
        /// <returns>The themed value, or the fallback.</returns>
        internal static int Metric(string member, int fallback)
            => Read(IntGetters, MetricsType, member, fallback);

        private static T Read<T>(Dictionary<string, Func<T>> cache, Type owner, string member, T fallback)
        {
            Func<T> getter = Get(cache, owner, member);

            return getter != null
                ? getter()
                : fallback;
        }

        // A miss is cached as a null entry, so a member that does not resolve costs one lookup per
        // domain reload rather than one per repaint.
        private static Func<T> Get<T>(Dictionary<string, Func<T>> cache, Type owner, string member)
        {
            if (owner == null)
                return null;

            string key = owner.Name + "." + member;

            if (cache.TryGetValue(key, out Func<T> cached))
                return cached;

            Func<T> getter = Resolve<T>(owner, member);
            cache[key] = getter;

            if (getter == null)
                Report(key, owner, member);

            return getter;
        }

        private static Func<T> Resolve<T>(Type owner, string member)
        {
            PropertyInfo property = owner.GetProperty(member, BindingFlags.Static | BindingFlags.Public);

            if (property == null || property.PropertyType != typeof(T))
                return null;

            MethodInfo getter = property.GetGetMethod();

            return getter != null
                ? (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), getter, false)
                : null;
        }

        private static void Report(string key, Type owner, string member)
        {
            if (!Reported.Add(key))
                return;

            Debug.LogWarning(string.Format(MissingMemberFormat, nameof(EditorUIBridge), owner.Name, member));
        }
    }
}