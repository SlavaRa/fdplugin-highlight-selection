using System;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel;
using PluginCore.Utilities;
using PluginCore.Managers;
using PluginCore.Helpers;
using PluginCore;
using PluginCore.FRService;
using System.Collections.Generic;
using ScintillaNet;
using System.Windows.Forms;

namespace HighlightSelection
{
	public class PluginMain : IPlugin
	{
		private string pluginName = "Highlight Selection";
		private string pluginGuid = "1f387fab-421b-42ac-a985-72a03534f731";
		private string pluginHelp = "";
		private string pluginDesc = "A plugin to highlight your selected text";
		private string pluginAuth = "mike.cann@gmail.com";
		private string settingFilename;
		private Settings settingObject;
        private Timer timer;
        private int prevPos;
        private string prevToken;

		#region Required Properties

        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public int Api
        {
            get { return 1; }
        }

		/// <summary>
		/// Name of the plugin
		/// </summary> 
		public string Name
		{
			get { return pluginName; }
		}

		/// <summary>
		/// GUID of the plugin
		/// </summary>
		public string Guid
		{
			get { return pluginGuid; }
		}

		/// <summary>
		/// Author of the plugin
		/// </summary> 
		public string Author
		{
			get { return pluginAuth; }
		}

		/// <summary>
		/// Description of the plugin
		/// </summary> 
		public string Description
		{
			get { return pluginDesc; }
		}

		/// <summary>
		/// Web address for help
		/// </summary> 
		public string Help
		{
			get { return pluginHelp; }
		}

		/// <summary>
		/// Object that contains the settings
		/// </summary>
		[Browsable(false)]
		public Object Settings
		{
			get { return settingObject; }
		}

		#endregion

		#region Required Methods

		/// <summary>
		/// Initializes the plugin
		/// </summary>
		public void Initialize()
		{
	        InitBasics();
			LoadSettings();
			AddEventHandlers();
            InitTimer();
		}

		/// <summary>
		/// Disposes the plugin
		/// </summary>
		public void Dispose()
		{
            timer.Dispose();
            timer = null;
		    SaveSettings();
		}

		/// <summary>
		/// Handles the incoming events
		/// </summary>
		public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority)
		{
            ITabbedDocument doc = PluginBase.MainForm.CurrentDocument;
			switch (e.Type)
			{
				// Catches FileSwitch event and displays the filename it in the PluginUI.
				case EventType.FileSwitch:
					if (doc.IsEditable)
					{
                        doc.SciControl.DoubleClick += onSciDoubleClick;
						doc.SciControl.Modified += onSciModified;
					}
				    break;
				case EventType.FileSave:
                    if (!settingObject.HighlightUnderCursor) RemoveHighlights(doc.SciControl);
                    else UpdateHighlightUnderCursor(doc.SciControl);
				    break;
			}
		}

		#endregion

		#region Custom Methods

		/// <summary>
		/// Initializes important variables
		/// </summary>
        private void InitBasics()
		{
			string dataPath = Path.Combine(PathHelper.DataDir, "HighlightSelection");
			if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
			settingFilename = Path.Combine(dataPath, "Settings.fdb");
		}

		/// <summary>
		/// Adds the required event handlers
		/// </summary> 
        private void AddEventHandlers()
		{
			EventManager.AddEventHandler(this, EventType.FileSwitch | EventType.FileSave);
		}

		/// <summary>
		/// Loads the plugin settings
		/// </summary>
        private void LoadSettings()
		{
			settingObject = new Settings();
            if (!File.Exists(settingFilename))
            {
                settingObject.HighlightColor = System.Drawing.Color.Red;
                settingObject.AddLineMarker = HighlightSelection.Settings.DEFAULT_ADD_LINE_MARKER;
                settingObject.MatchCase = HighlightSelection.Settings.DEFAULT_MATCH_CASE;
                settingObject.WholeWords = HighlightSelection.Settings.DEFAULT_WHOLE_WORD;
                settingObject.HighlightUnderCursor = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR;
                SaveSettings();
            }
            else settingObject = (Settings)ObjectSerializer.Deserialize(settingFilename, settingObject);
		}

