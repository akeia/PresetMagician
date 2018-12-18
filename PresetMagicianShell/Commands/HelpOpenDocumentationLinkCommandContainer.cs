﻿using System.Diagnostics;
using System.Threading.Tasks;
using Catel.IoC;
using Catel.Logging;
using Catel.MVVM;
using Catel.Services;

// ReSharper disable once CheckNamespace
namespace PresetMagicianShell
{
    // ReSharper disable once UnusedMember.Global
    public class HelpOpenDocumentationLinkCommandContainer : CommandContainerBase
    {
        public HelpOpenDocumentationLinkCommandContainer(ICommandManager commandManager,
            IUIVisualizerService uiVisualizerService,
            IViewModelFactory viewModelFactory, IServiceLocator serviceLocator)
            : base(Commands.Help.OpenDocumentationLink, commandManager)
        {
        }

        protected override async Task ExecuteAsync(object parameter)
        {
            base.Execute(parameter);
            Process.Start(Settings.Links.Documentation);
        }
    }
}