﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using DArvis.Components;
using Microsoft.Win32;
using DArvis.Extensions;
using DArvis.IO;
using DArvis.IO.Process;
using DArvis.Macro;
using DArvis.Media;
using DArvis.Metadata;
using DArvis.Models;
using DArvis.Services.Logging;
using DArvis.Services.Serialization;
using DArvis.Settings;
using DArvis.Win32;
using Path = System.IO.Path;

namespace DArvis.Views
{
    public partial class MainWindow : Window, IDisposable
    {
        private const int WM_HOTKEY = 0x312;
        
        static TimeSpan UpdateSpan { get; set; }
        static DateTime LastUpdate { get; set; }
        static Thread _updatingThread;
        
        static List<UpdateableComponent> _components = new();
        
        private const string DArvisMacroFileExtension = "sh4";
        private const string DArvisMacroFileFilter = "DArvis v4 Macro Files (*.sh4)|*.sh4";

        private static readonly int IconPadding = 14;

        private readonly ILogger logger;
        private readonly IMacroStateSerializer macroStateSerializer;

        private bool isDisposed;
        private HwndSource windowSource;

        private int recentSettingsTabIndex;
        private MetadataEditorWindow metadataWindow;
        private SettingsWindow settingsWindow;

        private BackgroundWorker processScannerWorker;
        private BackgroundWorker clientUpdateWorker;
        private BackgroundWorker flowerUpdateWorker;

        private PlayerMacroState selectedMacro;

        public MainWindow()
        {
            logger = App.Current.Services.GetService<ILogger>();
            macroStateSerializer = App.Current.Services.GetService<IMacroStateSerializer>();

            InitializeLogger();
            InitializePacketManager();
            InitializeComponent();
            InitializeViews();

            LoadThemes();
            LoadSettings();
            ApplyTheme();
            UpdateListBoxGridWidths();

            LoadVersions();

            UpdateToolbarState();

            LoadSkills();
            LoadSpells();
            LoadStaves();
            CalculateLines();

            ToggleInventory(false);
            ToggleSkills(false);
            ToggleSpells(false);
            ToggleSpellQueue(false);
            
            RefreshSpellQueue();
            RefreshFlowerQueue();

            StartUpdateTimers();

            Loaded += MainWindow_Loaded;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                processScannerWorker?.Dispose();
                clientUpdateWorker?.Dispose();
                flowerUpdateWorker?.Dispose();
                // TODO: packetScannerWorker?.Dispose();
            }

            windowSource?.Dispose();

            isDisposed = true;
        }

        private void LaunchClient()
        {
            launchClientButton.IsEnabled = false;

            var clientPath = UserSettingsManager.Instance.Settings.ClientPath;

            logger.LogInfo($"Attempting to launch client executable: {clientPath}");

            try
            {
                if (!File.Exists(clientPath))
                {
                    logger.LogError("Client executable not found, unable to launch");
                    return;
                }

                var processInformation = StartClientProcess(clientPath);

                if (!ClientVersionManager.TryDetectClientVersion(processInformation.ProcessId, out var detectedVersion))
                {
                    logger.LogWarn("Unable to determine client version, using default version");
                    detectedVersion = ClientVersionManager.Instance.DefaultVersion;
                }
                else
                {
                    logger.LogInfo($"Detected client pid {processInformation.ProcessId} version as {detectedVersion.Key}");
                }

                if (detectedVersion != null)
                    PatchClient(processInformation, detectedVersion);
                else
                    logger.LogWarn($"No client version, unable to apply patches to pid {processInformation.ProcessId}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Unable to launch a new client! Path = {clientPath}");
                logger.LogException(ex);

                this.ShowMessageBox("Launch Client Failed",
                   ex.Message, "Check that the executable exists and the version is correct.",
                   MessageBoxButton.OK,
                   440,
                   280);
            }
            finally
            {
                launchClientButton.IsEnabled = true;
            }
        }

        private ProcessInformation StartClientProcess(string clientPath)
        {
            // Create Process
            var startupInfo = new StartupInfo { Size = Marshal.SizeOf(typeof(StartupInfo)) };

            var processSecurity = new SecurityAttributes();
            var threadSecurity = new SecurityAttributes();

            processSecurity.Size = Marshal.SizeOf(processSecurity);
            threadSecurity.Size = Marshal.SizeOf(threadSecurity);

            logger.LogInfo($"Attempting to create process for executable: {clientPath}");

            bool wasCreated = NativeMethods.CreateProcess(clientPath,
               null,
               ref processSecurity, ref threadSecurity,
               false,
               ProcessCreationFlags.Suspended,
               nint.Zero,
               null,
               ref startupInfo, out var processInformation);

            // Ensure the process was actually created
            if (!wasCreated || processInformation.ProcessId == 0)
            {
                var errorCode = Marshal.GetLastPInvokeError();
                var errorMessage = Marshal.GetLastPInvokeErrorMessage();
                logger.LogError($"Failed to create client process, code = {errorCode}, message = {errorMessage}");

                throw new Win32Exception(errorCode, "Unable to create client process");
            }
            else
            {
                logger.LogInfo($"Created client process successfully with pid {processInformation.ProcessId}");
            }

            return processInformation;
        }

        private void PatchClient(ProcessInformation process, ClientVersion version)
        {
            var patchMultipleInstances = UserSettingsManager.Instance.Settings.AllowMultipleInstances;
            var patchIntroVideo = UserSettingsManager.Instance.Settings.SkipIntroVideo;
            var patchNoWalls = UserSettingsManager.Instance.Settings.NoWalls;

            var pid = process.ProcessId;
            logger.LogInfo($"Attempting to patch client process {pid}, version = {version.Key}");

            try
            {
                // Patch Process
                using var accessor = new ProcessMemoryAccessor(pid, ProcessAccess.ReadWrite);
                using var patchStream = accessor.GetWriteableStream();
                using var writer = new BinaryWriter(patchStream, Encoding.ASCII, leaveOpen: true);

                if (patchMultipleInstances && version.MultipleInstanceAddress > 0)
                {
                    logger.LogInfo($"Applying multiple instance patch to process {pid} (0x{version.MultipleInstanceAddress:x8})");
                    patchStream.Position = version.MultipleInstanceAddress;
                    writer.Write((byte)0x31);        // XOR
                    writer.Write((byte)0xC0);        // EAX, EAX
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                }

                if (patchIntroVideo && version.IntroVideoAddress > 0)
                {
                    logger.LogInfo($"Applying skip intro video patch to process {pid} (0x{version.IntroVideoAddress:x8})");
                    patchStream.Position = version.IntroVideoAddress;
                    writer.Write((byte)0x83);        // CMP
                    writer.Write((byte)0xFA);        // EDX
                    writer.Write((byte)0x00);        // 0
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                    writer.Write((byte)0x90);        // NOP
                }

                if (patchNoWalls && version.NoWallAddress > 0)
                {
                    logger.LogInfo($"Applying no walls patch to process {pid} (0x{version.NoWallAddress:x8})");
                    patchStream.Position = version.NoWallAddress;
                    writer.Write((byte)0xEB);        // JMP SHORT
                    writer.Write((byte)0x17);        // +0x17
                    writer.Write((byte)0x90);        // NOP
                }
            }
            finally
            {
                // Resume and close handles.
                NativeMethods.ResumeThread(process.ThreadHandle);
                NativeMethods.CloseHandle(process.ThreadHandle);
                NativeMethods.CloseHandle(process.ProcessHandle);
            }
        }

        private void InitializeLogger()
        {
            if (!UserSettingsManager.Instance.Settings.LoggingEnabled)
                return;

            var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            var logFile = $"session-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";

            var logFilePath = Path.Combine(logsDirectory, logFile);
            logger.AddFileTransport(logFilePath);

            logger.LogInfo("Logging initialized");
        }

        private void InitializePacketManager()
        {
            PacketManager.RegisterConsumers();
        }
        private void InitializeHotkeyHook()
        {
            var helper = new WindowInteropHelper(this);
            windowSource = HwndSource.FromHwnd(helper.Handle);

            windowSource.AddHook(WindowMessageHook);
            logger.LogInfo("Hotkey hook initialized");
        }

        private void InitializeViews()
        {
            PlayerManager.Instance.PlayerAdded += OnPlayerCollectionAdd;
            PlayerManager.Instance.PlayerRemoved += OnPlayerCollectionRemove;

            PlayerManager.Instance.PlayerPropertyChanged += OnPlayerPropertyChanged;

            SpellMetadataManager.Instance.SpellAdded += OnSpellManagerUpdated;
            SpellMetadataManager.Instance.SpellChanged += OnSpellManagerUpdated;
            SpellMetadataManager.Instance.SpellRemoved += OnSpellManagerUpdated;
        }

        private void OnSpellManagerUpdated(object sender, SpellMetadataEventArgs e)
        {
            if (selectedMacro == null)
                return;

            foreach (var spell in selectedMacro.QueuedSpells)
                spell.IsUndefined = !SpellMetadataManager.Instance.ContainsSpell(spell.Name);
        }

