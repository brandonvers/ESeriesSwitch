using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ESeriesSwitch.Localization;
using ESeriesSwitch.Services;

namespace ESeriesSwitch
{
    public partial class MainWindow : Window
    {
        const string RepositoryUrl = "https://github.com/brandonvers/ESeriesSwitch";

        static string Version => typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "";

        public MainWindow()
        {
            Loc.Instance.SetLanguage(AppSettings.LoadLanguage());
            InitializeComponent();
            VersionText.Text = "v" + Version;
            Loaded += (_, _) => RefreshStatus();
        }

        void RefreshStatus()
        {
            UpdateLanguageButtons();

            EnvStatus status;
            try
            {
                status = EnvironmentSwitcher.GetStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrReadEnv", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var (title, subtitle, brushKey) = status.Mode switch
            {
                ToolMode.Ista => (Loc.T("ModeIsta"), Loc.T("ModeIstaDesc"), "IstaBrush"),
                ToolMode.ESeries => (Loc.T("ModeESeries"), Loc.T("ModeESeriesDesc"), "ESeriesBrush"),
                _ => (Loc.T("ModeMixed"), Loc.T("ModeMixedDesc"), "MixedBrush")
            };

            var brush = (Brush)FindResource(brushKey);
            StatusTitle.Text = title;
            StatusSubtitle.Text = subtitle;
            StatusStripe.Background = brush;
            StatusDot.Fill = brush;

            ConfigDirText.Text = string.IsNullOrWhiteSpace(status.ConfigDirValue)
                ? Loc.T("NotSet")
                : "✓  " + status.ConfigDirValue;
            PathText.Text = status.PathContainsEdiabas
                ? Loc.T("PathContains", status.EdiabasBin)
                : Loc.T("PathMissing");

            ToESeriesButton.Visibility = status.Mode == ToolMode.ESeries ? Visibility.Collapsed : Visibility.Visible;
            ToIstaButton.Visibility = status.Mode == ToolMode.Ista ? Visibility.Collapsed : Visibility.Visible;

            WarningText.Text = string.Join("\n", status.Warnings.Select(w => "⚠  " + w));
            WarningBox.Visibility = status.Warnings.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            RefreshInterface();
        }

        void RefreshInterface()
        {
            string? value;
            try
            {
                value = EdiabasConfig.ReadInterface();
            }
            catch (Exception ex)
            {
                InterfaceCard.Visibility = Visibility.Visible;
                InterfaceTitle.Text = Loc.T("InterfaceUnreadable");
                InterfaceValue.Text = EdiabasConfig.IniPath;
                InterfaceHint.Text = ex.Message;
                IcomButton.IsEnabled = OfflineButton.IsEnabled = false;
                return;
            }

            if (value == null)
            {
                InterfaceCard.Visibility = Visibility.Collapsed;
                return;
            }

            InterfaceCard.Visibility = Visibility.Visible;
            InterfaceValue.Text = "Interface = " + value;

            bool isIcom = value.Equals(EdiabasConfig.IcomInterface, StringComparison.OrdinalIgnoreCase);
            bool isOffline = value.Equals(EdiabasConfig.OfflineInterface, StringComparison.OrdinalIgnoreCase);

            if (isIcom)
            {
                InterfaceTitle.Text = Loc.T("InterfaceIcomTitle");
                InterfaceHint.Text = Loc.T("InterfaceHintIcom");
            }
            else if (isOffline)
            {
                InterfaceTitle.Text = Loc.T("InterfaceOfflineTitle");
                InterfaceHint.Text = Loc.T("InterfaceHintOffline");
            }
            else
            {
                InterfaceTitle.Text = Loc.T("InterfaceOtherTitle");
                InterfaceHint.Text = Loc.T("InterfaceHintOther");
            }

            // The button of the active setting is disabled, the other one is highlighted
            SetSegment(IcomButton, isIcom);
            SetSegment(OfflineButton, isOffline);
        }

        void SetSegment(Button button, bool active)
        {
            button.IsEnabled = !active;
            button.Style = active ? null : (Style)FindResource("AccentButtonStyle");
        }

        void UpdateLanguageButtons()
        {
            // Here the active language is the highlighted one
            bool hungarian = Loc.Instance.Language == AppLanguage.Hungarian;
            EnglishButton.Style = hungarian ? null : (Style)FindResource("AccentButtonStyle");
            HungarianButton.Style = hungarian ? (Style)FindResource("AccentButtonStyle") : null;
        }

        void English_Click(object sender, RoutedEventArgs e) => ChangeLanguage(AppLanguage.English);

        void Hungarian_Click(object sender, RoutedEventArgs e) => ChangeLanguage(AppLanguage.Hungarian);

        void ChangeLanguage(AppLanguage language)
        {
            Loc.Instance.SetLanguage(language);
            AppSettings.SaveLanguage(language);
            RefreshStatus();
        }

        void Icom_Click(object sender, RoutedEventArgs e) => SetInterface(EdiabasConfig.IcomInterface);

        void Offline_Click(object sender, RoutedEventArgs e) => SetInterface(EdiabasConfig.OfflineInterface);

        void SetInterface(string value)
        {
            try
            {
                EdiabasConfig.SetInterface(value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrIniWrite", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshInterface();
        }

        async void ToESeries_Click(object sender, RoutedEventArgs e) => await SwitchTo(ToolMode.ESeries);

        async void ToIsta_Click(object sender, RoutedEventArgs e) => await SwitchTo(ToolMode.Ista);

        async Task SwitchTo(ToolMode target)
        {
            SetBusy(true);
            try
            {
                await Task.Run(() => EnvironmentSwitcher.SwitchTo(target));
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(this, Loc.T("ErrNoPermission"), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrSwitchFailed", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                SetBusy(false);
                RefreshStatus();
            }

            RebootBanner.Visibility = Visibility.Visible;

            var modeName = Loc.T(target == ToolMode.ESeries ? "SwitchedESeries" : "SwitchedIsta");
            var icomNote = target == ToolMode.ESeries && IsIcomInterface() ? Loc.T("IcomNote") : "";
            var answer = MessageBox.Show(this, Loc.T("SwitchSuccess", modeName, icomNote),
                Title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer == MessageBoxResult.Yes)
                Restart();
        }

        static bool IsIcomInterface()
        {
            try
            {
                return string.Equals(EdiabasConfig.ReadInterface(), EdiabasConfig.IcomInterface, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        void Reboot_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show(this, Loc.T("ConfirmRestart"), Title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.Yes)
                Restart();
        }

        void Restart()
        {
            try
            {
                EnvironmentSwitcher.RestartComputer();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrRestart", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void Refresh_Click(object sender, RoutedEventArgs e) => RefreshStatus();

        void OpenBackups_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(EnvironmentSwitcher.BackupDir);
            Process.Start(new ProcessStartInfo("explorer.exe", EnvironmentSwitcher.BackupDir) { UseShellExecute = true });
        }

        void About_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show(this, Loc.T("AboutText", Version, RepositoryUrl),
                Title, MessageBoxButton.YesNo, MessageBoxImage.Information);
            // Via explorer.exe, so the browser does not inherit the app's administrator rights
            if (answer == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo("explorer.exe", RepositoryUrl) { UseShellExecute = true });
        }

        void SetBusy(bool busy)
        {
            ToESeriesButton.IsEnabled = !busy;
            ToIstaButton.IsEnabled = !busy;
            Cursor = busy ? Cursors.Wait : null;
        }
    }
}
