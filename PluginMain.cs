using ASCompletion.Completion;
using ASCompletion.Model;
using PluginCore;
using PluginCore.FRService;
using PluginCore.Helpers;
using PluginCore.Managers;
using PluginCore.Utilities;
using ScintillaNet;
using ScintillaNet.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PluginCore.Controls;

namespace HighlightSelection
{
	public class PluginMain : IPlugin
	{
	    const int MARKER_NUMBER = 0;
	    string settingFilename;
		Settings settings;
        FlagToColor flagToColor;
        Timer underCursorTempo = new Timer();
        Timer tempo = new Timer();
        int prevPos;
        ASResult prevResult;
        string prevToken;
        readonly Dictionary<ScintillaControl, Dictionary<int, Control>> sciToLineToAnnotationMarker = new Dictionary<ScintillaControl, Dictionary<int, Control>>();

        #region Required Properties

        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public int Api => 1;

	    /// <summary>
		/// Name of the plugin
		/// </summary> 
		public string Name => "Highlight Selection";

	    /// <summary>
		/// GUID of the plugin
		/// </summary>
		public string Guid => "1f387fab-421b-42ac-a985-72a03534f731";

	    /// <summary>
		/// Author of the plugin
		/// </summary> 
		public string Author => "mike.cann@gmail.com, SlavaRa";

	    /// <summary>
		/// Description of the plugin
		/// </summary> 
		public string Description => "A plugin to highlight your selected text";

	    /// <summary>
		/// Web address for help
		/// </summary> 
		public string Help => "";

	    /// <summary>
		/// Object that contains the settings
		/// </summary>
		[Browsable(false)]
		public object Settings => settings;

	    #endregion

		#region Required Methods

		public void Initialize()
		{
	        InitBasics();
			LoadSettings();
            flagToColor = new FlagToColor(settings);
            InitTimers();
			AddEventHandlers();
            UpdateHighlightUnderCursorTimer();
		}

        public void Dispose()
		{
            underCursorTempo.Dispose();
            underCursorTempo = null;
            tempo.Dispose();
            tempo = null;
		    SaveSettings();
		}

		public void HandleEvent(object sender, NotifyEvent e, HandlingPriority priority)
		{
            ITabbedDocument doc = PluginBase.MainForm.CurrentDocument;
		    ScintillaControl sci = doc.SciControl;
		    switch (e.Type)
			{
				case EventType.FileSwitch:
                    RemoveHighlights(doc.SciControl);
			        if (doc.IsEditable)
                    {
                        sci.MarkerDefine(MARKER_NUMBER, MarkerSymbol.Fullrect);
                        sci.DoubleClick += OnSciDoubleClick;
                        sci.Modified += OnSciModified;
                        sci.Resize += OnSciResize;
                        if (!sciToLineToAnnotationMarker.ContainsKey(sci)) sciToLineToAnnotationMarker[sci] = new Dictionary<int, Control>();
                        UpdateAnnotationsBar(sci);
                        tempo.Interval = PluginBase.Settings.DisplayDelay;
                        tempo.Start();
                    }
                    else tempo.Stop();
				    break;
				case EventType.FileSave:
                    RemoveHighlights(sci);
                    break;
                case EventType.SettingChanged:
                    flagToColor = new FlagToColor(settings);
                    UpdateAnnotationsBar(sci);
                    UpdateHighlightUnderCursorTimer();
				    break;
			}
		}

        #endregion

		#region Custom Methods

        void InitBasics()
		{
            string path = Path.Combine(PathHelper.DataDir, "Highlight Selection");
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
			settingFilename = Path.Combine(path, "Settings.fdb");
		}

        void LoadSettings()
		{
			settings = new Settings();
            if (!File.Exists(settingFilename)) SaveSettings();
            else settings = (Settings)ObjectSerializer.Deserialize(settingFilename, settings);
		}

        void InitTimers()
        {
            underCursorTempo.Tick += OnHighlighUnderCursorTimerTick;
            tempo.Interval = PluginBase.Settings.DisplayDelay;
            tempo.Tick += OnTempoTick;
        }

        void AddEventHandlers() => EventManager.AddEventHandler(this, EventType.FileSwitch | EventType.FileSave | EventType.SettingChanged);

	    void SaveSettings() => ObjectSerializer.Serialize(settingFilename, settings);

	    static bool IsValidFile(string file)
        {
            IProject project = PluginBase.CurrentProject;
            if (project == null) return false;
            string ext = Path.GetExtension(file);
            return (ext == ".as" || ext == ".hx" || ext == ".ls") && PluginBase.CurrentProject.DefaultSearchFilter.Contains(ext);
        }

