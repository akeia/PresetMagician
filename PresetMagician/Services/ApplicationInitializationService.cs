﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationInitializationService.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Anotar.Catel;
using Catel;
using Catel.IoC;
using Catel.Logging;
using Catel.MVVM;
using Catel.Services;
using Catel.Threading;
using MethodTimer;
using Orc.Scheduling;
using Orc.Squirrel;
using Orchestra.Services;
using PresetMagician.Core.Commands.Plugin;
using PresetMagician.Core.Interfaces;
using PresetMagician.Legacy.Services;
using PresetMagician.Legacy.Services.EventArgs;
using PresetMagician.Services.Interfaces;
using PresetMagician.ViewModels;

namespace PresetMagician.Services
{
    public class ApplicationInitializationService : ApplicationInitializationServiceBase
    {
        #region Fields

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IServiceLocator _serviceLocator;
        private readonly ICommandManager _commandManager;
        private readonly IUIVisualizerService _uiVisualizerService;
        private FrontendService _frontendService;
        private readonly SplashScreenService _splashScreenService;
        private SquirrelResult _squirrelResult;

        #endregion Fields

        #region Constructors

        public ApplicationInitializationService(IServiceLocator serviceLocator,
            ICommandManager commandManager, IUIVisualizerService uiVisualizerService,
            IViewModelFactory viewModelFactory)
        {
            Argument.IsNotNull(() => serviceLocator);
            Argument.IsNotNull(() => commandManager);
            Argument.IsNotNull(() => uiVisualizerService);
            Argument.IsNotNull(() => viewModelFactory);

            _serviceLocator = serviceLocator;
            _commandManager = commandManager;
            _uiVisualizerService = uiVisualizerService;

            _splashScreenService = serviceLocator.ResolveType<ISplashScreenService>() as SplashScreenService;


            _squirrelResult = new SquirrelResult();
        }

        #endregion Constructors

        #region Methods

        [Time]
        public override async Task InitializeBeforeCreatingShellAsync()
        {
            // Non-async first
            FrontendInitializer.RegisterTypes(_serviceLocator);
            FrontendInitializer.Initialize(_serviceLocator);
            _frontendService = _serviceLocator.ResolveType<FrontendService>();
            _splashScreenService.Action = "Loading configuration…";
            LoadConfiguration();


            _serviceLocator.ResolveType<IApplicationService>().StartProcessPool();

            _splashScreenService.Action = "Migrating database…";
            if (File.Exists(FileLocations.LegacyDatabasePath))
            {
                await TaskHelper.Run(() =>
                {
                    _serviceLocator.RegisterType<Ef6MigrationService, Ef6MigrationService>();
                    var migrationService = _serviceLocator.ResolveType<Ef6MigrationService>();
                    migrationService.MigrationProgressUpdated += delegate(object sender, MigrationProgessEventArgs args)
                    {
                        _splashScreenService.Action = "Migrating database…" + args.Progress;
                    };
                    migrationService.LoadData();
                    migrationService.MigratePlugins();
                }).ConfigureAwait(false);
            }


            _splashScreenService.Action = "Loading database…";

            _frontendService.InitializeCommands();
        }

        [Time]
        public override async Task InitializeBeforeShowingShellAsync()
        {
            var vstService = _serviceLocator.ResolveType<IVstService>();
            vstService.Load();

            _splashScreenService.Action = "Almost there…";

            _frontendService.SetupCollectionSynchronizations();
        }

        [Time]
        public override async Task InitializeAfterShowingShellAsync()
        {
            LogTo.Debug("Running initialization after showing the shell");

            var serviceLocator = ServiceLocator.Default;
            var licenseService = serviceLocator.ResolveType<ILicenseService>();
            if (!licenseService.CheckLicense())
            {
                LogTo.Debug("No valid license found, showing registration dialog");
                StartRegistration();
            }


            await base.InitializeAfterShowingShellAsync();

            LogTo.Debug("Scheduling update check task");
            var schedulerService = _serviceLocator.ResolveType<ISchedulingService>();

            var updateCheckTask = new ScheduledTask
            {
                Name = "Update Check task",
                Start = DateTime.Now.AddMinutes(1),
                Action = CheckForUpdatesAsync
            };

            schedulerService.AddScheduledTask(updateCheckTask);

            TaskHelper.Run(() => { _serviceLocator.ResolveType<RefreshPluginsCommand>().ExecuteAsync(); });
        }

        private async Task StartRegistration()
        {
            await _uiVisualizerService.ShowDialogAsync<RegistrationViewModel>();
        }

        [Time]
        private async Task CheckForUpdatesAsync()
        {
            Log.Info("Checking for updates…");

            var updateService = _serviceLocator.ResolveType<IUpdateService>();


            _squirrelResult = await updateService.CheckForUpdatesAsync(new SquirrelContext());
        }

        public SquirrelResult getSquirrel()
        {
            return _squirrelResult;
        }


        [Time]
        private void LoadConfiguration()
        {
            var runtimeConfigurationService = _serviceLocator.ResolveType<IRuntimeConfigurationService>();
            runtimeConfigurationService.Load();
        }

        #endregion Methods
    }
}