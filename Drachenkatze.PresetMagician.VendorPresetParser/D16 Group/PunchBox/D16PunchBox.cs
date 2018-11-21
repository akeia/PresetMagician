﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drachenkatze.PresetMagician.VSTHost.VST;
using System.IO.Compression;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using GSF;

namespace Drachenkatze.PresetMagician.VendorPresetParser.D16_Group.PunchBox
{
    public class D16PunchBox : D16Group, IVendorPresetParser
    {
        private const String FactoryBankPath = @"D16 Group\PunchBox\Presets\Master.d16pkg";
        private const String UserBankPath = @"D16 Group\PunchBox\UserStore\Presets\Master";

        private const String BankNameFactory = "Factory";
        private const String BankNameUser = "User";

        public List<int> SupportedPlugins => new List<int> { 1347306072 };

        public List<PresetBank> Banks => GetBanks();

        private List<PresetBank> GetBanks ()
        {
            return new List<PresetBank> { GetFactoryPresets(), GetUserPresets() };
        }

        private PresetBank GetUserPresets ()
        {
            PresetBank userBank = new PresetBank();
            userBank.BankName = BankNameUser;

            return userBank;
        }

        private PresetBank GetFactoryPresets ()
        {
            PresetBank factoryBank = new PresetBank();
            factoryBank.BankName = BankNameFactory;

            using (ZipArchive archive = ZipFile.OpenRead(GetFactoryBankPath()))
            {
                var entry = archive.GetEntry("content");

                Stream contentStream = entry.Open();

                ZipArchive contentArchive = new ZipArchive(contentStream);

                foreach (ZipArchiveEntry presetEntry in contentArchive.Entries)
                {
                    if (presetEntry.Name == "__desc__")
                    {
                        continue;
                    }

                    MemoryStream ms = new MemoryStream();
                    presetEntry.Open().CopyTo(ms);


                    factoryBank.VSTPresets.Add(GetPreset(presetEntry.Name, ms, BankNameFactory));
                }

            }

            return factoryBank;
        }

        private VSTPreset GetPreset (String name, MemoryStream stream, String bankName)
        {
            
            stream.Seek(0, SeekOrigin.Begin);

            XElement pluginState = new XElement("PluginState");
            pluginState.SetAttributeValue("application", "PunchBox");
            XElement parametersState = new XElement("ParametersState");
            XElement midiControlMap = new XElement("MidiControlMap");
            midiControlMap.SetAttributeValue("name", "PunchBox");

          

            pluginState.Add(parametersState);
           
            
            
            XDocument xmlPreset = XDocument.Load(stream);
            var presetElement = xmlPreset.Element("Preset");
            presetElement.SetAttributeValue("name", name.Replace(".pbprs", ""));
            presetElement.SetAttributeValue("tags", null);
            presetElement.SetAttributeValue("version", null);

            foreach (var element in presetElement.Element("ExtraData").Element("Samples").Elements())
            {
                element.SetAttributeValue("origin", "Factory");
            }

            parametersState.Add(presetElement);

            VSTPreset vstPreset = new VSTPreset();
            vstPreset.PresetName = name.Replace(".pbprs", "");
            vstPreset.SetPlugin(VstPlugin);
            vstPreset.BankName = bankName;


            Debug.WriteLine(pluginState);
            //throw new InvalidCastException();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(new byte[] { 0x56, 0x43, 0x32, 0x21 },0,4);
                byte[] data = Encoding.UTF8.GetBytes(pluginState.ToString());
                ms.Write(LittleEndian.GetBytes(data.Length), 0, 4);
                ms.Write(data, 0, data.Length);
                ms.WriteByte(0);

                vstPreset.PresetData = ms.ToArray();
            }
            return vstPreset;
            
        }

        private String GetFactoryBankPath ()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), FactoryBankPath);
        }

        private String GetUserBankPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), UserBankPath);
        }
    }
}