        void UpdateHighlightUnderCursorTimer()
        {
            if (settings.HighlightUnderCursorUpdateInterval < HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL)
                settings.HighlightUnderCursorUpdateInterval = HighlightSelection.Settings.DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL;
            underCursorTempo.Interval = settings.HighlightUnderCursorUpdateInterval;
            if (!settings.HighlightUnderCursorEnabled) underCursorTempo.Stop();
        }

        void AddHighlights(ScintillaControl sci, List<SearchMatch> matches)
        {
            if (matches == null) return;
            Color color = settings.HighlightUnderCursorEnabled && prevResult != null ? GetHighlightColor() : settings.HighlightColor;
            int style = (int)settings.HighlightStyle;
            int mask = 1 << sci.StyleBits;
            int es = sci.EndStyled;
            bool addLineMarker = settings.AddLineMarker;
            int argb = DataConverter.ColorToInt32(color);
            var vScrollBarWidth = GetVScrollBarWidth(sci);
            Control.ControlCollection controls = PluginBase.MainForm.CurrentDocument.SplitContainer.Parent.Controls;
            bool enableAnnotationBar = settings.EnableAnnotationBar;
            int scale = GetVScrollBarHeight(sci) / sci.LineCount;
            Dictionary<int, Control> lineToAnnotationMarker = sciToLineToAnnotationMarker[sci];
            if (enableAnnotationBar)
            {
                foreach (Button control in controls.OfType<Button>())
                    controls.Remove(control);
                lineToAnnotationMarker.Clear();
            }
            foreach (SearchMatch match in matches)
            {
                #region markers
                int start = sci.MBSafePosition(match.Index);
                int end = start + sci.MBSafeTextLength(match.Value);
                sci.SetIndicStyle(0, style);
                sci.SetIndicFore(0, argb);
                sci.StartStyling(start, mask);
                sci.SetStyling(end - start, mask);
                sci.StartStyling(es, mask - 1);
                int line = sci.LineFromPosition(start);
                if (addLineMarker)
                {
                    sci.MarkerAdd(line, MARKER_NUMBER);
                    sci.MarkerSetBack(MARKER_NUMBER, argb);
                }
                #endregion
                if (!enableAnnotationBar || lineToAnnotationMarker.ContainsKey(line)) continue;
                Button item = new Button()
                {
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(vScrollBarWidth, Math.Max(2, scale)),
                    BackColor = color,
                    ForeColor = color,
                    Cursor = Cursors.Hand
                };
                item.FlatAppearance.BorderSize = 0;
                item.FlatAppearance.MouseOverBackColor = color;
                item.FlatAppearance.MouseDownBackColor = color;
                item.FlatAppearance.CheckedBackColor = color;
                item.MouseClick += (s, e) =>
                {
                    sci.Focus();
                    sci.GotoLine(line);
                    sci.SetSel(start, end);
                };
                lineToAnnotationMarker[line] = item;
                controls.Add(item);
                controls.SetChildIndex(item, 0);
            }
            prevPos = sci.CurrentPos;
            UpdateAnnotationsBar(sci);
        }

        Color GetHighlightColor()
        {
            if (settings.HighlightUnderCursorEnabled && prevResult != null && !prevResult.IsNull())
            {
                if (prevResult.IsPackage) return settings.PackageColor;
                FlagType flags = prevResult.Type != null && prevResult.Member == null ? prevResult.Type.Flags : prevResult.Member.Flags;
                Color color = flagToColor.GetColor(flags);
                if (!Color.Empty.Equals(color)) return color;
            }
            return settings.HighlightColor;
        }

        void RemoveHighlights(ScintillaControl sci)
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
            if (sci != null && sciToLineToAnnotationMarker.ContainsKey(sci)) sciToLineToAnnotationMarker[sci].Clear();
            Control.ControlCollection controls = PluginBase.MainForm.CurrentDocument.SplitContainer.Parent.Controls;
            foreach (Button control in controls.OfType<Button>())
                controls.Remove(control);
        }

        List<SearchMatch> GetResults(ScintillaControl sci, string text)
        {
            if (string.IsNullOrEmpty(text) || Regex.IsMatch(text, "[^a-zA-Z0-9_$]")) return null;
            FRSearch search = new FRSearch(text)
            {
                WholeWord = settings.WholeWords,
                NoCase = !settings.MatchCase,
                Filter = SearchFilter.None
            };
            return search.Matches(sci.Text);
        }

