namespace ESeriesSwitch.Services
{
    public static class AppInfo
    {
        public const string RepositoryUrl = "https://github.com/brandonvers/ESeriesSwitch";
        public const string LatestReleaseApiUrl = "https://api.github.com/repos/brandonvers/ESeriesSwitch/releases/latest";

        public static Version Version { get; } = Normalize(typeof(AppInfo).Assembly.GetName().Version ?? new Version(0, 0, 0));

        public static string VersionText => Version.ToString(3);

        /// <summary>Major.Minor.Build only, so that 1.2.0 and 1.2.0.0 compare as equal.</summary>
        public static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0));
    }
}
