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
		public Color highlightColor = Color.Red;
		public Boolean wholeWords = true;
		public Boolean matchCase = true;
		public Boolean addLineMarker = true;

		[DisplayName("Highlight Color")]
		[Description("The color to highlight the selected text.")]
		[DefaultValue(typeof(Color), "Red")]
		public Color HighlightColor
		{
			get { return this.highlightColor; }
			set { this.highlightColor = value; }
		}

		[DisplayName("Highlight Whole Words")]
		[Description("Only highlights whole words.")]
		[DefaultValue(true)]
		public Boolean WholeWords
		{
			get { return this.wholeWords; }
			set { this.wholeWords = value; }
		}

		[DisplayName("Match Case")]
		[Description("Only highlights text with the same case.")]
		[DefaultValue(true)]
		public Boolean MatchCase
		{
			get { return this.matchCase; }
			set { this.matchCase = value; }
		}

		[DisplayName("Add Line Marker")]
		[Description("Adds a line marker next to every highlight.")]
		[DefaultValue(true)]
		public Boolean AddLineMarker
		{
			get { return this.addLineMarker; }
			set { this.addLineMarker = value; }
		}
	}

}