        void UpdateHighlightUnderCursor(ScintillaControl sci)
        {
            string file = PluginBase.MainForm.CurrentDocument.FileName;
            if (!IsValidFile(file)) return;
            int currentPos = sci.CurrentPos;
            string newToken = sci.GetWordFromPosition(currentPos);
            if (!string.IsNullOrEmpty(newToken)) newToken = newToken.Trim();
            if (string.IsNullOrEmpty(newToken)) RemoveHighlights(sci);
            else 
            {
                if (prevResult == null && prevToken == newToken) return;
                ASResult result = IsValidFile(file) ? ASComplete.GetExpressionType(sci, sci.WordEndPosition(currentPos, true)) : null;
                if (result == null || result.IsNull()) RemoveHighlights(sci);
                else 
                {
                    if (prevResult != null && (!Equals(result.Member, prevResult.Member) || !Equals(result.Type, prevResult.Type) || result.Path != prevResult.Path)) return;
                    RemoveHighlights(sci);
                    prevToken = newToken;
                    prevResult = result;
                    List<SearchMatch> matches = FilterResults(GetResults(sci, prevToken), result, sci);
                    if (matches == null || matches.Count == 0) return;
                    underCursorTempo.Stop();
                    AddHighlights(sci, matches);
                }
            }
        }

        List<SearchMatch> FilterResults(ICollection<SearchMatch> matches, ASResult exprType, ScintillaControl sci)
        {
            if (matches == null || matches.Count == 0) return null;
            const FlagType localVarMask = FlagType.LocalVar | FlagType.ParameterVar;
            int lineFrom = 0;
            int lineTo = sci.LineCount;
            bool isLocalVar = false;
            if ((exprType.Member?.Flags & localVarMask) > 0)
            {
                var contextMember = exprType.Context.ContextFunction;
                lineFrom = contextMember.LineFrom;
                lineTo = contextMember.LineTo;
                isLocalVar = true;
            }
            List<SearchMatch> newMatches = new List<SearchMatch>();
            foreach (SearchMatch m in matches)
            {
                if (m.Line < lineFrom || m.Line > lineTo) continue;
                int pos = sci.MBSafePosition(m.Index);
                exprType = ASComplete.GetExpressionType(sci, sci.WordEndPosition(pos, true));
                if (exprType == null || (exprType.InClass != null && !Equals(exprType.InClass, prevResult.InClass))) continue;
                MemberModel member = exprType.Member;
                if (!isLocalVar)
                {
                    if ((exprType.Type != null && member == null) || (member != null && (member.Flags & localVarMask) == 0)) newMatches.Add(m);
                }
                else if (member != null && (member.Flags & localVarMask) > 0) newMatches.Add(m);
            }
            return newMatches;
        }

	    void UpdateAnnotationsBar(ScintillaControl sci)
        {
	        Dictionary<int, Control> lineToAnnotationMarker = sciToLineToAnnotationMarker[sci];
	        if (lineToAnnotationMarker.Count == 0) return;
	        bool enableAnnotationBar = settings.EnableAnnotationBar;
	        int lineCount = sci.LineCount;
	        decimal scale = (decimal)GetVScrollBarHeight(sci) / lineCount;
            int itemX = sci.Width - GetVScrollBarWidth(sci);
            for (int i = 0; i < lineCount; i++)
            {
                if (!lineToAnnotationMarker.ContainsKey(i)) continue;
                Control item = lineToAnnotationMarker[i];
                item.Visible = enableAnnotationBar;
                item.Location = new Point(itemX, (int)(i * scale));
            }
        }

	    static int GetVScrollBarWidth(ScintillaControl sci)
	    {
            foreach (Control control in sci.Controls)
            {
                if (control is ScrollBarEx && control.Width < control.Height)
                    return control.Width;
            }
            return 0;
	    }

        static int GetVScrollBarHeight(ScintillaControl sci)
        {
            foreach (Control control in sci.Controls)
            {
                if (control is ScrollBarEx && control.Width < control.Height)
                    return control.Height - control.Width;
            }
            return sci.Height - GetVScrollBarWidth(sci);
        }

        #endregion

        #region Event Handlers

        void OnSciDoubleClick(ScintillaControl sender)
        {
            RemoveHighlights(sender);
            prevResult = null;
            prevToken = sender.SelText.Trim();
            AddHighlights(sender, GetResults(sender, prevToken));
        }

        void OnSciModified(ScintillaControl sender, int position, int modificationType, string text, int length, int linesAdded, int line, int intfoldLevelNow, int foldLevelPrev)
        {
            underCursorTempo.Stop();
            tempo.Stop();
            RemoveHighlights(sender);
            UpdateAnnotationsBar(sender);
            tempo.Interval = PluginBase.Settings.DisplayDelay;
            tempo.Start();
        }

