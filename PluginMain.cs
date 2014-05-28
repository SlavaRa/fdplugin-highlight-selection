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
using ASCompletion.Completion;
using ASCompletion.Model;
using ASCompletion.Context;

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
        private Timer highlightUnderCursorTimer;
        private Timer tempo;
        private int prevPos;
        private ASResult prevResult;
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
            InitTimers();
            UpdateHighlightUnderCursorTimer();
		}

		/// <summary>
		/// Disposes the plugin
		/// </summary>
		public void Dispose()
		{
            highlightUnderCursorTimer.Dispose();
            highlightUnderCursorTimer = null;
            tempo.Dispose();
            tempo = null;
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
                    RemoveHighlights(doc.SciControl);
					if (doc.IsEditable)
					{
                        doc.SciControl.DoubleClick += onSciDoubleClick;
						doc.SciControl.Modified += onSciModified;
					}
				    break;
				case EventType.FileSave:
                    RemoveHighlights(doc.SciControl);
                    break;
                case EventType.SettingChanged:
                    UpdateHighlightUnderCursorTimer();
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
			EventManager.AddEventHandler(this, EventType.FileSwitch | EventType.FileSave | EventType.SettingChanged);
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
                settingObject.HighlightStyle = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_STYLE;
                settingObject.HighlightUnderCursorEnabled = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR;
                settingObject.HighlightUnderCursorUpdateInteval = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL;
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
        private void InitTimers()
        {
            highlightUnderCursorTimer = new Timer();
            highlightUnderCursorTimer.Tick += highlighUnderCursorTimerTick;
            tempo = new Timer();
            tempo.Interval = PluginBase.Settings.DisplayDelay;
            tempo.Tick += onTempoTick;
            tempo.Start();
        }

        /// <summary>
        /// Checks if the file is ok for refactoring
        /// </summary>
        private bool IsValidFile(string file)
        {
            IProject project = PluginBase.CurrentProject;
            if (project == null) return false;
            string ext = Path.GetExtension(file);
            return (ext == ".as" || ext == ".hx" || ext == ".ls") && PluginBase.CurrentProject.DefaultSearchFilter.Contains(ext);
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateHighlightUnderCursorTimer()
        {
            if (settingObject.HighlightUnderCursorUpdateInteval < HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL)
                settingObject.HighlightUnderCursorUpdateInteval = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL;
            highlightUnderCursorTimer.Interval = settingObject.HighlightUnderCursorUpdateInteval;
            if (!settingObject.HighlightUnderCursorEnabled) highlightUnderCursorTimer.Stop();
        }

        /// <summary>
        /// DoubleClick handler
        /// </summary>
        private void onSciDoubleClick(ScintillaControl sender)
        {
            RemoveHighlights(sender);
            prevResult = null;
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
        private void AddHighlights(ScintillaControl Sci, List<SearchMatch> matches)
        {
            if (matches == null) return;
            int highlightStyle = (int)settingObject.HighlightStyle;
            int highlightColor = DataConverter.ColorToInt32(settingObject.HighlightColor);
            int es = Sci.EndStyled;
            int mask = 1 << Sci.StyleBits;
            bool addLineMarker = settingObject.AddLineMarker;
            foreach (SearchMatch match in matches)
            {
                int start = Sci.MBSafePosition(match.Index);
                int end = start + Sci.MBSafeTextLength(match.Value);
                int line = Sci.LineFromPosition(start);
                int position = start;
                Sci.SetIndicStyle(0, highlightStyle);
                Sci.SetIndicFore(0, highlightColor);
                Sci.StartStyling(position, mask);
                Sci.SetStyling(end - start, mask);
                Sci.StartStyling(es, mask - 1);
                if (addLineMarker)
                {
                    Sci.MarkerAdd(line, 2);
                    Sci.MarkerSetBack(2, highlightColor);
                }
            }
            prevPos = Sci.CurrentPos;
        }

        /// <summary>
        /// Removes the highlights from the correct sci control
        /// </summary>
        private void RemoveHighlights(ScintillaControl sci)
        {
            if (sci != null)
            {
                int es = sci.EndStyled;
                int mask = 1 << sci.StyleBits;
                sci.StartStyling(0, mask);
                sci.SetStyling(sci.TextLength, 0);
                sci.StartStyling(es, mask - 1);
                if (settingObject.AddLineMarker) sci.MarkerDeleteAll(2);
            }
            prevPos = -1;
            prevToken = string.Empty;
            prevResult = null;
        }

        /// <summary>
        /// Gets search results for a sci control
        /// </summary>
        private List<SearchMatch> GetResults(ScintillaControl Sci, string text)
        {
            if (string.IsNullOrEmpty(text) || Regex.IsMatch(text, "[^a-zA-Z0-9_$]")) return null;
            FRSearch search = new FRSearch(text);
            search.WholeWord = settingObject.WholeWords;
            search.NoCase = !settingObject.MatchCase;
            search.Filter = SearchFilter.None;
            return search.Matches(Sci.Text);
        }

        /// <summary>
        /// 
        /// </summary>
        private void onTempoTick(object sender, EventArgs e)
        {
            ScintillaControl Sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (Sci == null) return;
            int currentPos = Sci.CurrentPos;
            if (currentPos == prevPos) return;
            string newToken = Sci.GetWordFromPosition(currentPos);
            if (!settingObject.HighlightUnderCursorEnabled)
            {
                if (newToken != prevToken) RemoveHighlights(Sci);
            }
            else
            {
                if (prevPos != currentPos) highlightUnderCursorTimer.Stop();
                if (prevResult != null)
                {
                    ASResult result = null;
                    if (IsValidFile(PluginBase.MainForm.CurrentDocument.FileName)) result = ASComplete.GetExpressionType(Sci, Sci.WordEndPosition(currentPos, true));
                    if (result == null || result.IsNull() || result.Member != prevResult.Member || result.Type != prevResult.Type || result.Path != prevResult.Path)
                    {
                        RemoveHighlights(Sci);
                        highlightUnderCursorTimer.Start();
                    }
                }
                else
                {
                    RemoveHighlights(Sci);
                    highlightUnderCursorTimer.Start();
                }
            }
            prevPos = currentPos;
        }

        /// <summary>
        /// 
        /// </summary>
        private void highlighUnderCursorTimerTick(object sender, EventArgs e)
        {
            ScintillaControl Sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (Sci != null) UpdateHighlightUnderCursor(Sci);
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateHighlightUnderCursor(ScintillaControl Sci)
        {
            if (!IsValidFile(PluginBase.MainForm.CurrentDocument.FileName)) return;
            int currentPos = Sci.CurrentPos;
            string newToken = Sci.GetWordFromPosition(currentPos);
            if (!string.IsNullOrEmpty(newToken)) newToken = newToken.Trim();
            if (string.IsNullOrEmpty(newToken))
            {
                RemoveHighlights(Sci);
                return;
            }
            if (prevResult == null && prevToken == newToken) return;
            ASResult result = null;
            if (IsValidFile(PluginBase.MainForm.CurrentDocument.FileName)) result = ASComplete.GetExpressionType(Sci, Sci.WordEndPosition(currentPos, true));
            if (result == null || result.IsNull())
            {
                RemoveHighlights(Sci);
                return;
            }
            if (prevResult != null && (result.Member != prevResult.Member || result.Type != prevResult.Type || result.Path != prevResult.Path)) return;
            RemoveHighlights(Sci);
            prevToken = newToken;
            prevResult = result;
            List<SearchMatch> matches = FilterResults(GetResults(Sci, prevToken), result, Sci);
            if (matches == null) return;
            highlightUnderCursorTimer.Stop();
            AddHighlights(Sci, matches);
        }

        /// <summary>
        /// TODO slavara: IMPLEMENT ME
        /// </summary>
        /// <param name="matches"></param>
        /// <param name="exprType"></param>
        /// <param name="Sci"></param>
        /// <returns></returns>
        private List<SearchMatch> FilterResults(List<SearchMatch> matches, ASResult exprType, ScintillaControl Sci)
        {
            if (matches == null) return null;
            MemberModel contextMember = null;
            bool isLocalVar = false;
            if (exprType.Member != null)
            {
                if ((exprType.Member.Flags & (FlagType.LocalVar | FlagType.ParameterVar)) > 0)
                {
                    contextMember = exprType.Context.ContextFunction;
                    isLocalVar = true;
                }
                else contextMember = ASContext.Context.CurrentClass;
            }
            if (contextMember == null) return matches;
            List<SearchMatch> newMatches = new List<SearchMatch>();
            int lineFrom = contextMember.LineFrom;
            int lineTo = contextMember.LineTo;
            foreach (SearchMatch m in matches)
            {
                if (isLocalVar && (m.Line < lineFrom || m.Line > lineTo)) continue;
                exprType = ASComplete.GetExpressionType(Sci, Sci.WordEndPosition(m.Index, true));
                if (exprType != null && exprType.Member != null)
                {
                    if (!isLocalVar)
                    {
                        if ((exprType.Member.Flags & (FlagType.LocalVar | FlagType.ParameterVar)) == 0) newMatches.Add(m);
                    }
                    else if ((exprType.Member.Flags & (FlagType.LocalVar | FlagType.ParameterVar)) > 0) newMatches.Add(m);
                }
            }
            return newMatches;
        }

        #endregion
    }
}