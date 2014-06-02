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
            string dataPath = Path.Combine(PathHelper.DataDir, pluginName);
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
			settings = new Settings();
            if (!File.Exists(settingFilename)) SaveSettings();
            else settings = (Settings)ObjectSerializer.Deserialize(settingFilename, settings);
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
            int style = (int)settings.HighlightStyle;
            int color = DataConverter.ColorToInt32(settings.HighlightColor);
            if (settings.HighlightUnderCursorEnabled && prevResult != null)
            {
                if (prevResult.IsPackage) color = DataConverter.ColorToInt32(settings.PackageColor);
                else
                {
                    FlagType flags;
                    if (prevResult.Type != null && prevResult.Member == null)
                    {
                        flags = prevResult.Type.Flags;
                        if ((flags & FlagType.Abstract) > 0) color = DataConverter.ColorToInt32(settings.AbstractColor);
                        else if ((flags & FlagType.TypeDef) > 0) color = DataConverter.ColorToInt32(settings.TypeDefColor);
                        else if ((flags & FlagType.Enum) > 0) color = DataConverter.ColorToInt32(settings.EnumColor);
                        else if ((flags & FlagType.Class) > 0) color = DataConverter.ColorToInt32(settings.ClassColor);
                    }
                    else if (prevResult.Member != null)
                    {
                        flags = prevResult.Member.Flags;
                        if ((flags & FlagType.Constant) > 0) color = DataConverter.ColorToInt32(settings.ConstantColor);
                        else if ((flags & FlagType.ParameterVar) > 0) color = DataConverter.ColorToInt32(settings.MemberFunctionColor);
                        else if ((flags & FlagType.LocalVar) > 0) color = DataConverter.ColorToInt32(settings.LocalVariableColor);
                        else if ((flags & FlagType.Static) == 0)
                        {
                            if ((flags & FlagType.Variable) > 0) color = DataConverter.ColorToInt32(settings.VariableColor);
                            else if ((flags & (FlagType.Setter | FlagType.Getter)) > 0) color = DataConverter.ColorToInt32(settings.AccessorColor);
                            else if ((flags & FlagType.Function) > 0) color = DataConverter.ColorToInt32(settings.MethodColor);
                        }
                        else
                        {
                            if ((flags & FlagType.Variable) > 0) color = DataConverter.ColorToInt32(settings.StaticVariableColor);
                            else if ((flags & (FlagType.Setter | FlagType.Getter)) > 0) color = DataConverter.ColorToInt32(settings.StaticAccessorColor);
                            else if ((flags & FlagType.Function) > 0) color = DataConverter.ColorToInt32(settings.StaticMethodColor);
                        }
                    }
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
                sci.SetIndicStyle(0, style);
                sci.SetIndicFore(0, color);
                sci.StartStyling(position, mask);
                sci.SetStyling(end - start, mask);
                sci.StartStyling(es, mask - 1);
                if (addLineMarker)
                {
                    sci.MarkerAdd(line, MARKER_NUMBER);
                    sci.MarkerSetBack(MARKER_NUMBER, color);
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
            ScintillaControl sci = document.SciControl;
            if (sci == null) return;
            int currentPos = sci.CurrentPos;
            if (currentPos == prevPos) return;
            string newToken = sci.GetWordFromPosition(currentPos);
            if (settings.HighlightUnderCursorEnabled)
            {
                if (prevPos != currentPos) highlightUnderCursorTimer.Stop();
                if (prevResult != null)
                {
                    ASResult result = IsValidFile(document.FileName) ? ASComplete.GetExpressionType(sci, sci.WordEndPosition(currentPos, true)) : null;
                    if (result == null || result.IsNull() || result.Member != prevResult.Member || result.Type != prevResult.Type || result.Path != prevResult.Path)
                    {
                        RemoveHighlights(sci);
                        highlightUnderCursorTimer.Start();
                    }
                }
                else
                {
                    RemoveHighlights(sci);
                    highlightUnderCursorTimer.Start();
                }
                
            }
            else if (newToken != prevToken) RemoveHighlights(sci);
            prevPos = currentPos;
        }

        /// <summary>
        /// 
        /// </summary>
        private void highlighUnderCursorTimerTick(object sender, EventArgs e)
        {
            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (sci != null) UpdateHighlightUnderCursor(sci);
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
                    if (matches == null || matches.Count == 0) return;
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
            if (matches == null || matches.Count == 0) return null;
            MemberModel contextMember = null;
            int lineFrom = 0;
            int lineTo = sci.LineCount;
            FlagType localVarMask = FlagType.LocalVar | FlagType.ParameterVar;
            bool isLocalVar = false;
            if (exprType.Member != null)
            {
                if ((exprType.Member.Flags & localVarMask) > 0)
                {
                    contextMember = exprType.Context.ContextFunction;
                    lineFrom = contextMember.LineFrom;
                    lineTo = contextMember.LineTo;
                    isLocalVar = true;
                }
            }
            List<SearchMatch> newMatches = new List<SearchMatch>();
            foreach (SearchMatch m in matches)
            {
                if (m.Line < lineFrom || m.Line > lineTo) continue;
                int pos = sci.MBSafePosition(m.Index);
                exprType = ASComplete.GetExpressionType(sci, sci.WordEndPosition(pos, true));
                if (exprType != null)
                {
                    MemberModel member = exprType.Member;
                    if (!isLocalVar)
                    {
                        if ((exprType.Type != null && member == null) || (member != null && (member.Flags & localVarMask) == 0)) newMatches.Add(m);
                    }
                    else if (member != null && (member.Flags & localVarMask) > 0) newMatches.Add(m);
                }
            }
            return newMatches;
        }

        #endregion
    }
}