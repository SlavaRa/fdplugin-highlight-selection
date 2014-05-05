using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text;

namespace HighlightSelection
{
	[Serializable]
	public class Settings
	{
		[DisplayName("Highlight Color")]
		[Description("The color to highlight the selected text.")]
		[DefaultValue(typeof(Color), "Red")]
		public Color HighlightColor {get; set;}

		[DisplayName("Highlight Whole Words")]
		[Description("Only highlights whole words.")]
		[DefaultValue(true)]
		public bool WholeWords {get; set;}

		[DisplayName("Match Case")]
		[Description("Only highlights text with the same case.")]
		[DefaultValue(true)]
		public bool MatchCase {get; set;}

		[DisplayName("Add Line Marker")]
		[Description("Adds a line marker next to every highlight.")]
		[DefaultValue(true)]
		public bool AddLineMarker {get; set;}
	}
}