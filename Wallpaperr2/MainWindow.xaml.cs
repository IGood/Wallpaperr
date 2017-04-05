namespace Wallpaperr2
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    using Goop.Wpf;
    using Cmd = Goop.Wpf.RoutedCommandUtilities<MainWindow>;
    using DP = Goop.Wpf.DependencyPropertyUtilities<MainWindow>;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static RoutedUICommand AddFiles = Cmd.CreateUI("Add _Files...", nameof(AddFiles), new KeyGesture(Key.O, ModifierKeys.Control));
        public static RoutedUICommand AddFolder = Cmd.CreateUI("Add Folde_r...", nameof(AddFolder), new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift));
        public static RoutedUICommand NewRandomWallpaper = Cmd.CreateUI("New Random _Wallpaper", nameof(NewRandomWallpaper), new KeyGesture(Key.F5));
        public static RoutedUICommand Quit = Cmd.CreateUI("_Quit", nameof(Quit));
        public static RoutedUICommand About = Cmd.CreateUI("_About Wallpaperr", nameof(About));
        public static RoutedUICommand ShowInExplorer = Cmd.CreateUI("Show In _Explorer", nameof(ShowInExplorer));
        public static RoutedCommand TogglePaused = Cmd.Create(nameof(TogglePaused));

        private bool closeToTray = true;

        public MainWindow()
        {
            this.TrayIconCommand = ApplicationCommands.Properties;

            this.BackgroundStyle.PropertyChanged += delegate
            {
                Properties.Settings.Default.BackgroundStyle = this.BackgroundStyle;
            };

            this.InitializeComponent();
        }

        public static DependencyProperty TrayIconCommandProperty = DP.Register(_ => _.TrayIconCommand);
        public ICommand TrayIconCommand
        {
            get => (ICommand)(this.GetValue(TrayIconCommandProperty));
            set => this.SetValue(TrayIconCommandProperty, value);
        }

        public BindableEnum<BackgroundStyle> BackgroundStyle { get; } = BindableEnum.Create(Wallpaperr2.BackgroundStyle.Spiffy);

        private void root_OnClosing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Reload();

            if (this.closeToTray)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void Quit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.closeToTray = false;
            this.Close();
        }

        private void DeleteItem_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.listView.SelectedItem != null;
            e.Handled = true;
        }

        private void DeleteItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void Properties_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
    }
}
