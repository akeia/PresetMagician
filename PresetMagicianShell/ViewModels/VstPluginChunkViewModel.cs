﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catel.Data;
using Catel.MVVM;
using PresetMagicianShell.Models;
using PresetMagicianShell.Services.Interfaces;

namespace PresetMagicianShell.ViewModels
{
    public class VstPluginChunkViewModel : VstPluginViewModel
    {
        public VstPluginChunkViewModel(Models.Plugin plugin, IVstService vstService) : base(vstService)
        {
            Plugin = plugin;
            OpenWithHxD = new TaskCommand(OnOpenWithHxDExecute);
        }

        public TaskCommand OpenWithHxD { get; set; }

        private async Task OnOpenWithHxDExecute ()
        {
            
                var tempFile = Path.GetTempFileName();
                File.WriteAllBytes(tempFile, Plugin.ChunkMemoryStream.ToArray());
          
            var process = new Process
            {
                StartInfo =
                {
                    FileName = @"C:\Program Files\HxD\HxD.exe",
                    Arguments = tempFile
                    
                }
            };

            process.Start();


        }

    }
}