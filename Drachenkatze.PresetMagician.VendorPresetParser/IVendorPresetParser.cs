﻿using Drachenkatze.PresetMagician.VSTHost.VST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drachenkatze.PresetMagician.VendorPresetParser
{
    public interface IVendorPresetParser
    {
        List<int> SupportedPlugins { get; }
        List<PresetBank> Banks { get; }
        VSTPlugin VstPlugin { get; set; }
    }
}