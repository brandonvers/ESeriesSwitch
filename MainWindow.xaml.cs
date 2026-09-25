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
            var answer = MessageBox.Show(this,
                $"Sikeres váltás: {modeName}.\n\n" +
                "Az újonnan indított programok már az új beállítást látják, de a biztos működéshez " +
                "(főleg az ISTA+ szolgáltatásai miatt) újraindítás ajánlott.\n\n" +
                "Újraindítod most a gépet?",
                Title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer == MessageBoxResult.Yes)
                Restart();
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