		/// <summary>
		/// Saves the plugin settings
		/// </summary>
		private void SaveSettings()
		{
			ObjectSerializer.Serialize(settingFilename, settingObject);
		}

        /// <summary>
        /// 
        /// </summary>
        private void InitTimer()
        {
            prevPos = -1;
            timer = new Timer();
            timer.Interval = 400;
            timer.Tick += onTimerTick;
            if (settingObject.HighlightUnderCursor) timer.Start();
        }

        /// <summary>
        /// DoubleClick handler
        /// </summary>
        private void onSciDoubleClick(ScintillaControl sender)
        {
            RemoveHighlights(sender);
            prevToken = sender.SelText.Trim();
            AddHighlights(sender, GetResults(sender, prevToken));
        }

        /// <summary>
        /// Modified Handler
        /// </summary>
        private void onSciModified(ScintillaControl sender, int position, int modificationType, string text, int length, int linesAdded, int line, int intfoldLevelNow, int foldLevelPrev)
        {
            RemoveHighlights(sender);
        }

        /// <summary>
        /// Adds highlights to the correct sci control
        /// </summary>
        private void AddHighlights(ScintillaControl sci, List<SearchMatch> matches)
        {
            if (matches == null) return;
            foreach (SearchMatch match in matches)
            {
                int start = sci.MBSafePosition(match.Index);
                int end = start + sci.MBSafeTextLength(match.Value);
                int line = sci.LineFromPosition(start);
                int position = start;
                int es = sci.EndStyled;
                int mask = 1 << sci.StyleBits;

                sci.SetIndicStyle(0, (int)ScintillaNet.Enums.IndicatorStyle.Max);
                sci.SetIndicFore(0, DataConverter.ColorToInt32(settingObject.HighlightColor));
                sci.StartStyling(position, mask);
                sci.SetStyling(end - start, mask);
                sci.StartStyling(es, mask - 1);

                if (settingObject.AddLineMarker)
                {
                    sci.MarkerAdd(line, 2);
                    sci.MarkerSetBack(2, DataConverter.ColorToInt32(settingObject.HighlightColor));
                }
            }
            prevPos = sci.CurrentPos;
            if (!settingObject.HighlightUnderCursor) timer.Start();
        }

        /// <summary>
        /// Removes the highlights from the correct sci control
        /// </summary>
        private void RemoveHighlights(ScintillaControl sci)
        {
            int es = sci.EndStyled;
            int mask = (1 << sci.StyleBits);
            sci.StartStyling(0, mask);
            sci.SetStyling(sci.TextLength, 0);
            sci.StartStyling(es, mask - 1);
            if (settingObject.AddLineMarker) sci.MarkerDeleteAll(2);
            prevPos = -1;
            prevToken = string.Empty;
            if (!settingObject.HighlightUnderCursor) timer.Stop();
        }

        /// <summary>
        /// Gets search results for a sci control
        /// </summary>
        private List<SearchMatch> GetResults(ScintillaControl sci, string text)
        {
            if (string.IsNullOrEmpty(text) || Regex.IsMatch(text, "[^a-zA-Z0-9_]")) return null;
            FRSearch search = new FRSearch(text);
            search.WholeWord = settingObject.WholeWords;
            search.NoCase = !settingObject.MatchCase;
            search.Filter = SearchFilter.None;
            return search.Matches(sci.Text);
        }

        private void onTimerTick(object sender, EventArgs e)
        {
            ScintillaControl Sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (Sci == null) return;
            if (settingObject.HighlightUnderCursor) UpdateHighlightUnderCursor(Sci);
            else if (Sci.CurrentPos != prevPos) RemoveHighlights(Sci);
        }

        private void UpdateHighlightUnderCursor(ScintillaControl Sci)
        {
            string newToken = Sci.GetWordFromPosition(Sci.CurrentPos);
            if (!string.IsNullOrEmpty(newToken)) newToken = newToken.Trim();
            if (newToken == prevToken) return;
            RemoveHighlights(Sci);
            prevToken = newToken;
            if (!string.IsNullOrEmpty(prevToken)) AddHighlights(Sci, GetResults(Sci, prevToken));
        }

		#endregion
    }
}