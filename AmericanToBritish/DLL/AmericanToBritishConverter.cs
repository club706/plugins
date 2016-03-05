﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Nikse.SubtitleEdit.PluginLogic
{
    public class AmericanToBritishConverter
    {
        private IList<Regex> _regexList = new List<Regex>();
        private IList<string> _replaceList = new List<string>();

        public string FixText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;
            for (int index = 0; index < _regexList.Count; index++)
            {
                var regex = _regexList[index];
                if (regex.IsMatch(text))
                {
                    text = regex.Replace(text, _replaceList[index]);
                }
            }
            return FixMissChangedWord(text);
        }

        public bool LoadBuiltInWords()
        {
            bool success = false;
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Nikse.SubtitleEdit.PluginLogic.WordList.xml"))
            {
                success = LoadWordsToLists(stream);
            }
            return success;
        }

        public bool LoadLocalWords(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            bool success = false;
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                success = LoadWordsToLists(fs);
            }
            return success;
        }

        private bool LoadWordsToLists(Stream stream)
        {
            _regexList = new List<Regex>();
            _replaceList = new List<string>();

            // always reload list
            XDocument xDoc;
            try
            {
                stream.Position = 0;
                xDoc = XDocument.Load(stream);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return false;
            }
            if (xDoc?.Root.Name == "Words")
            {
                foreach (XElement xe in xDoc.Root.Elements("Word"))
                {
                    if (xe.Attribute("us")?.Value.Length > 1 && xe.Attribute("br")?.Value.Length > 1)
                    {
                        string american = xe.Attribute("us").Value;
                        string british = xe.Attribute("br").Value;

                        _regexList.Add(new Regex("\\b" + american + "\\b", RegexOptions.Compiled));
                        _replaceList.Add(british);

                        _regexList.Add(new Regex("\\b" + american.ToUpperInvariant() + "\\b", RegexOptions.Compiled));
                        _replaceList.Add(british.ToUpperInvariant());

                        _regexList.Add(new Regex("\\b" + char.ToUpperInvariant(american[0]) + american.Substring(1) + "\\b", RegexOptions.Compiled));
                        if (british.Length > 1)
                            _replaceList.Add(char.ToUpperInvariant(british[0]) + british.Substring(1));
                        else
                            _replaceList.Add(british.ToUpper());
                    }
                }
            }
            return true;
        }

        private string FixMissChangedWord(string s)
        {
            var idx = s.IndexOf("<font", StringComparison.OrdinalIgnoreCase);
            while (idx >= 0) // Fix colour => color
            {
                var endIdx = s.IndexOf('>', idx + 5);
                if (endIdx < 5)
                    break;
                var tag = s.Substring(idx, endIdx - idx);
                tag = tag.Replace("colour", "color");
                tag = tag.Replace("COLOUR", "COLOR");
                tag = tag.Replace("Colour", "Color");
                s = s.Remove(idx, endIdx - idx).Insert(idx, tag);
                idx = s.IndexOf("<font", endIdx + 1, StringComparison.OrdinalIgnoreCase);
            }
            return s;
        }
    }
}