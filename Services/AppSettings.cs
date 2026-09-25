using System.Globalization;
using System.IO;
using System.Text.Json;
using ESeriesSwitch.Localization;

namespace ESeriesSwitch.Services
{
    /// <summary>
    /// App preferences, stored next to the backups in the app's own folder:
    /// C:\ProgramData\ESeriesSwitch\settings.json
    /// </summary>
    public static class AppSettings
    {
        static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ESeriesSwitch", "settings.json");

        sealed class Data
        {
            public string? Language { get; set; }
        }

        /// <summary>Saved language, or Hungarian on a Hungarian Windows and English everywhere else.</summary>
        public static AppLanguage LoadLanguage()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var data = JsonSerializer.Deserialize<Data>(File.ReadAllText(FilePath));
                    if (Enum.TryParse<AppLanguage>(data?.Language, out var saved))
                        return saved;
                }
            }
            catch
            {
                // A corrupt settings file must never stop the app from starting
            }

            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "hu"
                ? AppLanguage.Hungarian
                : AppLanguage.English;
        }

        public static void SaveLanguage(AppLanguage language)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(new Data { Language = language.ToString() }));
            }
            catch
            {
                // Not being able to remember the language is not worth an error dialog
            }
        }
    }
}
