﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PresetMagician.Core.Models;
using PresetMagician.VendorPresetParser.Common;

namespace PresetMagician.VendorPresetParser.D16_Group
{
    public abstract class D16Group : AbstractVendorPresetParser
    {
        protected abstract string XmlPluginName { get; }
        protected abstract string Extension { get; }

        protected async Task<int> ProcessD16PkgArchive(string archiveName, PresetBank bank, bool persist = true)
        {
            var count = 0;
            Logger.Debug($"ProcessD16PKGArchive {archiveName}");

            if (!File.Exists(archiveName))
            {
                Logger.Error($"Could not find the file {archiveName}");
                return 0;
            }
            
            using (var archive = ZipFile.OpenRead(archiveName))
            {
                var entry = archive.GetEntry("content");

                if (entry == null)
                {
                    Logger.Error($"Could not find the entry 'content' within archive {archiveName}");
                    return 0;
                }
                var contentStream = entry.Open();

                var contentArchive = new ZipArchive(contentStream);

                foreach (var presetEntry in contentArchive.Entries)
                {
                    if (presetEntry.Name == "__desc__")
                    {
                        continue;
                    }

                    count++;

                    if (persist)
                    {
                        var ms = new MemoryStream();
                        presetEntry.Open().CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        var presetData = Encoding.UTF8.GetString(ms.ToArray());

                        var presetInfo = GetPreset(presetEntry.Name, presetData, bank);
                        presetInfo.preset.SourceFile = archiveName + ":" + presetEntry.Name;
                        await DataPersistence.PersistPreset(presetInfo.preset, presetInfo.presetData);
                    }
                }
            }

            return count;
        }

        protected async Task<int> ProcessPresetDirectory(string presetDirectory, PresetBank bank, bool persist = true)
        {
            var count = 0;
            if (!Directory.Exists(presetDirectory))
            {
                return count;
            }

            Logger.Debug($"ProcessPresetDirectory {presetDirectory}");
            var dirInfo = new DirectoryInfo(presetDirectory);

            foreach (var file in dirInfo.EnumerateFiles("*" + Extension))
            {
                count++;

                if (persist)
                {
                    var presetData = File.ReadAllText(file.FullName);
                    var presetInfo = GetPreset(file.Name, presetData, bank);
                    presetInfo.preset.SourceFile = file.FullName;
                    await DataPersistence.PersistPreset(presetInfo.preset, presetInfo.presetData);
                }
            }

            foreach (var directory in dirInfo.EnumerateDirectories())
            {
                count += await ProcessPresetDirectory(directory.FullName, bank.CreateRecursive(directory.Name),
                    persist);
            }

            return count;
        }

        protected virtual (PresetParserMetadata preset, byte[] presetData) GetPreset(string name, string presetData,
            PresetBank presetBank)
        {
            var pluginState = new XElement("PluginState");
            pluginState.SetAttributeValue("application", XmlPluginName);
            var parametersState = new XElement("ParametersState");
            var midiControlMap = new XElement("MidiControlMap");
            midiControlMap.SetAttributeValue("name", XmlPluginName);

            pluginState.Add(parametersState);

            var xmlPreset = XDocument.Parse(presetData);
            var presetElement = xmlPreset.Element("Preset");
            presetElement.SetAttributeValue("name", name.Replace(Extension, ""));

            var preset = new PresetParserMetadata
            {
                PresetName = name.Replace(Extension, ""), Plugin = PluginInstance.Plugin, BankPath = presetBank.BankPath
            };

            var tagsAttribute = presetElement.Attribute("tags");

            if (!(tagsAttribute is null))
            {
                var modes = GetModes(tagsAttribute.Value);

                foreach (var modeName in modes)
                {
                    preset.Characteristics.Add(new Characteristic {CharacteristicName = modeName});
                }
            }

            presetElement.SetAttributeValue("tags", null);
            presetElement.SetAttributeValue("version", null);


            parametersState.Add(presetElement);


            var ms = RecursiveVC2Parser.WriteVC2(pluginState.ToString());

            return (preset, ms.ToArray());
        }

        protected virtual void PostProcessXml(XElement presetElement)
        {
        }

        protected List<string> GetModes(string tags)
        {
            var modes = new List<string>();

            var dict = ExtractTags(tags);
            if (dict.ContainsKey("Type"))
            {
                modes.AddRange(dict["Type"]);
            }

            if (dict.ContainsKey("Mode"))
            {
                modes.AddRange(dict["Mode"]);
            }

            return modes;
        }

        protected Dictionary<string, string[]> ExtractTags(string tags)
        {
            return tags.Split(';')
                .Select(x => x.Split(':'))
                .ToDictionary(x => x[0], x => x[1].Split('|'));
        }

        protected string GetFactoryBankPath(string factoryBankPath)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                factoryBankPath);
        }

        protected string GetUserBankPath(string userBankPath)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), userBankPath);
        }
    }
}