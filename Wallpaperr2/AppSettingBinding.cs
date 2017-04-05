namespace Wallpaperr2
{
    using System.Windows.Data;

    class AppSettingBinding : Binding
    {
        public AppSettingBinding() => this.Source = Properties.Settings.Default;
    }
}
