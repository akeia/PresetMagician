using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catel.IoC;
using Catel.MVVM;
using PresetMagician.Core.Models;

namespace PresetMagician
{
    // ReSharper disable once UnusedMember.Global
    public class PluginForceReloadMetadataCommandContainer: AbstractScanPluginsCommandContainer
    {
        public PluginForceReloadMetadataCommandContainer(ICommandManager commandManager,
            IServiceLocator serviceLocator)
            : base(Commands.Plugin.ForceReloadMetadata, commandManager, serviceLocator)
        {
            _globalFrontendService.SelectedPlugins.CollectionChanged += OnPluginsListChanged;
        }

        protected override List<Plugin> GetPluginsToScan()
        {
            return (from plugin in _globalFrontendService.SelectedPlugins where plugin.IsEnabled select plugin)
                .ToList();
        }

        protected override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) &&
                   _globalFrontendService.SelectedPlugins.Count > 0;
        }

        protected override async Task ExecuteThreaded(object parameter)
        {
            SkipPresetLoading = true;
            foreach (var plugin in _globalFrontendService.SelectedPlugins)
            {
                if (plugin.PluginLocation != null)
                {
                    plugin.PluginLocation.LastMetadataAnalysisVersion = null;
                }

                foreach (var pluginLocation in plugin.PluginLocations)
                {
                    pluginLocation.LastMetadataAnalysisVersion = null;
                }
                
                plugin.UpdateRequiresMetadataScanFlag(GlobalService.PresetMagicianVersion);
            }
            await base.ExecuteThreaded(parameter);
        }
    }
}