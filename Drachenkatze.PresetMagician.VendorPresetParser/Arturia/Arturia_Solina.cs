using System.Collections.Generic;
using JetBrains.Annotations;

namespace Drachenkatze.PresetMagician.VendorPresetParser.Arturia
{
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class Arturia_Solina: Arturia, IVendorPresetParser
    {
        public override List<int> SupportedPlugins => new List<int> { 1399811122 };

        public void ScanBanks()
        {
            var instruments = new List<string> {"Solina"};
            ScanPresets(instruments);
        }
    }
}