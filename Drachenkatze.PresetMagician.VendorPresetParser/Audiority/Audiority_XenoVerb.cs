using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace Drachenkatze.PresetMagician.VendorPresetParser.Audiority
{
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class Audiority_XenoVerb : Audiority, IVendorPresetParser
    {
        public override List<int> SupportedPlugins => new List<int> {1199076962};


        public void ScanBanks()
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                @"Audiority\Presets\XenoVerb");

            DoScan(RootBank, directory);
        }
    }
}