        private void OnPlayerCollectionAdd(object sender, PlayerEventArgs e)
        {
            logger.LogInfo($"Game client process detected with pid: {e.Player.Process.ProcessId}");

            UpdateToolbarState();
            UpdateClientList();
        }

        private void OnPlayerCollectionRemove(object sender, PlayerEventArgs e)
        {
            logger.LogInfo($"Game client process removed with pid: {e.Player.Process.ProcessId}");

            OnPlayerLoggedOut(e.Player);

            UpdateToolbarState();
            UpdateClientList();

            if (selectedMacro != null && selectedMacro.Name == e.Player.Name)
                SelectNextAvailablePlayer();
        }

        private async void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not Player player)
                return;

            await Dispatcher.SwitchToUIThread();

            if (string.Equals(nameof(player.IsLoggedIn), e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (!player.IsLoggedIn)
                    OnPlayerLoggedOut(player);
                else
                    OnPlayerLoggedIn(player);
            }

            clientListBox.Items.Refresh();
            LeaderListBox.Items.Refresh();
            
            var selectedPlayer = clientListBox.SelectedItem as Player;

            if (player == selectedPlayer)
            {
                var supportsFlowering = selectedPlayer?.Version?.SupportsFlowering ?? false;
                var hasLyliacPlant = selectedPlayer?.HasLyliacPlant ?? false;
                var hasLyliacVineyard = selectedPlayer?.HasLyliacVineyard ?? false;

                ToggleInventory(selectedPlayer != null);
                ToggleSkills(selectedPlayer != null);
                ToggleSpells(selectedPlayer != null);
                ToggleFlower(supportsFlowering, hasLyliacPlant, hasLyliacVineyard);
            }
        }

        private async void OnPlayerLoggedIn(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

            await Dispatcher.SwitchToUIThread();

            if (!player.LoginTimestamp.HasValue)
                player.LoginTimestamp = DateTime.Now;

            UpdateClientList();

            logger.LogInfo($"Player logged in: {player.Name} (pid {player.Process.ProcessId})");

            if (!string.IsNullOrEmpty(player.Name))
                NativeMethods.SetWindowText(player.Process.WindowHandle, $"{player.Version.WindowTitle} - {player.Name}");

            var autosaveEnabled = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var state = MacroManager.Instance.GetMacroState(player);

            if (state != null)
            {
                state.StatusChanged += HandleMacroStatusChanged;
                state.Client.Updated += HandleClientUpdateTick;
            }

            if (autosaveEnabled && state != null)
            {
                logger.LogInfo($"Auto-loading {state.Client.Name} macro state...");
                AutoLoadMacroState(state);
            }
            
            // Set default spell queue rotation mode
            if (state.SpellQueueRotation == SpellRotationMode.Default)
                state.SpellQueueRotation = UserSettingsManager.Instance.Settings.SpellRotationMode;
        }

        private async void OnPlayerLoggedOut(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return;

            await Dispatcher.SwitchToUIThread();

            player.LoginTimestamp = null;
            UpdateClientList();

            logger.LogInfo($"Player logged out: {player.Name} (pid {player.Process.ProcessId})");

            NativeMethods.SetWindowText(player.Process.WindowHandle, player.Version.WindowTitle);

            var autosaveEnabled = UserSettingsManager.Instance.Settings.SaveMacroStates;
            var state = MacroManager.Instance.GetMacroState(player);

            if (autosaveEnabled && state != null)
            {
                logger.LogInfo($"Auto-saving {state.Client.Name} macro state...");
                AutoSaveMacroState(state);
            }

            if (player.HasHotkey)
                HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, player.Hotkey);

            player.Hotkey = null;
            
            if (state != null)
            {
                state.StatusChanged -= HandleMacroStatusChanged;
                state.Client.Updated -= HandleClientUpdateTick;

                state.ClearSpellQueue();
                state.ClearFlowerQueue();
                state.LocalStorage.Clear();
            }

            UpdateUIForSelectedClient(player.Name);
        }

        private void UpdateUIForSelectedClient(string lastSelectedName = "")
        {
            UpdateToolbarState();

            if (selectedMacro != null && selectedMacro.Name == lastSelectedName)
                SelectNextAvailablePlayer();

            if (!PlayerManager.Instance.LoggedInPlayers.Any())
                ToggleSpellQueue(false);
        }

        private void HandleMacroStatusChanged(object sender, MacroStatusEventArgs e) => UpdateToolbarState();

        private void HandleClientUpdateTick(object sender, EventArgs e)
        {
            if (selectedMacro == null || sender is not Player player)
                return;

            if (selectedMacro.Client != player)
                return;

            // Refresh the spell queue levels on tick
            foreach (var queuedSpell in selectedMacro.QueuedSpells)
            {
                var spell = player.Spellbook.GetSpell(queuedSpell.Name);
                if (spell is null)
                    continue;

                queuedSpell.MaximumLevel = spell.MaximumLevel;
                queuedSpell.CurrentLevel = spell.CurrentLevel;
                queuedSpell.IsOnCooldown = spell.IsOnCooldown;

                var isWaitingOnHealth = false;

                // Min health percentage (ex: > 90%), cannot use yet
                if (spell.MinHealthPercent.HasValue && spell.MinHealthPercent.Value >= player.Stats.HealthPercent)
                    isWaitingOnHealth = true;
                // Max health percentage (ex: < 2%), cannot use yet
                else if (spell.MaxHealthPercent.HasValue && player.Stats.HealthPercent > spell.MaxHealthPercent.Value)
                    isWaitingOnHealth = true;

                queuedSpell.IsWaitingOnHealth = isWaitingOnHealth;
            }
        }

        private void SelectNextAvailablePlayer()
        {
            if (!PlayerManager.Instance.LoggedInPlayers.Any())
            {
                clientListBox.SelectedItem = null;
                UpdateToolbarState();
                ToggleSpellQueue(false);
                return;
            }
        }

        private async void RefreshInventory()
        {
            await Dispatcher.SwitchToUIThread();

            // Do some stuff with inventory on UI thread
        }

        private async void RefreshSpellQueue()
        {
            await Dispatcher.SwitchToUIThread();

            if (selectedMacro != null)
                spellQueueRotationComboBox.SelectedValue = selectedMacro.SpellQueueRotation;

            var hasItemsInQueue = selectedMacro != null && selectedMacro.QueuedSpells.Count > 0;

            removeSelectedSpellButton.IsEnabled = hasItemsInQueue;
            removeAllSpellsButton.IsEnabled = hasItemsInQueue;

            spellQueueListBox.ItemsSource = selectedMacro?.QueuedSpells ?? null;
            spellQueueListBox.Items.Refresh();
        }

        private async void RefreshFlowerQueue()
        {
            await Dispatcher.SwitchToUIThread();

            var hasItemsInQueue = selectedMacro != null && selectedMacro.FlowerQueueCount > 0;

            removeSelectedFlowerTargetButton.IsEnabled = hasItemsInQueue;
            removeAllFlowerTargetsButton.IsEnabled = hasItemsInQueue;

            flowerListBox.ItemsSource = selectedMacro?.FlowerTargets ?? null;
            flowerListBox.Items.Refresh();
        }

        private void LoadVersions()
        {
            var versionsFile = Path.Combine(Environment.CurrentDirectory, ClientVersionManager.VersionsFile);
            logger.LogInfo($"Attempting to load client versions from file: {versionsFile}");

            try
            {
                if (File.Exists(versionsFile))
                {
                    ClientVersionManager.Instance.LoadFromFile(versionsFile);
                    logger.LogInfo("Client versions successfully loaded");

                    // Register all window class names so they can be detected
                    foreach (var version in ClientVersionManager.Instance.Versions)
                    {
                        if (string.IsNullOrWhiteSpace(version.WindowClassName))
                            continue;

                        ProcessManager.Instance.RegisterWindowClassName(version.WindowClassName);
                        logger.LogInfo($"Registered window class name: {version.WindowClassName} (version = {version.Key})");
                    }
                }
                else
                {
                    UpdateToolbarState();
                    logger.LogInfo("No client version file was found");

                    this.ShowMessageBox("Missing Client Versions File", "The client versions file was not found.\nUnable to start new clients.", "You should re-install the application.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load client versions");
                logger.LogException(ex);

                UpdateToolbarState();

                this.ShowMessageBox("Unable to Load Client Versions", "The client versions file could not be loaded.\nUnable to start new clients.", "You should re-install the application.");
            }
        }

        private void LoadThemes()
        {
            var themesFile = ColorThemeManager.ThemesFile;
            logger.LogInfo($"Attempting to load themes from file: {themesFile}");

            try
            {
                if (File.Exists(themesFile))
                {
                    ColorThemeManager.Instance.LoadFromFile(themesFile);
                    logger.LogInfo("Themes loaded successfully");
                }
                else
                {
                    logger.LogInfo("No themes file was found, using default theme");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load themes, resetting to default theme");
                logger.LogException(ex);
            }
        }

        private void LoadSettings()
        {
            var settingsFile = UserSettingsManager.SettingsFile;
            logger.LogInfo($"Attempting to user settings from file: {settingsFile}");

            try
            {
                if (File.Exists(settingsFile))
                {
                    UserSettingsManager.Instance.LoadFromFile(settingsFile);
                    logger.LogInfo("User settings loaded successfully");

                    if (string.IsNullOrWhiteSpace(UserSettingsManager.Instance.Settings.SelectedTheme))
                    {
                        logger.LogWarn("User settings does not have a selected theme, using default theme");
                        UserSettingsManager.Instance.Settings.SelectedTheme = ColorThemeManager.Instance.DefaultTheme?.Name;
                    }
                    else
                    {
                        var selectedTheme = UserSettingsManager.Instance.Settings.SelectedTheme;
                        if (!ColorThemeManager.Instance.ContainsTheme(selectedTheme))
                        {
                            logger.LogWarn($"User settings has an invalid theme selected: {selectedTheme}");
                            UserSettingsManager.Instance.Settings.SelectedTheme = ColorThemeManager.Instance.DefaultTheme?.Name;
                        }
                    }
                }
                else
                {
                    UserSettingsManager.Instance.Settings.ResetDefaults();
                    logger.LogInfo("No user settings file was found, using defaults");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load user settings, resetting to defaults");
                logger.LogException(ex);

                UserSettingsManager.Instance.Settings.ResetDefaults();
            }
            finally
            {
                UserSettingsManager.Instance.Settings.PropertyChanged += UserSettings_PropertyChanged;

                PlayerManager.Instance.SortOrder = UserSettingsManager.Instance.Settings.ClientSortOrder;
                PlayerManager.Instance.ShowAllClients = UserSettingsManager.Instance.Settings.ShowAllProcesses;
                UpdateClientList();
            }
        }

        private void LoadSkills()
        {
            var skillsFile = SkillMetadataManager.SkillMetadataFile;
            logger.LogInfo($"Attempting to skills metadata from file: {skillsFile}");

            try
            {
                if (File.Exists(skillsFile))
                {
                    SkillMetadataManager.Instance.LoadFromFile(skillsFile);
                    logger.LogInfo("Skill metadata loaded successfully");
                }
                else
                {
                    logger.LogWarn("No skills metadata file was found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load skills metadata");
                logger.LogException(ex);
            }
        }

        private void LoadSpells()
        {
            var spellsFile = SpellMetadataManager.SpellMetadataFile;
            logger.LogInfo($"Attempting to spells metadata from file: {spellsFile}");

            try
            {
                if (File.Exists(spellsFile))
                {
                    SpellMetadataManager.Instance.LoadFromFile(spellsFile);
                    logger.LogInfo("Spell metadata loaded successfully");
                }
                else
                {
                    logger.LogWarn("No spells metadata file was found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load spells metadata");
                logger.LogException(ex);
            }
        }

        private void LoadStaves()
        {
            var stavesFile = StaffMetadataManager.StaffMetadataFile;
            logger.LogInfo($"Attempting to staves metadata from file: {stavesFile}");

            try
            {
                if (File.Exists(stavesFile))
                {
                    StaffMetadataManager.Instance.LoadFromFile(stavesFile);
                    logger.LogInfo("Staves metadata loaded successfully");
                }
                else
                {
                    logger.LogWarn("No staves metadata file was found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load staves metadata");
                logger.LogException(ex);
            }
        }

        private void CalculateLines()
        {
            logger.LogInfo("Reculating all staff lines");
            StaffMetadataManager.Instance.RecalculateAllStaves();
        }

        private void StartUpdateTimers()
        {
            IconManager.Instance.Context = TaskScheduler.FromCurrentSynchronizationContext();

            StartProcessScanner();
            StartClientUpdate();
            StartFlowerUpdate();
            PlayerManager.Instance.PlayerAdded += PacketManager.Instance.OnPlayerAdded;
        }

        private void StartProcessScanner()
        {
            processScannerWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };

            processScannerWorker.DoWork += (sender, e) =>
            {
                var delayTime = (TimeSpan)e.Argument;

                if (delayTime > TimeSpan.Zero)
                    Thread.Sleep(delayTime);

                ProcessManager.Instance.ScanForProcesses();
            };

            processScannerWorker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Cancelled)
                    return;

                // Dead Clients
                while (ProcessManager.Instance.DeadClientCount > 0)
                {
                    var deadClient = ProcessManager.Instance.DequeueDeadClient();
                    // TODO: if client had followers then remove them
                    PlayerManager.Instance.RemovePlayer(deadClient.ProcessId);
                }

                // New Clients
                while (ProcessManager.Instance.NewClientCount > 0)
                {
                    var newClient = ProcessManager.Instance.DequeueNewClient();
                    PlayerManager.Instance.AddNewClient(newClient);
                    
                }

                if (clientListBox.SelectedIndex == -1 && clientListBox.Items.Count > 0)
                    clientListBox.SelectedIndex = 0;

                processScannerWorker.RunWorkerAsync(UserSettingsManager.Instance.Settings.ProcessUpdateInterval);
            };

            // Start immediately!
            processScannerWorker.RunWorkerAsync(TimeSpan.Zero);
            logger.LogInfo("Process scanner background worker has started");
        }

        private void StartClientUpdate()
        {
            var positionUpdateWorker = new BackgroundWorker();
            positionUpdateWorker.DoWork += (sender, e) =>
            {
                var players = PlayerManager.Instance.VisiblePlayers;
                foreach (var player in players)
                {
                    var stream = player.Accessor.GetStream();
                    var reader = new BinaryReader(stream, Encoding.ASCII);
                    
                    var mapNumberVar = new DynamicMemoryVariable("MapNumber", 0x882E68, 0, 0, 0, [0x26C]);
                    mapNumberVar.TryReadInt32(reader, out var mapNumber);
                    player.Location.MapNumber = mapNumber;

                    var mapNameVar = new DynamicMemoryVariable("MapName", 0x82B76C, 32, 0, 0, [0x4E3C]);
                    mapNameVar.TryReadString(reader, out var mapName);
                    player.Location.MapName = mapName;

                    var x = new DynamicMemoryVariable("MapX", 0x882E68, 0, 0, 0, [0x23C]);
                    x.TryReadInt32(reader, out var playerX);
                    
                    var y = new DynamicMemoryVariable("MapY", 0x882E68, 0, 0, 0, [0x238]);
                    y.TryReadInt32(reader, out var playerY);
                    player.Location.Point = new Point(playerX, playerY);
                    player.Location.X = playerX;
                    player.Location.Y = playerY;
                }
                Thread.Sleep(50);

            };
            positionUpdateWorker.RunWorkerCompleted += (sender, e) =>
            {
                positionUpdateWorker.RunWorkerAsync();
            };
            positionUpdateWorker.RunWorkerAsync();
            
            clientUpdateWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };

            clientUpdateWorker.DoWork += (sender, e) =>
            {
                var delayTime = (TimeSpan)e.Argument;
                if (delayTime > TimeSpan.Zero)
                    Thread.Sleep(delayTime);

                LastUpdate = DateTime.Now;
                PlayerManager.Instance.UpdateClients();
            };

            clientUpdateWorker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Cancelled)
                    return;

                clientUpdateWorker.RunWorkerAsync(UserSettingsManager.Instance.Settings.ClientUpdateInterval);
            };

            // Start immediately!
            clientUpdateWorker.RunWorkerAsync(TimeSpan.Zero);
            logger.LogInfo("Client update background worker has started");
        }

        private void StartFlowerUpdate()
        {
            var updateInterval = TimeSpan.FromMilliseconds(16);

            flowerUpdateWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };

            flowerUpdateWorker.DoWork += (sender, e) =>
            {
                var delayTime = (TimeSpan)e.Argument;

                if (delayTime > TimeSpan.Zero)
                    Thread.Sleep(delayTime);

                foreach (var macro in MacroManager.Instance.Macros)
                    if (macro.Status == MacroStatus.Running)
                        macro.TickFlowerTimers(updateInterval);
            };

            flowerUpdateWorker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Cancelled)
                    return;

                flowerUpdateWorker.RunWorkerAsync(updateInterval);
            };

            flowerUpdateWorker.RunWorkerAsync(updateInterval);
            logger.LogInfo($"Flower update background worker has started, interval = {updateInterval.TotalMilliseconds:0}ms");
        }

        private void ApplyTheme()
        {
            var themeName = UserSettingsManager.Instance.Settings.SelectedTheme;
            if (string.IsNullOrWhiteSpace(themeName))
            {
                logger.LogWarn("Selected theme is not defined, using default theme");
                themeName = ColorThemeManager.Instance.DefaultTheme?.Name;
            }

            if (themeName == null || !ColorThemeManager.Instance.ContainsTheme(themeName))
            {
                logger.LogWarn("Theme name is null or invalid, using default theme instead");
                ColorThemeManager.Instance.ApplyDefaultTheme();
                return;
            }

            logger.LogInfo($"Applying UI theme: {themeName}");
            ColorThemeManager.Instance.ApplyTheme(themeName);
        }

        private void ActivateHotkey(Key key, ModifierKeys modifiers)
        {
            var hotkey = HotkeyManager.Instance.GetHotkey(key, modifiers);

            if (hotkey == null)
                return;

            Player hotkeyPlayer = null;

            foreach (var player in PlayerManager.Instance.LoggedInPlayers)
                if (player.HasHotkey && player.Hotkey.Key == hotkey.Key && player.Hotkey.Modifiers == hotkey.Modifiers)
                {
                    hotkeyPlayer = player;
                    break;
                }

            if (hotkeyPlayer == null)
                return;

            logger.LogInfo($"Hotkey {hotkey.Modifiers}+{hotkey.Key} activated for character: {hotkeyPlayer.Name}");

            var macroState = MacroManager.Instance.GetMacroState(hotkeyPlayer);

            if (macroState == null)
                return;

            if (macroState.Status == MacroStatus.Running)
            {
                macroState.Pause();
                logger.LogInfo($"Paused macro state for character: {hotkeyPlayer.Name} (hotkey)");
            }
            else
            {
                macroState.Start();
                logger.LogInfo($"Started macro state for character: {hotkeyPlayer.Name} (hotkey)");
            }
        }

        private void UpdateListBoxGridWidths()
        {
            var settings = UserSettingsManager.Instance.Settings;

            SetInventoryGridWidth(settings.InventoryGridWidth);
            SetSkillGridWidth(settings.SkillGridWidth);
            SetWorldSkillGridWidth(settings.WorldSkillGridWidth);
            SetSpellGridWidth(settings.SpellGridWidth);
            SetWorldSpellGridWidth(settings.WorldSpellGridWidth);
        }

        private void SetInventoryGridWidth(int units)
        {
            if (units < 1)
            {
                inventoryListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.InventoryIconSize;
            inventoryListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetSkillGridWidth(int units)
        {
            if (units < 1)
            {
                temuairSkillListBox.MaxWidth = medeniaSkillListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            temuairSkillListBox.MaxWidth = medeniaSkillListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetWorldSkillGridWidth(int units)
        {
            if (units < 1)
            {
                worldSkillListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            worldSkillListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetSpellGridWidth(int units)
        {
            if (units < 1)
            {
                temuairSpellListBox.MaxWidth = medeniaSpellListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            temuairSpellListBox.MaxWidth = medeniaSpellListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private void SetWorldSpellGridWidth(int units)
        {
            if (units < 1)
            {
                worldSpellListBox.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconSize = UserSettingsManager.Instance.Settings.SkillIconSize;
            worldSpellListBox.MaxWidth = ((iconSize + IconPadding) * units) + 6;
        }

        private async void UpdateUIForMacroStatus(MacroStatus status)
        {
            await Dispatcher.SwitchToUIThread();

            switch (status)
            {
                case MacroStatus.Running:
                    startMacroButton.Tag = "Start Macro";
                    startMacroButton.IsEnabled = false;
                    pauseMacroButton.IsEnabled = true;
                    stopMacroButton.IsEnabled = true;
                    break;

                case MacroStatus.Paused:
                    startMacroButton.Tag = "Resume Macro";
                    startMacroButton.IsEnabled = true;
                    pauseMacroButton.IsEnabled = false;
                    stopMacroButton.IsEnabled = true;
                    break;

                default:
                    startMacroButton.Tag = "Start Macro";
                    startMacroButton.IsEnabled = true;
                    pauseMacroButton.IsEnabled = false;
                    stopMacroButton.IsEnabled = false;
                    break;
            }
        }

        private void ToggleModalOverlay(bool showHide) => modalOverlay.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;

        private void ToggleSpellQueue(bool showQueue)
        {
            if (spellQueueListBox == null)
                return;

            if (showQueue)
            {
                Grid.SetColumnSpan(tabControl, 1);
                spellQueueListBox.Visibility = Visibility.Visible;
            }
            else
            {
                Grid.SetColumnSpan(tabControl, 2);
                spellQueueListBox.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowMetadataWindow(int selectedTabIndex = -1)
        {
            if (metadataWindow == null || !metadataWindow.IsLoaded)
            {
                metadataWindow = new MetadataEditorWindow
                {
                    Owner = this
                };
            }

            if (selectedTabIndex >= 0)
                metadataWindow.SelectedTabIndex = selectedTabIndex;

            logger.LogInfo("Showing metadata editor window");
            metadataWindow.Show();
        }

        public void ShowSettingsWindow(int selectedTabIndex = -1)
        {
            if (settingsWindow == null || !settingsWindow.IsLoaded)
                settingsWindow = new SettingsWindow() { Owner = this };

            if (selectedTabIndex >= 0)
                settingsWindow.SelectedTabIndex = selectedTabIndex;
            else
                settingsWindow.SelectedTabIndex = recentSettingsTabIndex;

            settingsWindow.Closing += (sender, e) =>
            {
                recentSettingsTabIndex = (sender as SettingsWindow).SelectedTabIndex;
            };
            settingsWindow.Closed += (sender, e) =>
            {
                logger.LogInfo($"Settings window has been closed");
            };

            logger.LogInfo($"Showing settings window (tabIndex = {selectedTabIndex})");
            settingsWindow.Show();
        }

        private void UpdateUpdater(string updateFile, string installationPath)
        {
            if (!File.Exists(updateFile))
            {
                logger.LogError($"Missing update file, unable to update: {updateFile}");
                return;
            }

            try
            {
                using (var archive = ZipFile.OpenRead(updateFile))
                {
                    var entry = archive.GetEntry("Updater.exe");
                    if (entry == null)
                    {
                        logger.LogWarn($"Updater tool was not found in the update file: {updateFile}");
                        return;
                    }

                    var outputFile = Path.Combine(installationPath, entry.Name);
                    entry.ExtractToFile(outputFile, true);

                    logger.LogInfo($"Successfully updated the Updater tool: {outputFile}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to update the Updater tool");
                logger.LogException(ex);
            }
        }

        private void RunUpdater(string updateFile, string installationPath)
        {
            var updaterExecutable = Path.Combine(installationPath, "Updater.exe");
            logger.LogInfo($"Attempting start the updater executable: {updaterExecutable}");

            if (!File.Exists(updaterExecutable))
            {
                logger.LogError("Updater executable was not found");

                this.ShowMessageBox("Missing Updater", "Unable to start auto-updater executable.", "You may need to install the update manually.");
                return;
            }

            logger.LogInfo($"Starting the updater with arguments: {updateFile} {installationPath}");
            Process.Start(updaterExecutable, $"\"{updateFile}\" \"{installationPath}\"");
            Application.Current.Shutdown();
        }

        private nint WindowMessageHook(nint windowHandle, int message, nint wParam, nint lParam, ref bool isHandled)
        {
            if (message == WM_HOTKEY)
            {
                var key = KeyInterop.KeyFromVirtualKey(lParam.ToInt32() >> 16);
                var modifiers = (ModifierKeys)(lParam.ToInt32() & 0xFFFF);

                ActivateHotkey(key, modifiers);
            }

            return nint.Zero;
        }

        private void Window_Shown(object sender, EventArgs e)
        {
            InitializeHotkeyHook();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            logger.LogInfo("Application is shutting down");

            UserSettingsManager.Instance.Settings.PropertyChanged -= UserSettings_PropertyChanged;

            try
            {
                logger.LogInfo("Unregistering all hotkeys...");
                HotkeyManager.Instance.UnregisterAllHotkeys(windowSource.Handle);
                logger.LogInfo("Unregistered all hotkeys successfully");
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to unregister all hotkeys");
                logger.LogException(ex);
            }

            try
            {
                var settingsFile = UserSettingsManager.SettingsFile;

                logger.LogInfo($"Saving user settings to file: {settingsFile}");
                UserSettingsManager.Instance.SaveToFile(settingsFile);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to save user settings file");
                logger.LogException(ex);
            }

            try
            {
                FileArchiveManager.Instance.ClearArchives();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }

            var allMacros = MacroManager.Instance.Macros.ToList();

            foreach (var state in allMacros)
            {
                state.Stop();

                if (state.Client == null || !state.Client.IsLoggedIn)
                    continue;

                state.Stop();

                if (UserSettingsManager.Instance.Settings.SaveMacroStates)
                {
                    logger.LogInfo($"Auto-saving {state.Client.Name} macro state...");
                    AutoSaveMacroState(state, showError: false);
                }
            }

            logger.LogInfo("Application shutdown tasks have completed");
        } 

        private void PromptUserToOpenUserManual()
        {
            var result = this.ShowMessageBox("Welcome to DArvis",
                "It appears to be your first time running the application.\nDo you want to open the user manual?\n\n(This is recommended for new users)",
                "This prompt will not be displayed again.",
                MessageBoxButton.YesNo,
                480, 280);

            if (result.HasValue && result.Value)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(App.USER_MANUAL_URL) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    logger.LogInfo("Unable to open the user manual");
                    logger.LogException(ex);
                }
            }
            else
            {
                logger.LogInfo("User declined to view the user manual");
            }
        }

        private void AutoSaveMacroState(PlayerMacroState state, bool showError = true)
        {
            var autosaveDirectory = Path.Combine(Environment.CurrentDirectory, "autosave");
            try
            {
                if (!Directory.Exists(autosaveDirectory))
                {
                    logger.LogInfo($"Creating autosave directory: {autosaveDirectory}");
                    Directory.CreateDirectory(autosaveDirectory);
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                logger.LogError($"Unable to create autosave directory: {autosaveDirectory}");

                if (showError)
                {
                    this.ShowMessageBox("Failed to Autosave", $"Unable to save macro state for {state.Client.Name}.", ex.Message);
                    return;
                }
            }

            var autosaveFile = $"{state.Client.Name}-Autosave.{DArvisMacroFileExtension}";
            var autosaveFilePath = Path.Combine (autosaveDirectory, autosaveFile);

            SaveMacroState(state, autosaveFilePath, showError);
        }

        private void SaveMacroState(PlayerMacroState state, string filename, bool showError = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (filename == null)
                throw new ArgumentNullException(nameof(filename));

            try
            {
                logger.LogInfo($"Saving {state.Client.Name} macro state into {filename}...");
                macroStateSerializer.Serialize(state, filename);
                logger.LogInfo("Serialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                logger.LogError($"Unable to save macro state file: {filename}");

                if (showError)
                    this.ShowMessageBox("Failed to Save Macro", $"Unable to save the macro state for {state.Client.Name}.", ex.Message);
            }
        }

        private void AutoLoadMacroState(PlayerMacroState state, bool showError = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var autosaveDirectory = Path.Combine(Environment.CurrentDirectory, "autosave");
            if (!Directory.Exists(autosaveDirectory))
            {
                logger.LogInfo($"Auto-save directory does not exist: {autosaveDirectory}");
                return;
            }

            var autosaveFile = $"{state.Client.Name}-Autosave.{DArvisMacroFileExtension}";
            var autosaveFilePath = Path.Combine(autosaveDirectory, autosaveFile);

            if (!File.Exists(autosaveFilePath))
            {
                logger.LogInfo($"Auto-save file does not exist: {autosaveFilePath}");
                return;
            }

            var didLoad = LoadMacroState(state, autosaveFilePath, showError);
            
            // File is probably broken, delete it
            if (!didLoad && File.Exists(autosaveFilePath))
            {
                try
                {
                    File.Delete(autosaveFilePath);
                }
                catch(Exception ex)
                {
                    logger.LogException(ex);
                    logger.LogWarn($"Unable to delete autosave file: {autosaveFilePath}");
                }
            }
        }

        private bool LoadMacroState(PlayerMacroState state, string filename, bool showError = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (filename == null)
                throw new ArgumentNullException(nameof(filename));

            state.Stop();

            SerializedMacroState deserialized;

            try
            {
                logger.LogInfo($"Loading {state.Client.Name} macro state from {filename}...");
                deserialized = macroStateSerializer.Deserialize(state, filename);
                logger.LogInfo("Deserialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                logger.LogError($"Unable to load macro state file: {filename}");

                if (showError)
                    this.ShowMessageBox("Failed to Load Macro", $"Unable to load the macro state for {state.Client.Name}.", ex.Message);

                return false;
            }

            try
            {
                logger.LogInfo($"Updating {state.Client.Name} macro state from deserialized data...");

                var process = Process.GetCurrentProcess();

                // Stop macro and unregister any current hotkey
                state.Stop();

                if (state.Client.HasHotkey)
                    HotkeyManager.Instance.UnregisterHotkey(process.MainWindowHandle, state.Client.Hotkey);

                // Clear state
                state.Client.Skillbook.ClearActiveSkills();
                state.ClearSpellQueue();
                state.ClearFlowerQueue();
                state.LocalStorage.Clear();

                // Re-register the new hotkey (if defined)
                if (deserialized.Hotkey != null)
                {
                    var hotkey = new Hotkey(deserialized.Hotkey.Modifiers, deserialized.Hotkey.Key);
                    HotkeyManager.Instance.RegisterHotkey(process.MainWindowHandle, hotkey);

                    state.Client.Hotkey = hotkey;
                }
                else state.Client.Hotkey = null;

                // Set spell rotation mode and flower configuration
                if (deserialized.SpellRotation != SpellRotationMode.Default)
                    state.SpellQueueRotation = deserialized.SpellRotation;
                else
                    state.SpellQueueRotation = UserSettingsManager.Instance.Settings.SpellRotationMode;

                state.UseLyliacVineyard = deserialized.UseLyliacVineyard;
                state.FlowerAlternateCharacters = deserialized.FlowerAlternateCharacters;

                // Add all skill macros to state
                foreach (var skillMacro in deserialized.Skills)
                {
                    if (!state.Client.Skillbook.ContainSkill(skillMacro.SkillName))
                        continue;

                    state.Client.Skillbook.ToggleActive(skillMacro.SkillName, true);
                }

                // Add all spell macros to state
                foreach (var spellMacro in deserialized.Spells)
                {
                    var spellInfo = state.Client.Spellbook.GetSpell(spellMacro.SpellName);
                    if (spellInfo == null)
                        continue;

                    state.AddToSpellQueue(new SpellQueueItem
                    {
                        Icon = spellInfo.Icon,
                        Name = spellInfo.Name,
                        Target = new SpellTarget
                        {
                            Mode = spellMacro.TargetMode,
                            CharacterName = spellMacro.TargetName,
                            Location = new Point(spellMacro.LocationX, spellMacro.LocationY),
                            Offset = new Point(spellMacro.OffsetX, spellMacro.OffsetY),
                            InnerRadius = spellMacro.InnerRadius,
                            OuterRadius = spellMacro.OuterRadius,
                        },
                        TargetLevel = spellMacro.TargetLevel > 0 ? spellMacro.TargetLevel : null,
                        CurrentLevel = spellInfo.CurrentLevel,
                        MaximumLevel = spellInfo.MaximumLevel,
                        IsOnCooldown = spellInfo.IsOnCooldown
                    });
                }

                // Add all flower macros to state
                foreach (var flowerMacro in deserialized.FlowerTargets)
                {
                    if (flowerMacro.TargetMode == SpellTargetMode.None)
                        continue;

                    state.AddToFlowerQueue(new FlowerQueueItem
                    {
                        Target = new SpellTarget
                        {
                            Mode = flowerMacro.TargetMode,
                            CharacterName = flowerMacro.TargetName,
                            Location = new Point(flowerMacro.LocationX, flowerMacro.LocationY),
                            Offset = new Point(flowerMacro.OffsetX, flowerMacro.OffsetY),
                            InnerRadius = flowerMacro.InnerRadius,
                            OuterRadius = flowerMacro.OuterRadius,
                        },
                        Interval = flowerMacro.HasInterval ? flowerMacro.Interval : null,
                        ManaThreshold = flowerMacro.ManaThreshold > 0 ? flowerMacro.ManaThreshold : null
                    });
                }

                // Copy local storage
                foreach (var keyValuePair in deserialized.LocalStorage.Entries)
                {
                    state.LocalStorage.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                logger.LogError($"Unable to update {state.Client.Name} macro state from deserialized data");

                this.ShowMessageBox("Failed to Load Macro", "Unable to load the macro state.", ex.Message);
                return false;
            }
            finally
            {
                UpdateToolbarState();
                RefreshSpellQueue();
                RefreshFlowerQueue();

                if (selectedMacro != null && selectedMacro == state)
                    ToggleSpellQueue(state.QueuedSpells.Count > 0);
            }
        }

        #region Toolbar Button Click Methods
        private void launchClientButton_Click(object sender, RoutedEventArgs e) => LaunchClient();

        private void loadStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || selectedMacro.Client == null || !selectedMacro.Client.IsLoggedIn)
                return;

            var defaultFilename = $"{selectedMacro.Client.Name}.{DArvisMacroFileExtension}";
            var savesDirectory = Path.Combine(Environment.CurrentDirectory, "saves");

            var dialog = new OpenFileDialog
            {
                Title = "Load Macro State",
                Filter = DArvisMacroFileFilter,
                DefaultExt = DArvisMacroFileExtension,
                FileName = defaultFilename,
                InitialDirectory = savesDirectory,
                Multiselect = false,
                CheckPathExists = true,
                CheckFileExists = true
            };

            logger.LogInfo($"User has requested to load the macro state for character: {selectedMacro.Client.Name}");

            if (!dialog.ShowDialog(this).GetValueOrDefault())
            {
                logger.LogInfo("User has cancelled the load macro dialog");
                return;
            }

            if (selectedMacro != null)
                LoadMacroState(selectedMacro, dialog.FileName);
        }

        private void saveStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || selectedMacro.Client == null || !selectedMacro.Client.IsLoggedIn)
                return;

            var defaultFilename = $"{selectedMacro.Client.Name}.{DArvisMacroFileExtension}";
            var savesDirectory = Path.Combine(Environment.CurrentDirectory, "saves");

            var dialog = new SaveFileDialog
            {
                Title = "Save Macro State",
                Filter = DArvisMacroFileFilter,
                DefaultExt = DArvisMacroFileExtension,
                FileName = defaultFilename,
                InitialDirectory = savesDirectory,
                OverwritePrompt = true,
                AddExtension = true,
                CheckPathExists = true,
                ValidateNames = true,
            };

            logger.LogInfo($"User has requested to save the macro state for character: {selectedMacro.Client.Name}");

            if (!dialog.ShowDialog(this).GetValueOrDefault())
            {
                logger.LogInfo("User has cancelled the save macro dialog");
                return;
            }

            if (selectedMacro != null)
                SaveMacroState(selectedMacro, dialog.FileName);
        }

        private void startMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || selectedMacro.Client == null || !selectedMacro.Client.IsLoggedIn)
                return;

            selectedMacro.Client.Update();
            selectedMacro.Start();
            UpdateToolbarState();

            logger.LogInfo($"Started macro state for character: {selectedMacro.Client.Name} (toolbar)");
        }

        private void pauseMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Pause();
            UpdateToolbarState();

            logger.LogInfo($"Paused macro state for character {selectedMacro.Client.Name} (toolbar)");
        }

        private void stopMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Stop();
            UpdateToolbarState();

            logger.LogInfo($"Stopped macro state for character {selectedMacro.Client.Name} (toolbar)");
        }

        private void stopAllMacrosButton_Click(object sender, RoutedEventArgs e)
        {
            MacroManager.Instance.StopAll();
            UpdateToolbarState();

            logger.LogInfo("Stopped all macro states (toolbar)");
        }

        private void showSpellQueueButton_Click(object sender, RoutedEventArgs e) => ToggleSpellQueue(true);
        private void hideSpellQueueButton_Click(object sender, RoutedEventArgs e) => ToggleSpellQueue(false);
        private void metadataEditorButton_Click(object sender, RoutedEventArgs e) => ShowMetadataWindow();
        private void settingsButton_Click(object sender, RoutedEventArgs e) => ShowSettingsWindow();
        #endregion

        private void clientListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not Player player)
                return;

            NativeMethods.SetForegroundWindow(player.Process.WindowHandle);
            logger.LogInfo($"Setting foreground window for client: {player.Name} (double-click)");
        }

        private void spellQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not SpellQueueItem queueItem)
                return;

            if (selectedMacro == null)
                return;

            var player = selectedMacro.Client;
            var spell = player.Spellbook.GetSpell(queueItem.Name);

            if (spell == null)
                return;

            var dialog = new SpellTargetWindow(spell, queueItem)
            {
                Owner = this
            };

            logger.LogInfo($"Showing spell '{spell.Name}' target dialog for character: {player.Name}");
            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            dialog.SpellQueueItem.CopyTo(queueItem);
        }

        private void spellQueueListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not SpellQueueItem spell)
                return;

            if (selectedMacro == null)
                return;

            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                logger.LogInfo($"Removing spell '{spell.Name}' from spell queue for character: {selectedMacro.Client.Name}");

                if (selectedMacro.RemoveFromSpellQueue(spell))
                    RefreshSpellQueue();
            }
        }

        private void spellQueueListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || sender is not ListBoxItem draggedItem)
                return;

            logger.LogInfo($"Drag spell queue item: {draggedItem}");

            DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
            draggedItem.IsSelected = true;
        }

        private void spellQueueListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Effects != DragDropEffects.Move)
                return;

            var droppedItem = e.Data.GetData(typeof(SpellQueueItem)) as SpellQueueItem;
            var target = (sender as ListBoxItem)?.DataContext as SpellQueueItem;

            var removedIndex = spellQueueListBox.Items.IndexOf(droppedItem);
            var targetIndex = spellQueueListBox.Items.IndexOf(target);

            logger.LogInfo($"Drop spell queue item: {droppedItem} (target = {target})");

            if (removedIndex < targetIndex)
            {
                selectedMacro.AddToSpellQueue(droppedItem, targetIndex + 1);
                selectedMacro.RemoveFromSpellQueueAtIndex(removedIndex);
            }
            else
            {
                if (selectedMacro.QueuedSpells.Count + 1 > removedIndex + 1)
                {
                    selectedMacro.AddToSpellQueue(droppedItem, targetIndex);
                    selectedMacro.RemoveFromSpellQueueAtIndex(removedIndex + 1);
                }
            }

            RefreshSpellQueue();
        }

        private void spellQueueListBox_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;

            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }

        private void flowerQueueListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not FlowerQueueItem queueItem)
                return;

            if (selectedMacro == null)
                return;

            var dialog = new FlowerTargetWindow(queueItem)
            {
                Owner = this
            };

            logger.LogInfo($"Showing flower target dialog for character: {selectedMacro.Client.Name}");
            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            dialog.FlowerQueueItem.CopyTo(queueItem);
        }

        private void flowerQueueListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ListBoxItem listBoxItem)
                return;

            if (listBoxItem.Content is not FlowerQueueItem flower)
                return;

            if (selectedMacro == null)
                return;

            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                logger.LogInfo($"Removing '{flower.Target}' from flower queue for character: {selectedMacro.Name}");

                if (selectedMacro.RemoveFromFlowerQueue(flower))
                    RefreshFlowerQueue();
            }
        }

        private void flowerQueueListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || sender is not ListBoxItem draggedItem)
                return;

            logger.LogInfo($"Drag flower queue item: {draggedItem}");

            DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
            draggedItem.IsSelected = true;
        }

        private void flowerQueueListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Effects != DragDropEffects.Move)
                return;

            var droppedItem = e.Data.GetData(typeof(FlowerQueueItem)) as FlowerQueueItem;
            var target = (sender as ListBoxItem)?.DataContext as FlowerQueueItem;

            var removedIndex = flowerListBox.Items.IndexOf(droppedItem);
            var targetIndex = flowerListBox.Items.IndexOf(target);

            logger.LogInfo($"Drop flower queue item: {droppedItem} (target = {target})");

            if (removedIndex < targetIndex)
            {
                selectedMacro.AddToFlowerQueue(droppedItem, targetIndex + 1);
                selectedMacro.RemoveFromFlowerQueueAtIndex(removedIndex);
            }
            else
            {
                if (selectedMacro.FlowerTargets.Count + 1 > removedIndex + 1)
                {
                    selectedMacro.AddToFlowerQueue(droppedItem, targetIndex);
                    selectedMacro.RemoveFromFlowerQueueAtIndex(removedIndex + 1);
                }
            }

            RefreshFlowerQueue();
        }

        private void flowerQueueListBox_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;

            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }

        private void clientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.SelectedItem is not Player player)
            {
                if (selectedMacro != null)
                    selectedMacro.PropertyChanged -= SelectedMacro_PropertyChanged;

                selectedMacro = null;
                ToggleInventory(false);
                ToggleSkills(false);
                ToggleSpells(false);
                ToggleFlower(false);
                UpdateToolbarState();
                return;
            }

            var macroState = MacroManager.Instance.GetMacroState(player);

            UnsubscribeMacroHandlers(selectedMacro);
            var prevSelectedMacro = selectedMacro;
            selectedMacro = macroState;
            SubscribeMacroHandlers(selectedMacro);

            UpdateToolbarState();

            if (selectedMacro == null)
                return;

            UpdateLeaderTabContents();
            
            tabControl.SelectedIndex = Math.Max(0, selectedMacro.Client.SelectedTabIndex);

            if (prevSelectedMacro == null && selectedMacro?.QueuedSpells.Count > 0)
                ToggleSpellQueue(true);

            var supportsFlowering = player.Version?.SupportsFlowering ?? false;

            ToggleInventory(player.IsLoggedIn);
            ToggleSkills(player.IsLoggedIn);
            ToggleSpells(player.IsLoggedIn);
            ToggleFlower(supportsFlowering, player.HasLyliacPlant, player.HasLyliacVineyard);
            
            if (selectedMacro != null)
            {
                RefreshInventory();

                spellQueueRotationComboBox.SelectedValue = selectedMacro.SpellQueueRotation;

                spellQueueListBox.ItemsSource = selectedMacro.QueuedSpells;
                RefreshSpellQueue();

                if (selectedMacro.QueuedSpells.Count > 0)
                    ToggleSpellQueue(true);

                flowerListBox.ItemsSource = selectedMacro.FlowerTargets;
                RefreshFlowerQueue();

                flowerVineyardCheckBox.IsChecked = selectedMacro.UseLyliacVineyard && player.HasLyliacVineyard;
                flowerAlternateCharactersCheckBox.IsChecked = selectedMacro.FlowerAlternateCharacters && player.HasLyliacPlant;

                foreach (var spell in selectedMacro.QueuedSpells)
                    spell.IsUndefined = !SpellMetadataManager.Instance.ContainsSpell(spell.Name);
            }
            else
            {
                ToggleSpellQueue(false);
            }
        }

        private void UpdateLeaderTabContents()
        {
            if (selectedMacro?.Client?.Leader != null)
            {
                var leaderItem = LeaderSelectionManager.Instance.Leaders
                    .FirstOrDefault(item => item.Player == selectedMacro.Client.Leader);
        
                if (leaderItem != null)
                {
                    LeaderListBox.SelectedItem = leaderItem;
                }
            }
            else
            {
                LeaderListBox.SelectedItem =
                    LeaderSelectionManager.Instance.Leaders.FirstOrDefault(item => item.IsNone);
            }
            LeaderListBox.Items.Refresh();
        }
        
        private void leaderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedMacro?.Client == null)
                return;
            
            if (LeaderListBox.SelectedItem is LeaderSelectionItem selectedItem)
            {
                if (selectedMacro.Client.Leader == selectedItem.Player)
                    return;

                if (selectedMacro.Client.Leader == null && selectedItem.IsNone)
                    return;

                if (selectedItem.IsNone)
                {
                    selectedMacro.Client.Leader.Follower = null;
                    selectedMacro.Client.Leader = null;
                    return;
                }
                
                selectedMacro.Client.Leader = selectedItem.Player;
                selectedItem.Player.Follower = selectedMacro.Client;
                
                Console.WriteLine(selectedMacro.Client.Name + " is following " + selectedMacro.Client.Leader.Name);
                Console.WriteLine(selectedItem.Player + " is leading " + selectedItem.Player.Follower.Name);
            }
        }
        
        private void SubscribeMacroHandlers(PlayerMacroState state)
        {
            if (state != null)
                return;

            state.PropertyChanged += SelectedMacro_PropertyChanged;

            state.SpellAdded += selectedMacro_SpellQueueChanged;
            state.SpellUpdated += selectedMacro_SpellQueueChanged;
            state.SpellRemoved += selectedMacro_SpellQueueChanged;

            state.FlowerTargetAdded += selectedMacro_FlowerQueueChanged;
            state.FlowerTargetUpdated += selectedMacro_FlowerQueueChanged;
            state.FlowerTargetRemoved += selectedMacro_FlowerQueueChanged;
        }

        private void UnsubscribeMacroHandlers(PlayerMacroState state)
        {
            if (state == null)
                return;

            state.PropertyChanged -= SelectedMacro_PropertyChanged;

            state.SpellAdded -= selectedMacro_SpellQueueChanged;
            state.SpellUpdated -= selectedMacro_SpellQueueChanged;
            state.SpellRemoved -= selectedMacro_SpellQueueChanged;

            state.FlowerTargetAdded -= selectedMacro_FlowerQueueChanged;
            state.FlowerTargetUpdated -= selectedMacro_FlowerQueueChanged;
            state.FlowerTargetRemoved -= selectedMacro_FlowerQueueChanged;
        }

        private void clientListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.None)
                return;

            if (sender is not ListBoxItem listBoxItem || listBoxItem.Content is not Player player)
                return;

            var key = ((e.Key == Key.System) ? e.SystemKey : e.Key);
            var hasControl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || (e.SystemKey == Key.LeftCtrl || e.SystemKey == Key.RightCtrl);
            var hasAlt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt);
            var hasShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || (e.SystemKey == Key.LeftShift || e.SystemKey == Key.RightShift);
            var hasWindows = Keyboard.Modifiers.HasFlag(ModifierKeys.Windows);
            var isFunctionKey = Hotkey.IsFunctionKey(key);

            if (key == Key.LeftCtrl || key == Key.RightCtrl)
                return;

            if (key == Key.LeftAlt || e.Key == Key.RightAlt)
                return;

            if (key == Key.LeftShift || e.Key == Key.RightShift)
                return;

            if (!hasControl && !hasAlt && !hasShift && !isFunctionKey)
            {
                if (e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Escape)
                {
                    if (player.Hotkey != null)
                    {
                        logger.LogInfo($"Clearing hotkey for character: {player.Name}");
                        HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, player.Hotkey);
                    }

                    player.Hotkey = null;
                }
                return;
            }

            var modifiers = ModifierKeys.None;

            if (hasControl)
                modifiers |= ModifierKeys.Control;
            if (hasAlt)
                modifiers |= ModifierKeys.Alt;
            if (hasShift)
                modifiers |= ModifierKeys.Shift;
            if (hasWindows)
                modifiers |= ModifierKeys.Windows;

            var hotkey = new Hotkey(modifiers, key);
            var oldHotkey = HotkeyManager.Instance.GetHotkey(hotkey.Key, hotkey.Modifiers);

            if (oldHotkey != null)
            {
                foreach (var p in PlayerManager.Instance.AllClients)
                {
                    if (!p.HasHotkey)
                        continue;

                    if (p.Hotkey.Key == hotkey.Key && p.Hotkey.Modifiers == hotkey.Modifiers)
                        p.Hotkey = null;
                }
            }

            HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, hotkey);

            if (!HotkeyManager.Instance.RegisterHotkey(windowSource.Handle, hotkey))
            {
                logger.LogError($"Unable to set hotkey {hotkey.Modifiers}+{hotkey.Key} for character: {player.Name}");

                this.ShowMessageBox("Set Hotkey Error",
                   "There was an error setting the hotkey, please try again.",
                   "If this continues, try restarting the application.",
                   MessageBoxButton.OK,
                   420, 240);
            }
            else
            {
                if (player.Hotkey != null)
                    HotkeyManager.Instance.UnregisterHotkey(windowSource.Handle, player.Hotkey);

                player.Hotkey = hotkey;
            }

            e.Handled = true;
        }

        private void selectedMacro_SpellQueueChanged(object sender, SpellQueueItemEventArgs e)
        {
            if (sender is not PlayerMacroState macro)
                return;

            spellQueueListBox.ItemsSource = macro.QueuedSpells;
            RefreshSpellQueue();
        }

        private void selectedMacro_FlowerQueueChanged(object sender, FlowerQueueItemEventArgs e)
        {
            if (sender is not PlayerMacroState macro)
                return;

            flowerListBox.ItemsSource = macro.FlowerTargets;
            RefreshFlowerQueue();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl)
                return;

            if (selectedMacro == null)
                return;

            TabItem oldTab = null;
            TabItem newTab = null;

            if (e.RemovedItems.Count > 0)
                oldTab = e.RemovedItems[0] as TabItem;

            if (e.AddedItems.Count > 0)
                newTab = e.AddedItems[0] as TabItem;

            if (oldTab != null)
                TabDeselected(oldTab);

            if (newTab != null)
                TabSelected(newTab);
        }

        private void TabDeselected(TabItem tab)
        {
            if (selectedMacro == null)
                return;
        }

        private void TabSelected(TabItem tab)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.Client.SelectedTabIndex = tabControl.Items.IndexOf(tab);

            var supportsFlowering = selectedMacro.Client.Version?.SupportsFlowering ?? false;
            ToggleFlower(supportsFlowering, selectedMacro.Client.HasLyliacPlant, selectedMacro.Client.HasLyliacVineyard);
        }

        private void inventoryListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            // Do nothing for now
        }

        private void equipmentListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            // Do nothing for now
        }

        private void skillListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem item)
                return;

            if (item.Content is not Skill skill)
                return;

            if (clientListBox.SelectedItem is not Player player)
                return;

            if (skill.IsEmpty || string.IsNullOrWhiteSpace(skill.Name))
                return;

            logger.LogInfo($"Toggling skill '{skill.Name}' for character: {player.Name}");
            player.Skillbook.ToggleActive(skill.Name);
        }

        private void spellListBox_ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only handle left-click
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ListBoxItem item)
                return;

            if (item.Content is not Spell spell)
                return;

            if (clientListBox.SelectedItem is not Player player)
                return;

            if (spell.IsEmpty || string.IsNullOrWhiteSpace(spell.Name))
                return;

            if (spell.TargetType == AbilityTargetType.TextInput)
            {
                this.ShowMessageBox("Not Supported",
                   "This spell requires a user text input and cannot be macroed.",
                   "Only spells with no target or a single target can be macroed.",
                   MessageBoxButton.OK,
                   500, 240);
                return;
            }

            if (selectedMacro == null)
                return;

            var spellTargetWindow = new SpellTargetWindow(spell)
            {
                Owner = this
            };

            logger.LogInfo($"Showing spell '{spell.Name}' target dialog for character: {player.Name}");
            var result = spellTargetWindow.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            var queueItem = spellTargetWindow.SpellQueueItem;

            var isAlreadyQueued = selectedMacro.IsSpellInQueue(queueItem.Name);

            if (isAlreadyQueued && UserSettingsManager.Instance.Settings.WarnOnDuplicateSpells)
            {
                logger.LogInfo($"Spell '{spell.Name}' is already queued for character {player.Name}, asking user to override");

                var userOverride = this.ShowMessageBox("Already Queued",
                   string.Format("The spell '{0}' is already queued.\nDo you want to queue it again anyways?", spell.Name),
                   "This warning message can be disabled in the Spell Macro settings.",
                   MessageBoxButton.YesNo,
                   460, 240);

                if (!userOverride.HasValue || !userOverride.Value)
                    return;
            }

            selectedMacro.AddToSpellQueue(queueItem);
            ToggleSpellQueue(true);
            RefreshSpellQueue();

            logger.LogInfo($"Spell '{spell.Name}' added to spell queue for character: {player.Name}");
        }

        private void removeSelectedSpellButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || spellQueueListBox.SelectedItem is not SpellQueueItem selectedSpell)
                return;

            selectedMacro.RemoveFromSpellQueue(selectedSpell);
            RefreshSpellQueue();

            logger.LogInfo($"Spell '{selectedSpell.Name}' removed from spell queue for character: {selectedMacro.Client.Name}");
        }

        private void removeAllSpellsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.ClearSpellQueue();
            RefreshSpellQueue();

            logger.LogInfo($"Spell queue cleared for character: {selectedMacro.Client.Name}");
        }

        private void addFlowerTargetButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            var flowerTargetDialog = new FlowerTargetWindow
            {
                Owner = this
            };

            logger.LogInfo($"Showing flower target dialog for character: {selectedMacro.Client.Name}");
            var result = flowerTargetDialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return;

            var queueItem = flowerTargetDialog.FlowerQueueItem;
            queueItem.LastUsedTimestamp = DateTime.Now;

            selectedMacro.AddToFlowerQueue(queueItem);
            RefreshFlowerQueue();

            logger.LogInfo($"Added '{queueItem.Target}' to flower queue for character: {selectedMacro.Client.Name}");
        }

        private void removeSelectedFlowerTargetButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null || flowerListBox.SelectedItem is not FlowerQueueItem selectedTarget)
                return;

            selectedMacro.RemoveFromFlowerQueue(selectedTarget);
            RefreshFlowerQueue();

            logger.LogInfo($"Removed '{selectedTarget.Target}' from flower queue for character: {selectedMacro.Client.Name}");
        }

        private void removeAllFlowerTargetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            selectedMacro.ClearFlowerQueue();
            RefreshFlowerQueue();

            logger.LogInfo($"Cleared flower queue for character: {selectedMacro.Client.Name}");
        }

        private void UserSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not UserSettings settings)
                return;

            logger.LogInfo($"User setting property changed: {e.PropertyName}");

            if (string.Equals(nameof(settings.SelectedTheme), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                ApplyTheme();

            if (string.Equals(nameof(settings.ClientSortOrder), e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                PlayerManager.Instance.SortOrder = settings.ClientSortOrder;
                UpdateClientList();
            }

            if (string.Equals(nameof(settings.InventoryGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetInventoryGridWidth(settings.InventoryGridWidth);

            if (string.Equals(nameof(settings.SkillGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetSkillGridWidth(settings.SkillGridWidth);

            if (string.Equals(nameof(settings.WorldSkillGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetWorldSkillGridWidth(settings.WorldSkillGridWidth);

            if (string.Equals(nameof(settings.SpellGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetSpellGridWidth(settings.SpellGridWidth);

            if (string.Equals(nameof(settings.WorldSpellGridWidth), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                SetWorldSpellGridWidth(settings.WorldSpellGridWidth);

            if (string.Equals(nameof(settings.InventoryIconSize), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateListBoxGridWidths();

            if (string.Equals(nameof(settings.SkillIconSize), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateListBoxGridWidths();

            // Debug settings

            if (string.Equals(nameof(settings.ShowAllProcesses), e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                PlayerManager.Instance.ShowAllClients = settings.ShowAllProcesses;
                UpdateClientList();
            }
        }

        private void SelectedMacro_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not PlayerMacroState macro)
                return;

            if (string.Equals(nameof(macro.Status), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                UpdateUIForMacroStatus(macro.Status);

            // Update Spell Queue Rotation
            if (string.Equals(nameof(macro.SpellQueueRotation), e.PropertyName, StringComparison.OrdinalIgnoreCase))
                spellQueueRotationComboBox.SelectedValue = macro.SpellQueueRotation;
        }

        private void spellQueueRotationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedMacro == null || e.AddedItems.Count < 1)
                return;

            if (e.AddedItems[0] is not UserSetting selection)
                return;

            if (!Enum.TryParse<SpellRotationMode>(selection.Value as string, out var newRotationMode))
                return;

            selectedMacro.SpellQueueRotation = newRotationMode;
        }

        private void flowerVineyardCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            if (flowerVineyardCheckBox != null)
                selectedMacro.UseLyliacVineyard = flowerVineyardCheckBox.IsChecked.Value;
        }

        private void flowerAlternateCharactersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (selectedMacro == null)
                return;

            if (flowerAlternateCharactersCheckBox != null)
                selectedMacro.FlowerAlternateCharacters = flowerAlternateCharactersCheckBox.IsChecked.Value;
        }


        private void testButton_Click(object sender, RoutedEventArgs e)
        {
            var bw = new BackgroundWorker();
            bw.DoWork += (s, args) =>
            {
                try
                {
                    var player = PlayerManager.Instance.AllClients.FirstOrDefault();
                    GameActions.Refresh(player);
                    GameActions.Refresh(player); // Refresh twice to ensure all data is loaded
                    Thread.Sleep(2000);
                    GameActions.Walk(player, Direction.North);
                    Thread.Sleep(500);
                    GameActions.Walk(player, Direction.North);
                    Thread.Sleep(1000);
                    GameActions.Walk(player, Direction.East);
                    Thread.Sleep(500);
                    GameActions.Walk(player, Direction.East);
                    Thread.Sleep(1000);
                    GameActions.Walk(player, Direction.South);
                    Thread.Sleep(500);
                    GameActions.Walk(player, Direction.South);
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error during test walk: {ex.Message}");
                }
            };
            bw.RunWorkerAsync();

        }
        
        private void injectButton_Click(object sender, RoutedEventArgs e)
        {
            var players = PlayerManager.Instance.AllClients;
            foreach (var player in players)
            {
                var pId = player.Process.ProcessId;
                PacketManager.InjectDAvid(pId);
            }
            
        }
        
        private async void UpdateToolbarState()
        {
            await Dispatcher.SwitchToUIThread();

            launchClientButton.IsEnabled = ClientVersionManager.Instance.Versions.Any(v => v.Key != "Auto-Detect");
            loadStateButton.IsEnabled = saveStateButton.IsEnabled = selectedMacro != null && selectedMacro.Client.IsLoggedIn;

            stopAllMacrosButton.IsEnabled = MacroManager.Instance.Macros.Any(macro => macro.Status == MacroStatus.Running || macro.Status == MacroStatus.Paused);

            if (selectedMacro == null)
                startMacroButton.IsEnabled = pauseMacroButton.IsEnabled = stopMacroButton.IsEnabled = false;
            else
                UpdateUIForMacroStatus(selectedMacro.Status);
        }

        private void ToggleInventory(bool show = true)
        {
            inventoryTab.IsEnabled = show;
            inventoryEquipmentToggleGroup.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleSkills(bool show = true)
        {
            temuairSkillListBox.Visibility = medeniaSkillListBox.Visibility = worldSkillListBox.Visibility = (show ? Visibility.Visible : Visibility.Collapsed);
            skillsTab.IsEnabled = show;

            if (!show)
                skillsTab.TabIndex = -1;
        }

        private void ToggleSpells(bool show = true)
        {
            temuairSpellListBox.Visibility = medeniaSpellListBox.Visibility = worldSpellListBox.Visibility = (show ? Visibility.Visible : Visibility.Collapsed);
            spellsTab.IsEnabled = show;

            if (!show)
                spellsTab.TabIndex = -1;
        }

        private void ToggleFlower(bool show = false, bool hasLyliacPlant = false, bool hasLyliacVineyard = false)
        {
            flowerTab.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            flowerTab.IsEnabled = hasLyliacPlant || hasLyliacVineyard;

            flowerAlternateCharactersCheckBox.IsEnabled = hasLyliacPlant;
            flowerVineyardCheckBox.IsEnabled = hasLyliacVineyard;

            if (!hasLyliacPlant)
                flowerAlternateCharactersCheckBox.IsChecked = false;

            if (!hasLyliacVineyard)
                flowerVineyardCheckBox.IsChecked = false;
        }

        private async void UpdateClientList()
        {
            await Dispatcher.SwitchToUIThread();

            var showAll = PlayerManager.Instance.ShowAllClients;
            var sortOrder = PlayerManager.Instance.SortOrder;

            logger.LogInfo($"Updating the client list (showAll = {showAll}, sortOrder = {sortOrder})");

            clientListBox.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateTarget();
            clientListBox.Items.Refresh();
            LeaderListBox.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateTarget();
            LeaderListBox.Items.Refresh();
            
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource?.AddHook(WndProcHook);
        }
        
        private void Window_Closed(object sender, EventArgs e)
        {
            
        }
        
        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return PacketManager.Instance.InterceptPacket(hwnd, msg, wParam, lParam, ref handled);
        }
    }
}
