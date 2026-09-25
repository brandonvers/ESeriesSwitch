using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ESeriesSwitch.Services
{
    /// <summary>
    /// Az EDIABAS.INI [Configuration] szekciójában lévő "Interface" sor olvasása és átírása.
    /// ICOM módban (RPLUS:ICOM_P) az EDIABAS indításkor az ICOM-ot keresi, és ha nincs
    /// csatlakoztatva, az INPA / Tool32 NET-0009 TIMEOUT hibát ad. NUL-lal hibaüzenet nélkül indulnak.
    /// </summary>
    public static class EdiabasConfig
    {
        public const string IcomInterface = "RPLUS:ICOM_P";
        public const string OfflineInterface = "NUL";

        public static string IniPath => Path.Combine(EnvironmentSwitcher.EdiabasBin, "EDIABAS.INI");

        // Latin1: bájtra pontos oda-vissza alakítás, így a fájl többi része (ékezetek, sorvégek) érintetlen marad.
        static readonly Encoding FileEncoding = Encoding.Latin1;

        static readonly Regex SectionRegex = new(@"^\[Configuration\][ \t]*\r?$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        static readonly Regex NextSectionRegex = new(@"^\[", RegexOptions.Multiline);
        static readonly Regex InterfaceRegex = new(@"^[ \t]*Interface[ \t]*=(?<value>[^\r\n;]*)",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        /// <summary>Az aktuális Interface érték, vagy null, ha az INI nem található / nincs benne ilyen sor.</summary>
        public static string? ReadInterface()
        {
            if (!File.Exists(IniPath))
                return null;
            var text = FileEncoding.GetString(File.ReadAllBytes(IniPath));
            var match = FindInterface(text);
            return match?.Groups["value"].Value.Trim();
        }

        /// <summary>Átírja az Interface értékét. Előtte mentést készít az INI-ről.</summary>
        public static void SetInterface(string value)
        {
            var text = FileEncoding.GetString(File.ReadAllBytes(IniPath));
            var match = FindInterface(text)
                ?? throw new InvalidOperationException("Az EDIABAS.INI [Configuration] részében nem található 'Interface' sor.");

            Directory.CreateDirectory(EnvironmentSwitcher.BackupDir);
            File.Copy(IniPath, Path.Combine(EnvironmentSwitcher.BackupDir, $"EDIABAS_{DateTime.Now:yyyyMMdd_HHmmss}.INI"), overwrite: true);

            // Az érték körüli szóközöket (és egy esetleges ; megjegyzés előtti részt) meghagyjuk
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
