using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using DArvis.Extensions;
using DArvis.Models;
using DArvis.Services.Logging;
using DArvis.Settings;
using DArvis.Win32;

namespace DArvis.Views
{
    public partial class SettingsWindow : Window
    {
        public static readonly int GeneralTabIndex = 0;
        public static readonly int UserInterfaceTabIndex = 1;
        public static readonly int GameClientTabIndex = 2;
        public static readonly int AllMacrosTabIndex = 3;
        public static readonly int SkillMacrosTabIndex = 4;
        public static readonly int SpellMacrosTabIndex = 5;
        public static readonly int FloweringTabIndex = 6;
        public static readonly int UpdatesTabIndex = 7;
        public static readonly int DebugTabIndex = 8;
        public static readonly int AboutTabIndex = 9;

        private readonly ILogger logger;

        private Version currentVersion;
        private ReleaseVersion latestRelease;
        private bool isCheckingForVersion;

        public int SelectedTabIndex
        {
            get => (int)GetValue(SelectedTabIndexProperty);
            set => SetValue(SelectedTabIndexProperty, value);
        }

        public static readonly DependencyProperty SelectedTabIndexProperty =
            DependencyProperty.Register(nameof(SelectedTabIndex), typeof(int), typeof(SettingsWindow), new PropertyMetadata(0));

        public SettingsWindow()
        {
            logger = App.Current.Services.GetService<ILogger>();

            InitializeComponent();

            GetVersion();
        }

        private void GetVersion()
        {
            currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            versionText.Text = $"Version {currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}";
            frameworkVersionText.Text = $"{RuntimeInformation.FrameworkDescription}";
        }

        private void GetComputerInfo()
        {
            var cpuArch = RuntimeInformation.OSArchitecture.ToString();
            osVersionText.Text = $"{Environment.OSVersion} ({cpuArch})";

            var cpuCount = Environment.ProcessorCount;

            if (cpuCount == 8)
                cpuCountText.Text = "Octa-core";
            else if (cpuCount == 6)
                cpuCountText.Text = "Hexa-core";
            else if (cpuCount == 4)
                cpuCountText.Text = "Quad-core";
            else if (cpuCount == 2)
                cpuCountText.Text = "Dual-core";
            else if (cpuCount == 1)
                cpuCountText.Text = "Single-core";
            else
                cpuCountText.Text = $"{cpuCount}-core";

            if (NativeMethods.GetPhysicallyInstalledSystemMemory(out var ramKilobytes))
            {
                var mb = ramKilobytes / 1024.0;
                var gb = mb / 1024.0;

                if (gb >= 1)
                    memorySizeText.Text = $"{gb:0.0} GB RAM";
                else
                    memorySizeText.Text = $"{mb:0.0} MB RAM";
            }
            else
                memorySizeText.Text = string.Empty;
        }

        private void resetDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            bool? isOkToReset = this.ShowMessageBox("Reset Default Settings",
               "This will reset all settings to their default values.\nDo you wish to continue?",
               "This action cannot be undone.",
               MessageBoxButton.YesNo,
               460, 240);

            if (isOkToReset.Value)
                UserSettingsManager.Instance.Settings.ResetDefaults();
        }

        private async void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl tabControl)
                return;

            if (tabControl.SelectedItem is not TabItem tabItem)
            {
                Title = "Settings";
                return;
            }

            var headerName = (tabItem.Header as string).Replace("_", string.Empty);
            logger.LogInfo($"User has selected '{headerName}' tab in Settings window");

            Title = $"Settings - {headerName}";

            if (tabItem.TabIndex == AboutTabIndex)
            {
                GetComputerInfo();
            }
        }

        private void userManualLink_Click(object sender, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo(App.USER_MANUAL_URL) { UseShellExecute = true });
        
        private void locateClientButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Locate Game Client",
                Filter = "Executable Files (*.exe)|*.exe",
                CheckFileExists = true,
                CheckPathExists = true
            };

            var currentPath = UserSettingsManager.Instance.Settings.ClientPath;
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                dialog.InitialDirectory = currentPath;
            }

            if (!dialog.ShowDialog(this).GetValueOrDefault())
                return;

            UserSettingsManager.Instance.Settings.ClientPath = dialog.FileName;
        }
    }
}
