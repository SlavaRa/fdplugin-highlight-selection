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
        public const bool HIGHLIGHT_UNDER_CURSOR = true;

        [Category("General")]
        [DisplayName("Highlight Color")]
		[Description("The color to highlight the selected text.")]
		[DefaultValue(typeof(Color), "Red")]
		public Color HighlightColor {get; set;}

        [Category("General")]
		[DisplayName("Highlight Whole Words")]
		[Description("Only highlights whole words.")]
        [DefaultValue(DEFAULT_WHOLE_WORD)]
		public bool WholeWords {get; set;}

        [Category("General")]
		[DisplayName("Match Case")]
		[Description("Only highlights text with the same case.")]
        [DefaultValue(DEFAULT_MATCH_CASE)]
		public bool MatchCase {get; set;}

        [Category("General")]
		[DisplayName("Add Line Marker")]
		[Description("Adds a line marker next to every highlight.")]
        [DefaultValue(DEFAULT_ADD_LINE_MARKER)]
        public bool AddLineMarker {get; set;}

        [Category("General")]
        [DisplayName("Highlight references to symbol under cursor")]
        [DefaultValue(HIGHLIGHT_UNDER_CURSOR)]
        public Boolean HighlightUnderCursor { get; set; }

	}
}