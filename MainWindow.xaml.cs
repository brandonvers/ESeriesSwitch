using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ESeriesSwitch.Services;

namespace ESeriesSwitch
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            VersionText.Text = $"v{typeof(MainWindow).Assembly.GetName().Version?.ToString(3)}";
            Loaded += (_, _) => RefreshStatus();
        }

        void RefreshStatus()
        {
            EnvStatus status;
            try
            {
                status = EnvironmentSwitcher.GetStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Nem sikerült beolvasni a környezeti változókat:\n\n" + ex.Message,
                    Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var (title, subtitle, brushKey) = status.Mode switch
            {
                ToolMode.Ista => ("Gyári ISTA+ mód",
                    "Az EDIABAS nincs a rendszer változói között, az ISTA+ használható.",
                    "IstaBrush"),
                ToolMode.ESeries => ("E-szériás mód",
                    "INPA / WinKFP / NCS Expert / Tool32 használható. Az ISTA+ ilyenkor nem fog rendesen működni.",
                    "ESeriesBrush"),
                _ => ("Részleges állapot",
                    "Csak az egyik beállítás van jelen. Válassz lent egy módot a rendbetételhez.",
                    "MixedBrush")
            };

            var brush = (Brush)FindResource(brushKey);
            StatusTitle.Text = title;
            StatusSubtitle.Text = subtitle;
            StatusStripe.Background = brush;
            StatusDot.Fill = brush;

            ConfigDirText.Text = string.IsNullOrWhiteSpace(status.ConfigDirValue)
                ? "✗  nincs beállítva"
                : "✓  " + status.ConfigDirValue;
            PathText.Text = status.PathContainsEdiabas
                ? "✓  tartalmazza: " + EnvironmentSwitcher.EdiabasBin
                : "✗  nem tartalmazza az EDIABAS mappát";

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
                InterfaceTitle.Text = "Nem olvasható";
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
                InterfaceTitle.Text = "ICOM";
                InterfaceHint.Text = "Az INPA / Tool32 indításkor az ICOM-ot keresi. Ha nincs csatlakoztatva, " +
                    "„NET-0009: TIMEOUT” hibát kapsz. Ilyenkor válts Offline-ra.";
            }
            else if (isOffline)
            {
                InterfaceTitle.Text = "Offline (nincs interfész)";
                InterfaceHint.Text = "Az INPA / Tool32 hibaüzenet nélkül indul, de az autóval nem tud kommunikálni. " +
                    "Diagnosztikához válts ICOM-ra.";
            }
            else
            {
                InterfaceTitle.Text = "Egyéb interfész";
                InterfaceHint.Text = "Az EDIABAS.INI-ben nem ICOM és nem NUL van beállítva.";
            }

            // Az aktív mód gombja inaktív, a másik kiemelt
            IcomButton.IsEnabled = !isIcom;
            OfflineButton.IsEnabled = !isOffline;
            IcomButton.Style = isIcom ? null : (Style)FindResource("AccentButtonStyle");
            OfflineButton.Style = isOffline ? null : (Style)FindResource("AccentButtonStyle");
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
                MessageBox.Show(this, "Az EDIABAS.INI módosítása nem sikerült:\n\n" + ex.Message,
                    Title, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(this, "Nincs jogosultság a rendszer környezeti változóinak módosításához.\n" +
                    "Indítsd az appot rendszergazdaként.", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "A váltás nem sikerült:\n\n" + ex.Message,
                    Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                SetBusy(false);
                RefreshStatus();
            }

            RebootBanner.Visibility = Visibility.Visible;

            var modeName = target == ToolMode.ESeries ? "E-szériás mód (INPA / WinKFP / EDIABAS)" : "gyári ISTA+ mód";
            var icomNote = target == ToolMode.ESeries && IsIcomInterface()
                ? "Az EDIABAS ICOM-ra van állítva: ICOM nélkül az INPA „NET-0009: TIMEOUT” hibát ad. " +
                  "Ha nincs ICOM a gépen, válts lent Offline-ra.\n\n"
                : "";
            var answer = MessageBox.Show(this,
                $"Sikeres váltás: {modeName}.\n\n" + icomNote +
                "Az újonnan indított programok már az új beállítást látják, de a biztos működéshez " +
                "(főleg az ISTA+ szolgáltatásai miatt) újraindítás ajánlott.\n\n" +
                "Újraindítod most a gépet?",
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
            var answer = MessageBox.Show(this, "Biztosan újraindítod a gépet? Mentsd el a nyitott munkáidat!",
                Title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
                MessageBox.Show(this, "Nem sikerült elindítani az újraindítást:\n\n" + ex.Message,
                    Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void Refresh_Click(object sender, RoutedEventArgs e) => RefreshStatus();

        void OpenBackups_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(EnvironmentSwitcher.BackupDir);
            Process.Start(new ProcessStartInfo("explorer.exe", EnvironmentSwitcher.BackupDir) { UseShellExecute = true });
        }

        void SetBusy(bool busy)
        {
            ToESeriesButton.IsEnabled = !busy;
            ToIstaButton.IsEnabled = !busy;
            Cursor = busy ? Cursors.Wait : null;
        }
    }
}
