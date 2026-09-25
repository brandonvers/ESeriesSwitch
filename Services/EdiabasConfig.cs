using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ESeriesSwitch.Localization;

namespace ESeriesSwitch.Services
{
    /// <summary>
    /// Reads and rewrites the "Interface" line in the [Configuration] section of EDIABAS.INI.
    /// In ICOM mode (RPLUS:ICOM_P) EDIABAS looks for the ICOM on startup, and without a connected ICOM
    /// INPA / Tool32 show a NET-0009 TIMEOUT error. With NUL they start without an error.
    /// </summary>
    public static class EdiabasConfig
    {
        public const string IcomInterface = "RPLUS:ICOM_P";
        public const string OfflineInterface = "NUL";

        public static string IniPath => Path.Combine(EnvironmentSwitcher.EdiabasBin, "EDIABAS.INI");

        // Latin1 round-trips every byte, so the rest of the file (accents, line endings) stays untouched.
        static readonly Encoding FileEncoding = Encoding.Latin1;

        static readonly Regex SectionRegex = new(@"^\[Configuration\][ \t]*\r?$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        static readonly Regex NextSectionRegex = new(@"^\[", RegexOptions.Multiline);
        static readonly Regex InterfaceRegex = new(@"^[ \t]*Interface[ \t]*=(?<value>[^\r\n;]*)",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        /// <summary>The current Interface value, or null if the INI or the line does not exist.</summary>
        public static string? ReadInterface()
        {
            if (!File.Exists(IniPath))
                return null;
            var text = FileEncoding.GetString(File.ReadAllBytes(IniPath));
            var match = FindInterface(text);
            return match?.Groups["value"].Value.Trim();
        }

        /// <summary>Rewrites the Interface value. Makes a backup copy of the INI first.</summary>
        public static void SetInterface(string value)
        {
            var text = FileEncoding.GetString(File.ReadAllBytes(IniPath));
            var match = FindInterface(text)
                ?? throw new InvalidOperationException(Loc.T("ErrInterfaceLineMissing"));

            Directory.CreateDirectory(EnvironmentSwitcher.BackupDir);
            File.Copy(IniPath, Path.Combine(EnvironmentSwitcher.BackupDir, $"EDIABAS_{DateTime.Now:yyyyMMdd_HHmmss}.INI"), overwrite: true);

            // Keep the whitespace around the value (and anything before a ; comment)
            var group = match.Groups["value"];
            var old = group.Value;
            var leading = old[..(old.Length - old.TrimStart().Length)];
            var trailing = old[old.TrimEnd().Length..];
            var updated = text[..group.Index] + leading + value + trailing + text[(group.Index + group.Length)..];
            File.WriteAllBytes(IniPath, FileEncoding.GetBytes(updated));
        }

        static Match? FindInterface(string text)
        {
            var section = SectionRegex.Match(text);
            if (!section.Success)
                return null;

            int start = section.Index + section.Length;
            var next = NextSectionRegex.Match(text, start);
            int end = next.Success ? next.Index : text.Length;

            var match = InterfaceRegex.Match(text, start);
            return match.Success && match.Index < end ? match : null;
        }
    }
}
