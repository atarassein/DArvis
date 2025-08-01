﻿using System;
using System.Windows;
using System.Windows.Threading;
using DArvis.Services;
using DArvis.Services.Logging;
using DArvis.Services.Serialization;
using DArvis.Services.SideQuest;
using DArvis.Views;
using IServiceProvider = DArvis.Services.IServiceProvider;

namespace DArvis
{
    public partial class App : Application
    {
        public const string USER_MANUAL_URL = @"https://ewrogers.github.io/DArvis4/";

        private ILogger? logger;
        private ISideQuest? sideQuestService;

        public static new App Current => (App)Application.Current;

        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();
            InitializeComponent();

            SetupCleanupHandlers();
            Current.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (logger == null)
                logger = Services.GetService<ILogger>();

            logger.LogError("Unhandled exception!");
            logger.LogException(e.Exception);

            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start SideQuest service
            sideQuestService = Services.GetService<ISideQuest>();
            sideQuestService?.Start();

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Services.Dispose();
            base.OnExit(e);
        }

        private void SetupCleanupHandlers()
        {
            Application.Current.Exit += (s, e) => Cleanup();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Cleanup();
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Cleanup();
        }

        private void Cleanup()
        {
            sideQuestService?.Stop();
        }
        
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            
            // Services
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<ISideQuest, SideQuest>();
            services.AddTransient<IMacroStateSerializer, MacroStateSerializer>();

            // ViewModels

            return services.BuildServiceProvider();
        }


    }
}
