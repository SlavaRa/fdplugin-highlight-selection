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

        [DisplayName("Highlight Color")]
		[Description("The color to highlight the selected text.")]
		[DefaultValue(typeof(Color), "Red")]
		public Color HighlightColor {get; set;}

		[DisplayName("Highlight Whole Words")]
		[Description("Only highlights whole words.")]
        [DefaultValue(DEFAULT_WHOLE_WORD)]
		public bool WholeWords {get; set;}

		[DisplayName("Match Case")]
		[Description("Only highlights text with the same case.")]
        [DefaultValue(DEFAULT_MATCH_CASE)]
		public bool MatchCase {get; set;}

		[DisplayName("Add Line Marker")]
		[Description("Adds a line marker next to every highlight.")]
        [DefaultValue(DEFAULT_ADD_LINE_MARKER)]
        public bool AddLineMarker {get; set;}
	}
}