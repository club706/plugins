﻿using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace Nikse.SubtitleEdit.PluginLogic
{
    internal class Configuration
    {
        public static double CurrentFrameRate = 23.976;
        public static string ListViewLineSeparatorString = "<br />";
        private readonly string _configFile;

        private XElement _xmlSetting;

        public bool SkipNarrator { get; set; }

        public bool SkipMoods { get; set; }

        public bool SkipDialogs { get; set; }

        public int MaxLineLength { get; set; }

        public Configuration()
        {
            // Load configs from files
            _configFile = GetSettingsFileName();
            LoadConfigurations();
        }

        private static string GetSettingsFileName()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            if (path.StartsWith(@"file:\", StringComparison.Ordinal))
                path = path.Substring(6);
            path = Path.Combine(path, "Plugins");
            if (!Directory.Exists(path))
                path = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Subtitle Edit"), "Plugins");
            return Path.Combine(path, "SeLinesUnbreaker.xml");
        }

        private void LoadConfigurations()
        {
            try
            {
                // load
                if (File.Exists(_configFile))
                {
                    _xmlSetting = XElement.Load(_configFile);

                    SkipDialogs = Convert.ToBoolean(_xmlSetting.Element("SkipDialog").Value);
                    SkipNarrator = Convert.ToBoolean(_xmlSetting.Element("SkipNarrator").Value);
                    SkipMoods = Convert.ToBoolean(_xmlSetting.Element("SkipMoods").Value);

                    int.TryParse(_xmlSetting.Element("Shorterthan").Value, out int val);
                    if (val > 0)
                    {
                        MaxLineLength = val;
                    }
                    else
                    {
                        MaxLineLength = 35;
                    }
                }
                else
                {
                    // create new one & save
                    _xmlSetting = new XElement("SeLinesUnbreaker",
                        new XElement("Shorterthan", MaxLineLength),
                        new XElement("SkipDialog", true),
                        new XElement("SkipNarrator", true),
                        new XElement("SkipMoods", false)
                        );
                    _xmlSetting.Save(_configFile);

                    LoadDefaultOptions();
                }
            }
            catch
            {
                LoadDefaultOptions();
            }
        }

        public void LoadDefaultOptions()
        {
            SkipDialogs = true;
            SkipNarrator = true;
            SkipMoods = false;
        }

        public void SaveConfiguration()
        {
            // save settings
            if (_xmlSetting == null)
            {
                return;
            }
            _xmlSetting.Element("Shorterthan").Value = MaxLineLength.ToString();
            _xmlSetting.Element("SkipMoods").Value = SkipMoods.ToString();
            _xmlSetting.Element("SkipNarrator").Value = SkipNarrator.ToString();
            _xmlSetting.Element("SkipDialog").Value = SkipDialogs.ToString();
            try
            {
                _xmlSetting.Save(_configFile);
            }
            catch
            {
                // ignore
            }
        }

    }
}
