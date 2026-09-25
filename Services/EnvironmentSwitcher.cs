using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using ESeriesSwitch.Localization;
using Microsoft.Win32;

namespace ESeriesSwitch.Services
{
    public enum ToolMode
    {
        /// <summary>Factory ISTA+: EDIABAS is not in the system environment variables.</summary>
        Ista,
        /// <summary>Legacy E-series tools: EDIABAS config variable and PATH entry are both set.</summary>
        ESeries,
        /// <summary>Only one of the two settings is present.</summary>
        Mixed
    }

    public sealed record EnvStatus(
        ToolMode Mode,
        string EdiabasBin,
        string? ConfigDirValue,
        bool PathContainsEdiabas,
        IReadOnlyList<string> Warnings);

    /// <summary>
    /// Switches the system-wide (HKLM) environment variables between the legacy E-series tools
    /// (INPA, WinKFP, NCS Expert, Tool32) and the factory ISTA+.
    /// </summary>
    public static class EnvironmentSwitcher
    {
        public const string ConfigDirName = "ediabas_config_dir";

        // Known EDIABAS install locations, in order of preference. The first one that exists is used
        // when the config variable is not set (e.g. while ISTA+ mode is active).
        static readonly string[] KnownEdiabasBins =
        [
            @"c:\ec-apps\ediabas\bin",
            @"C:\EDIABAS\BIN",
        ];

        const string SystemEnvKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
        const string UserEnvKey = "Environment";
        const string PathName = "Path";

        public static string BackupDir { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ESeriesSwitch", "Backups");

        /// <summary>The EDIABAS\BIN folder the app works with. Refreshed by <see cref="GetStatus"/>.</summary>
        public static string EdiabasBin { get; private set; } = KnownEdiabasBins[0];

        public static EnvStatus GetStatus()
        {
            var warnings = new List<string>();

            using var key = Registry.LocalMachine.OpenSubKey(SystemEnvKey, writable: false)
                ?? throw new InvalidOperationException(Loc.T("ErrEnvKeyRead"));

            var configDir = ReadString(key, ConfigDirName);
            EdiabasBin = DetectEdiabasBin(configDir);

            var path = ReadString(key, PathName) ?? "";
            bool hasVar = !string.IsNullOrWhiteSpace(configDir);
            bool inPath = SplitPath(path).Any(IsEdiabasEntry);

            var mode = (hasVar, inPath) switch
            {
                (true, true) => ToolMode.ESeries,
                (false, false) => ToolMode.Ista,
                _ => ToolMode.Mixed
            };

            if (hasVar && !Directory.Exists(Expand(configDir!)))
                warnings.Add(Loc.T("WarnConfigDirMissingFolder", ConfigDirName, configDir));
            else if (!Directory.Exists(EdiabasBin))
                warnings.Add(Loc.T("WarnFolderMissing", EdiabasBin));

            // User-level variables can override the system ones. They are only reported, never changed.
            using (var userKey = Registry.CurrentUser.OpenSubKey(UserEnvKey, writable: false))
            {
                if (userKey != null)
                {
                    if (!string.IsNullOrWhiteSpace(ReadString(userKey, ConfigDirName)))
                        warnings.Add(Loc.T("WarnUserVar", ConfigDirName));
                    if (SplitPath(ReadString(userKey, PathName) ?? "").Any(IsEdiabasEntry))
                        warnings.Add(Loc.T("WarnUserPath"));
                }
            }

            return new EnvStatus(mode, EdiabasBin, configDir, inPath, warnings);
        }

        /// <summary>Switches to the given mode. Returns the path of the backup file.</summary>
        public static string SwitchTo(ToolMode target)
        {
            if (target == ToolMode.Mixed)
                throw new ArgumentException("Cannot switch to the mixed state.", nameof(target));

            using var key = Registry.LocalMachine.OpenSubKey(SystemEnvKey, writable: true)
                ?? throw new InvalidOperationException(Loc.T("ErrEnvKeyWrite"));

            var path = ReadString(key, PathName) ?? "";
            // Keep the value type of PATH (normally REG_EXPAND_SZ), otherwise %SystemRoot% and friends stop expanding.
            var pathKind = key.GetValueNames().Contains(PathName, StringComparer.OrdinalIgnoreCase)
                ? key.GetValueKind(PathName)
                : RegistryValueKind.ExpandString;
            var configDir = ReadString(key, ConfigDirName);
            EdiabasBin = DetectEdiabasBin(configDir);

            var backupFile = WriteBackup(target, path, pathKind, configDir);

            if (target == ToolMode.ESeries)
            {
                key.SetValue(ConfigDirName, EdiabasBin, RegistryValueKind.String);
                if (!SplitPath(path).Any(IsEdiabasEntry))
                {
                    var separator = path.Length == 0 || path.EndsWith(';') ? "" : ";";
                    key.SetValue(PathName, path + separator + EdiabasBin, pathKind);
                }
            }
            else
            {
                key.DeleteValue(ConfigDirName, throwOnMissingValue: false);
                var kept = SplitPath(path).Where(e => !IsEdiabasEntry(e));
                key.SetValue(PathName, string.Join(';', kept), pathKind);
            }

            BroadcastEnvironmentChange();
            return backupFile;
        }

        public static void RestartComputer()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("shutdown", "/r /t 5")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        // An existing config variable wins (keeping its exact spelling), then the first known folder that exists.
        static string DetectEdiabasBin(string? configDir)
        {
            if (!string.IsNullOrWhiteSpace(configDir) && Directory.Exists(Expand(configDir)))
                return configDir.Trim();
            return KnownEdiabasBins.FirstOrDefault(Directory.Exists) ?? KnownEdiabasBins[0];
        }

        static string? ReadString(RegistryKey key, string name) =>
            key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;

        // Empty entries are kept as well, so that the rest of PATH is left exactly as it was.
        static string[] SplitPath(string path) => path.Length == 0 ? [] : path.Split(';');

        static string Expand(string value) => Environment.ExpandEnvironmentVariables(value.Trim().Trim('"')).TrimEnd('\\');

        // Any known EDIABAS folder counts, so switching to ISTA+ removes all of them from PATH.
        static bool IsEdiabasEntry(string entry)
        {
            if (entry.Trim().Length == 0)
                return false;
            var normalized = Expand(entry);
            return KnownEdiabasBins.Append(EdiabasBin)
                .Any(bin => string.Equals(normalized, Expand(bin), StringComparison.OrdinalIgnoreCase));
        }

        static string WriteBackup(ToolMode target, string path, RegistryValueKind pathKind, string? configDir)
        {
            Directory.CreateDirectory(BackupDir);
            var file = Path.Combine(BackupDir, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var backup = new
            {
                Timestamp = DateTime.Now,
                SwitchingTo = target.ToString(),
                RegistryKey = @"HKLM\" + SystemEnvKey,
                Path = path,
                PathKind = pathKind.ToString(),
                EdiabasConfigDir = configDir
            };
            File.WriteAllText(file, JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true }));
            return file;
        }

        // Tells Windows (Explorer etc.) that the environment changed,
        // so newly started programs get the new values.
        static void BroadcastEnvironmentChange()
        {
            SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, "Environment",
                SMTO_ABORTIFHUNG, 5000, out _);
        }

        static readonly IntPtr HWND_BROADCAST = new(0xffff);
        const uint WM_SETTINGCHANGE = 0x001A;
        const uint SMTO_ABORTIFHUNG = 0x0002;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam,
            uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
    }
}
