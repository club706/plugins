﻿using System;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.PluginLogic
{
    internal partial class MainForm : Form
    {
        public string FixedSubtitle { get; private set; }

        private Subtitle _subtitle;
        private string _fileName;
        private bool _allowFixes;
        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(Subtitle sub, string fileName, string description)
            : this()
        {
            // TODO: Complete member initialization
            this._subtitle = sub;
            this._fileName = fileName;

            this.Resize += delegate
            {
                listViewFixes.Columns[listViewFixes.Columns.Count - 1].Width = -2;
            };
            this.listViewFixes.SizeChanged += delegate
            {
                var width = listViewFixes.Width / 2 - 100;
                columnHeaderActual.Width = width;
                columnHeaderAfter.Width = width;
            };
            FindNarrators();
        }

        public void FindNarrators()
        {
            listViewFixes.BeginUpdate();
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                var p = _subtitle.Paragraphs[i];
                var text = p.Text;
                var before = text;

                if (text.IndexOf('(') < 0 && text.IndexOf('[') < 0)
                    continue;

                var idx = text.IndexOf('(');
                while (idx >= 0)
                {
                    var endIdx = text.IndexOf(')', idx + 1);
                    if (endIdx < idx)
                        break;
                    var mood = text.Substring(idx, endIdx - idx + 1).Trim('(', ' ', ')');
                    if (Utilities.FixIfInList(mood))
                    {
                        // todo: if name contains <i>:, note there could be a italic tag at begining
                        text = text.Remove(idx, endIdx - idx + 1).TrimStart(':', ' ');
                        if (text.Length > idx && text[idx] != ':')
                            text = text.Insert(idx, mood + ": ");
                        else
                            text = text.Insert(idx, mood);
                        idx = text.IndexOf('(');
                    }
                    else
                    {
                        idx = text.IndexOf('(', endIdx + 1);
                    }
                }

                idx = text.IndexOf('[');
                while (idx >= 0)
                {
                    var endIdx = text.IndexOf(']', idx + 1);
                    if (endIdx < idx)
                        break;
                    var mood = text.Substring(idx, endIdx - idx + 1);
                    mood = mood.Substring(1);
                    mood = mood.Substring(0, mood.Length - 1);
                    if (Utilities.FixIfInList(mood))
                    {
                        text = text.Remove(idx, endIdx - idx + 1).TrimStart('(', ' ');
                        if (text.Length > idx && text[idx] != ':')
                            text = text.Insert(idx, mood + ":");
                        else
                            text = text.Insert(idx, mood);
                        idx = text.IndexOf('[');
                    }
                    else
                    {
                        idx = text.IndexOf('[', endIdx + 1);
                    }
                }
                text = AddHyphenOnBothLine(text);
                if (text != before && !AllowFix(p))
                {
                    // add hyphen is both contains narrator
                    AddFixToListView(p, before, text);
                }
                else
                {
                    p.Text = text;
                }
            }
            listViewFixes.EndUpdate();
        }

        private string AddHyphenOnBothLine(string text)
        {
            if (!text.Contains(Environment.NewLine) || Utilities.CountTagInText(text, ':') < 1)
                return text;

            const string endLineChars = ".?)]!";
            var noTagText = Utilities.RemoveHtmlTags(text);
            bool addHyphen = false;
            var noTagLines = noTagText.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < noTagLines.Length; i++)
            {
                var line = noTagLines[i];
                var preLine = i - 1 < 0 ? null : noTagLines[i - 1];
                var idx = line.IndexOf(':');
                addHyphen = ((idx >= 0 && !Utilities.IsBetweenNumbers(line, idx)) && (preLine == null || endLineChars.IndexOf(preLine[preLine.Length - 1]) >= 0)) ? true : false;
            }
            /*
            foreach (var noTagLine in noTagText.Replace("\r\n", "\n").Split('\n'))
            {
                if (noTagLine.Length == 0)
                    return text;
                var idx = noTagLine.IndexOf(':');
                addHyphen = (idx >= 0 && !Utilities.IsBetweenNumbers(noTagLine, idx)) && (endLineChars.IndexOf(noTagLine[noTagLine.Length - 1]) >= 0) ? true : false;
            }*/
            if (addHyphen && (noTagLines[0].Length > 2 && noTagLines[1].Length > 2))
            {
                if (noTagLines[0][0] != '-')
                    text = "- " + text;

                if (!noTagLines[1].Contains("\r\n-"))
                    text = text.Insert(text.IndexOf(Environment.NewLine) + 2, "- ");
            }
            return text;
        }

        private bool AllowFix(Paragraph p)
        {
            if (!_allowFixes)
                return false;
            string ln = p.Number.ToString();
            foreach (ListViewItem item in listViewFixes.Items)
            {
                if (item.SubItems[1].Text == ln)
                    return item.Checked;
            }
            return false;
        }

        private void buttonToNarrator_Click(object sender, EventArgs e)
        {
            var len = Utilities.ListNames.Count;
            var name = this.textBoxName.Text;
            name = name.Trim();
            if (name.Length == 0)
                return;
            Utilities.AddNameToList(name);
            if (len != Utilities.ListNames.Count)
            {
                this.listViewFixes.BeginUpdate();
                this.listViewFixes.Items.Clear();
                this.listViewFixes.EndUpdate();
                FindNarrators();
            }
            //TODO: Update list view after adding new naem
        }

        private void AddFixToListView(Paragraph p, string before, string after)
        {
            var item = new ListViewItem() { Checked = true, UseItemStyleForSubItems = true, Tag = p };
            var subItem = new ListViewItem.ListViewSubItem(item, p.Number.ToString());
            item.SubItems.Add(subItem);

            subItem = new ListViewItem.ListViewSubItem(item, before.Replace(Environment.NewLine,
                Configuration.ListViewLineSeparatorString));
            item.SubItems.Add(subItem);

            subItem = new ListViewItem.ListViewSubItem(item, after.Replace(Environment.NewLine,
                Configuration.ListViewLineSeparatorString));
            item.SubItems.Add(subItem);

            listViewFixes.Items.Add(item);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            _allowFixes = true;
            FindNarrators();
            FixedSubtitle = _subtitle.ToText(new SubRip());
        }

        private void buttonGetNames_Click(object sender, EventArgs e)
        {
            this.Hide();
            // store the names in list on constructor runtime instead of loading it each time
            using (var formGetName = new GetNames(this, this._subtitle)) // send the loaded list 
            {
                if (formGetName.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    FindNarrators();
                }
            }
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            _allowFixes = true;
            FindNarrators();
            FixedSubtitle = _subtitle.ToText(new SubRip());
            _allowFixes = !_allowFixes;
            this.listViewFixes.Clear();
            FindNarrators();
        }
    }
}