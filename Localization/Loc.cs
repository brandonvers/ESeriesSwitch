using System.ComponentModel;

namespace ESeriesSwitch.Localization
{
    public enum AppLanguage
    {
        English,
        Hungarian
    }

    /// <summary>
    /// Runtime-switchable UI strings.
    /// XAML binds to the indexer: {Binding [Key], Source={x:Static loc:Loc.Instance}}
    /// </summary>
    public sealed class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public AppLanguage Language { get; private set; } = AppLanguage.English;

        public string this[string key] => Get(key);

        public static string T(string key) => Instance.Get(key);

        public static string T(string key, params object?[] args) => string.Format(Instance.Get(key), args);

        public void SetLanguage(AppLanguage language)
        {
            Language = language;
            // "Item[]" refreshes every indexer binding in the UI
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        }

        string Get(string key)
        {
            var table = Language == AppLanguage.Hungarian ? Strings.Hungarian : Strings.English;
            if (table.TryGetValue(key, out var value))
                return value;
            return Strings.English.TryGetValue(key, out var fallback) ? fallback : key;
        }
    }
}
