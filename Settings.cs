using ScintillaNet.Enums;
using System;
using System.ComponentModel;
using System.Drawing;

namespace HighlightSelection
{
	[Serializable]
	public class Settings
	{
        public const bool DEFAULT_WHOLE_WORD = true;
        public const bool DEFAULT_MATCH_CASE = true;
        public const bool DEFAULT_ADD_LINE_MARKER = true;
        public const bool DEFAULT_HIGHLIGHT_UNDER_CURSOR = false;
        public const int DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL = 100;
        public const IndicatorStyle DEFAULT_HIGHLIGHT_STYLE = IndicatorStyle.Box;

        [Category("General")]
        [DisplayName("Highlight Color")]
        [Description("The color to highlight the selected text.")]
        [DefaultValue(typeof(Color), "Red")]
        public Color HighlightColor { get; set; }

        [Category("General")]
        [DisplayName("Highlight Whole Words")]
        [Description("Only highlights whole words.")]
        [DefaultValue(DEFAULT_WHOLE_WORD)]
        public bool WholeWords { get; set; }

        [Category("General")]
        [DisplayName("Match Case")]
        [Description("Only highlights text with the same case.")]
        [DefaultValue(DEFAULT_MATCH_CASE)]
        public bool MatchCase { get; set; }

        [Category("General")]
        [DisplayName("Add Line Marker")]
        [Description("Adds a line marker next to every highlight.")]
        [DefaultValue(DEFAULT_ADD_LINE_MARKER)]
        public bool AddLineMarker { get; set; }

        [Category("General")]
        [DisplayName("Highlight style")]
        [DefaultValue(typeof(IndicatorStyle), "Box")]
        public IndicatorStyle HighlightStyle { get; set; }

        [Category("Highlight references to symbol under cursor")]
        [DisplayName("Enabled")]
        [DefaultValue(DEFAULT_HIGHLIGHT_UNDER_CURSOR)]
        public bool HighlightUnderCursorEnabled { get; set; }

        [Category("Highlight references to symbol under cursor")]
        [DisplayName("Update interval")]
        [DefaultValue(DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL)]
        public int HighlightUnderCursorUpdateInteval { get; set; }

        [Category("Highlight references to symbol under cursor. Colors")]
        [DisplayName("Static accessor")]
        [DefaultValue(typeof(Color), "Red")]
        public Color StaticAccessorColor { get; set; }

        [Category("Highlight references to symbol under cursor. Colors")]
        [DisplayName("Static variable")]
        [DefaultValue(typeof(Color), "Red")]
        public Color StaticVariableColor { get; set; }

        [Category("Highlight references to symbol under cursor. Colors")]
        [DisplayName("Accessor")]
        [DefaultValue(typeof(Color), "Red")]
        public Color AccessorColor { get; set; }

        [Category("Highlight references to symbol under cursor. Colors")]
        [DisplayName("Variable")]
        [DefaultValue(typeof(Color), "Red")]
        public Color VariableColor { get; set; }

        [Category("Highlight references to symbol under cursor. Colors")]
        [DisplayName("Member function")]
        [DefaultValue(typeof(Color), "Red")]
        public Color MemberFunctionColor { get; set; }

        [Category("Highlight references to symbol under cursor. Colors")]
        [DisplayName("Local variable")]
        [DefaultValue(typeof(Color), "Red")]
        public Color LocalVariableColor { get; set; }
	}
}