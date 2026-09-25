using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace ESeriesSwitch.Services
{
    public enum ToolMode
    {
        /// <summary>Gyári ISTA+: nincs EDIABAS a rendszer környezeti változóiban.</summary>
        Ista,
        /// <summary>Régi E-szériás toolok: EDIABAS_CONFIG_DIR és PATH beállítva.</summary>
        ESeries,
        /// <summary>Csak az egyik beállítás van jelen.</summary>
        Mixed
    }

    public sealed record EnvStatus(
        ToolMode Mode,
        string? ConfigDirValue,
        bool PathContainsEdiabas,
        IReadOnlyList<string> Warnings);

    /// <summary>
    /// A rendszerszintű (HKLM) környezeti változókat kapcsolja a régi E-szériás toolok
    /// (INPA, WinKFP, NCS Expert, Tool32) és a gyári ISTA+ között.
    /// </summary>
    public static class EnvironmentSwitcher
    {
        // Pontosan így (kisbetűvel) szerepelt eredetileg a gépen; a felismerés kis-nagybetű független.
        public const string EdiabasBin = @"c:\ec-apps\ediabas\bin";
        public const string ConfigDirName = "ediabas_config_dir";

        const string SystemEnvKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
        const string UserEnvKey = "Environment";
        const string PathName = "Path";

        public static string BackupDir { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ESeriesSwitch", "Backups");

        public static EnvStatus GetStatus()
        {
            var warnings = new List<string>();

            using var key = Registry.LocalMachine.OpenSubKey(SystemEnvKey, writable: false)
                ?? throw new InvalidOperationException("A rendszer környezeti változóinak kulcsa nem olvasható.");

            var configDir = ReadString(key, ConfigDirName);
            var path = ReadString(key, PathName) ?? "";
            bool hasVar = !string.IsNullOrWhiteSpace(configDir);
            bool inPath = SplitPath(path).Any(IsEdiabasEntry);

            var mode = (hasVar, inPath) switch
            {
                (true, true) => ToolMode.ESeries,
                (false, false) => ToolMode.Ista,
                _ => ToolMode.Mixed
            };

            if (hasVar && !IsEdiabasEntry(configDir!))
                warnings.Add($"Az {ConfigDirName} értéke eltér a megszokottól: {configDir}");

            if (!Directory.Exists(EdiabasBin))
                warnings.Add($"A(z) {EdiabasBin} mappa nem létezik ezen a gépen.");

            // A felhasználói szintű változók felülírhatják a rendszerszintűeket, ezeket nem módosítjuk, csak jelezzük.
            using (var userKey = Registry.CurrentUser.OpenSubKey(UserEnvKey, writable: false))
            {
                if (userKey != null)
                {
                    if (!string.IsNullOrWhiteSpace(ReadString(userKey, ConfigDirName)))
                        warnings.Add($"A felhasználói változók között is van {ConfigDirName}. Ezt az app nem módosítja, érdemes kézzel törölni.");
                    if (SplitPath(ReadString(userKey, PathName) ?? "").Any(IsEdiabasEntry))
                        warnings.Add("A felhasználói PATH is tartalmazza az EDIABAS mappát. Ezt az app nem módosítja, érdemes kézzel törölni.");
                }
            }

            return new EnvStatus(mode, configDir, inPath, warnings);
        }

        /// <summary>Átkapcsol a megadott módra. Visszaadja a mentés fájl útvonalát.</summary>
        public static string SwitchTo(ToolMode target)
        {
            if (target == ToolMode.Mixed)
                throw new ArgumentException("Vegyes állapotra nem lehet váltani.", nameof(target));

            using var key = Registry.LocalMachine.OpenSubKey(SystemEnvKey, writable: true)
                ?? throw new InvalidOperationException("A rendszer környezeti változóinak kulcsa nem írható.");

            var path = ReadString(key, PathName) ?? "";
            // A PATH típusát (általában REG_EXPAND_SZ) meg kell tartani, különben a %SystemRoot% és társai eltörnek.
            var pathKind = key.GetValueNames().Contains(PathName, StringComparer.OrdinalIgnoreCase)
                ? key.GetValueKind(PathName)
                : RegistryValueKind.ExpandString;
            var configDir = ReadString(key, ConfigDirName);

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

        static string? ReadString(RegistryKey key, string name) =>
            key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;

        // Az üres elemeket is megtartjuk, hogy a többi bejegyzéshez egyáltalán ne nyúljunk.
        static string[] SplitPath(string path) => path.Length == 0 ? [] : path.Split(';');

        static bool IsEdiabasEntry(string entry)
        {
            var normalized = Environment.ExpandEnvironmentVariables(entry.Trim().Trim('"')).TrimEnd('\\');
            return string.Equals(normalized, EdiabasBin, StringComparison.OrdinalIgnoreCase);
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

        // Szól a Windowsnak (Explorer stb.), hogy változtak a környezeti változók,
        // így az újonnan indított programok már az új értékeket kapják.
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
