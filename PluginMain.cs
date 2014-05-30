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
using ScintillaNet.Enums;
using System.Drawing;

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
		private Settings settings;
        private Timer highlightUnderCursorTimer;
        private Timer tempo;
        private int prevPos;
        private ASResult prevResult;
        private string prevToken;
        private readonly int MARKER_NUMBER = 0;

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
			get { return settings; }
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
                        ScintillaControl sci = doc.SciControl;
                        sci.MarkerDefine(MARKER_NUMBER, MarkerSymbol.Fullrect);
                        sci.DoubleClick += onSciDoubleClick;
                        sci.Modified += onSciModified;
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
            bool changed = false;
			settings = new Settings();
            if (!File.Exists(settingFilename))
            {
                settings.HighlightColor = Color.Red;
                settings.AddLineMarker = HighlightSelection.Settings.DEFAULT_ADD_LINE_MARKER;
                settings.MatchCase = HighlightSelection.Settings.DEFAULT_MATCH_CASE;
                settings.WholeWords = HighlightSelection.Settings.DEFAULT_WHOLE_WORD;
                settings.HighlightStyle = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_STYLE;
                settings.HighlightUnderCursorEnabled = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR;
                settings.HighlightUnderCursorUpdateInteval = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL;
                settings.MemberFunctionsColor = Color.Red;
                settings.LocalVariablesColor = Color.Red;
                changed = true;
            }
            else settings = (Settings)ObjectSerializer.Deserialize(settingFilename, settings);
            if (settings.MemberFunctionsColor == Color.Empty)
            {
                settings.MemberFunctionsColor = Color.Red;
                changed = true;
            }
            if (settings.LocalVariablesColor == Color.Empty)
            {
                settings.LocalVariablesColor = Color.Red;
                changed = true;
            }
            if (changed) SaveSettings();
		}

		/// <summary>
		/// Saves the plugin settings
		/// </summary>
		private void SaveSettings()
		{
			ObjectSerializer.Serialize(settingFilename, settings);
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
            if (settings.HighlightUnderCursorUpdateInteval < HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL)
                settings.HighlightUnderCursorUpdateInteval = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL;
            highlightUnderCursorTimer.Interval = settings.HighlightUnderCursorUpdateInteval;
            if (!settings.HighlightUnderCursorEnabled) highlightUnderCursorTimer.Stop();
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
        private void AddHighlights(ScintillaControl sci, List<SearchMatch> matches)
        {
            if (matches == null) return;
            int highlightStyle = (int)settings.HighlightStyle;
            int highlightColor = DataConverter.ColorToInt32(settings.HighlightColor);
            if (settings.HighlightUnderCursorEnabled && prevResult != null)
            {
                if (prevResult.Member != null)
                {
                    FlagType flags = prevResult.Member.Flags;
                    if ((flags & FlagType.ParameterVar) > 0) highlightColor = DataConverter.ColorToInt32(settings.MemberFunctionsColor);
                    else if ((flags & FlagType.LocalVar) > 0) highlightColor = DataConverter.ColorToInt32(settings.LocalVariablesColor);
                }
            }
            int es = sci.EndStyled;
            int mask = 1 << sci.StyleBits;
            bool addLineMarker = settings.AddLineMarker;
            foreach (SearchMatch match in matches)
            {
                int start = sci.MBSafePosition(match.Index);
                int end = start + sci.MBSafeTextLength(match.Value);
                int line = sci.LineFromPosition(start);
                int position = start;
                sci.SetIndicStyle(0, highlightStyle);
                sci.SetIndicFore(0, highlightColor);
                sci.StartStyling(position, mask);
                sci.SetStyling(end - start, mask);
                sci.StartStyling(es, mask - 1);
                if (addLineMarker)
                {
                    sci.MarkerAdd(line, MARKER_NUMBER);
                    sci.MarkerSetBack(MARKER_NUMBER, highlightColor);
                }
            }
            prevPos = sci.CurrentPos;
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
                if (settings.AddLineMarker) sci.MarkerDeleteAll(MARKER_NUMBER);
            }
            prevPos = -1;
            prevToken = string.Empty;
            prevResult = null;
        }

        /// <summary>
        /// Gets search results for a sci control
        /// </summary>
        private List<SearchMatch> GetResults(ScintillaControl sci, string text)
        {
            if (string.IsNullOrEmpty(text) || Regex.IsMatch(text, "[^a-zA-Z0-9_$]")) return null;
            FRSearch search = new FRSearch(text);
            search.WholeWord = settings.WholeWords;
            search.NoCase = !settings.MatchCase;
            search.Filter = SearchFilter.None;
            return search.Matches(sci.Text);
        }

        /// <summary>
        /// 
        /// </summary>
        private void onTempoTick(object sender, EventArgs e)
        {
            ITabbedDocument document = PluginBase.MainForm.CurrentDocument;
            ScintillaControl Sci = document.SciControl;
            if (Sci == null) return;
            int currentPos = Sci.CurrentPos;
            if (currentPos == prevPos) return;
            string newToken = Sci.GetWordFromPosition(currentPos);
            if (settings.HighlightUnderCursorEnabled)
            {
                if (prevPos != currentPos) highlightUnderCursorTimer.Stop();
                if (prevResult != null)
                {
                    ASResult result = IsValidFile(document.FileName) ? ASComplete.GetExpressionType(Sci, Sci.WordEndPosition(currentPos, true)) : null;
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
            else if (newToken != prevToken) RemoveHighlights(Sci);
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
        private void UpdateHighlightUnderCursor(ScintillaControl sci)
        {
            string file = PluginBase.MainForm.CurrentDocument.FileName;
            if (!IsValidFile(file)) return;
            int currentPos = sci.CurrentPos;
            string newToken = sci.GetWordFromPosition(currentPos);
            if (!string.IsNullOrEmpty(newToken)) newToken = newToken.Trim();
            if (!string.IsNullOrEmpty(newToken))
            {
                if (prevResult == null && prevToken == newToken) return;
                ASResult result = IsValidFile(file) ? ASComplete.GetExpressionType(sci, sci.WordEndPosition(currentPos, true)) : null;
                if (result != null && !result.IsNull())
                {
                    if (prevResult != null && (result.Member != prevResult.Member || result.Type != prevResult.Type || result.Path != prevResult.Path)) return;
                    RemoveHighlights(sci);
                    prevToken = newToken;
                    prevResult = result;
                    List<SearchMatch> matches = FilterResults(GetResults(sci, prevToken), result, sci);
                    if (matches == null) return;
                    highlightUnderCursorTimer.Stop();
                    AddHighlights(sci, matches);
                }
                else RemoveHighlights(sci);
            }
            else RemoveHighlights(sci);
        }

        /// <summary>
        /// TODO slavara: IMPLEMENT ME
        /// </summary>
        /// <param name="matches"></param>
        /// <param name="exprType"></param>
        /// <param name="sci"></param>
        /// <returns></returns>
        private List<SearchMatch> FilterResults(List<SearchMatch> matches, ASResult exprType, ScintillaControl sci)
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
                exprType = ASComplete.GetExpressionType(sci, sci.WordEndPosition(m.Index, true));
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