        void OnSciResize(object sender, EventArgs e) => UpdateAnnotationsBar((ScintillaControl)sender);

	    void OnTempoTick(object sender, EventArgs e)
        {
            ITabbedDocument document = PluginBase.MainForm.CurrentDocument;
            ScintillaControl sci = document?.SciControl;
            if (sci == null) return;
            int currentPos = sci.CurrentPos;
            if (currentPos == prevPos) return;
            string newToken = sci.GetWordFromPosition(currentPos);
            if (settings.HighlightUnderCursorEnabled)
            {
                if (prevPos != currentPos) underCursorTempo.Stop();
                if (prevResult != null)
                {
                    ASResult result = IsValidFile(document.FileName) ? ASComplete.GetExpressionType(sci, sci.WordEndPosition(currentPos, true)) : null;
                    if (result == null || result.IsNull() || !Equals(result.Member, prevResult.Member) || !Equals(result.Type, prevResult.Type) || result.Path != prevResult.Path)
                    {
                        RemoveHighlights(sci);
                        underCursorTempo.Start();
                    }
                }
                else
                {
                    RemoveHighlights(sci);
                    underCursorTempo.Start();
                }

            }
            else if (newToken != prevToken) RemoveHighlights(sci);
            prevPos = currentPos;
        }

        void OnHighlighUnderCursorTimerTick(object sender, EventArgs e)
        {
            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (sci != null) UpdateHighlightUnderCursor(sci);
        }

        #endregion
    }

    class FlagToColor
    {
        readonly Settings settings;
        Dictionary<FlagType, Color> flagsToColor;

        public FlagToColor(Settings settings)
        {
            this.settings = settings;
            Initialize();
        }

        void Initialize() => flagsToColor = new Dictionary<FlagType, Color>
        {
            [FlagType.Abstract] = settings.AbstractColor,
            [FlagType.TypeDef] = settings.TypeDefColor,
            [FlagType.Enum] = settings.EnumColor,
            [FlagType.Class] = settings.ClassColor,
            [FlagType.ParameterVar] = settings.MemberFunctionColor,
            [FlagType.LocalVar] = settings.LocalVariableColor,
            [FlagType.Constant] = settings.ConstantColor,
            [FlagType.Variable] = settings.VariableColor,
            [FlagType.Setter] = settings.AccessorColor,
            [FlagType.Getter] = settings.AccessorColor,
            [FlagType.Function] = settings.MethodColor,
            [FlagType.Static & FlagType.Constant] = settings.StaticConstantColor,
            [FlagType.Static & FlagType.Variable] = settings.StaticVariableColor,
            [FlagType.Static & FlagType.Setter] = settings.StaticAccessorColor,
            [FlagType.Static & FlagType.Getter] = settings.StaticAccessorColor,
            [FlagType.Static & FlagType.Function] = settings.StaticMethodColor
        };

        public Color GetColor(FlagType flags)
        {
            if ((flags & FlagType.Abstract) > 0) return flagsToColor[FlagType.Abstract];
            if ((flags & FlagType.TypeDef) > 0) return flagsToColor[FlagType.TypeDef];
            if ((flags & FlagType.Enum) > 0) return flagsToColor[FlagType.Enum];
            if ((flags & FlagType.Class) > 0) return flagsToColor[FlagType.Class];
            if ((flags & FlagType.ParameterVar) > 0) return flagsToColor[FlagType.ParameterVar];
            if ((flags & FlagType.LocalVar) > 0) return flagsToColor[FlagType.LocalVar];
            if ((flags & FlagType.Static) == 0)
            {
                if ((flags & FlagType.Constant) > 0) return flagsToColor[FlagType.Constant];
                if ((flags & FlagType.Variable) > 0) return flagsToColor[FlagType.Variable];
                if ((flags & FlagType.Setter) > 0) return flagsToColor[FlagType.Setter];
                if ((flags & FlagType.Getter) > 0) return flagsToColor[FlagType.Getter];
                if ((flags & FlagType.Function) > 0) return flagsToColor[FlagType.Function];
            }
            else
            {
                if ((flags & FlagType.Constant) > 0) return flagsToColor[FlagType.Static & FlagType.Constant];
                if ((flags & FlagType.Variable) > 0) return flagsToColor[FlagType.Static & FlagType.Variable];
                if ((flags & FlagType.Setter) > 0) return flagsToColor[FlagType.Static & FlagType.Setter];
                if ((flags & FlagType.Getter) > 0) return flagsToColor[FlagType.Static & FlagType.Getter];
                if ((flags & FlagType.Function) > 0) return flagsToColor[FlagType.Static & FlagType.Function];
            }
            return Color.Empty;
        }
    }
}