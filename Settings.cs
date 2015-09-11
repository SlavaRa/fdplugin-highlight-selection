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

	    Color highlightColor = Color.Red;

	    [Category("General")]
	    [DisplayName("Highlight Color")]
	    [Description("The color to highlight the selected text.")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color HighlightColor
	    {
	        get { return highlightColor; }
	        set { highlightColor = value; }
	    }

	    bool wholeWords = DEFAULT_WHOLE_WORD;

	    [Category("General")]
	    [DisplayName("Highlight Whole Words")]
	    [Description("Only highlights whole words.")]
	    [DefaultValue(DEFAULT_WHOLE_WORD)]
	    public bool WholeWords
	    {
	        get { return wholeWords; }
	        set { wholeWords = value; }
	    }

	    bool matchCase = DEFAULT_MATCH_CASE;

	    [Category("General")]
	    [DisplayName("Match Case")]
	    [Description("Only highlights text with the same case.")]
	    [DefaultValue(DEFAULT_MATCH_CASE)]
	    public bool MatchCase
	    {
	        get { return matchCase; }
	        set { matchCase = value; }
	    }

	    bool addLineMarker = DEFAULT_ADD_LINE_MARKER;

	    [Category("General")]
	    [DisplayName("Add Line Marker")]
	    [Description("Adds a line marker next to every highlight.")]
	    [DefaultValue(DEFAULT_ADD_LINE_MARKER)]
	    public bool AddLineMarker
	    {
	        get { return addLineMarker; }
	        set { addLineMarker = value; }
	    }

	    IndicatorStyle highlightStyle = IndicatorStyle.Box;

	    [Category("General")]
	    [DisplayName("Highlight style")]
	    [DefaultValue(typeof (IndicatorStyle), "Box")]
	    public IndicatorStyle HighlightStyle
	    {
	        get { return highlightStyle; }
	        set { highlightStyle = value; }
	    }

	    bool enableAnnotationBar = true;

	    [Category("General")]
	    [DisplayName("Enable Annotations Bar")]
	    [DefaultValue(true)]
	    public bool EnableAnnotationBar
	    {
	        get { return enableAnnotationBar; }
	        set { enableAnnotationBar = value; }
	    }

	    bool highlightUnderCursorEnabled = DEFAULT_HIGHLIGHT_UNDER_CURSOR;

	    [Category("Highlight references to symbol under cursor")]
	    [DisplayName("Enabled")]
	    [DefaultValue(DEFAULT_HIGHLIGHT_UNDER_CURSOR)]
	    public bool HighlightUnderCursorEnabled
	    {
	        get { return highlightUnderCursorEnabled; }
	        set { highlightUnderCursorEnabled = value; }
	    }

	    int highlightUnderCursorUpdateInterval = DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL;

	    [Category("Highlight references to symbol under cursor")]
	    [DisplayName("Update interval")]
	    [DefaultValue(DEFAULT_HIGHLIGHT_UNDER_CURSOR_UPDATE_INTERVAL)]
	    public int HighlightUnderCursorUpdateInterval
	    {
	        get { return highlightUnderCursorUpdateInterval; }
	        set { highlightUnderCursorUpdateInterval = value; }
	    }

	    Color packageColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Package")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color PackageColor
	    {
	        get { return packageColor; }
	        set { packageColor = value; }
	    }

	    Color classColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Class")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color ClassColor
	    {
	        get { return classColor; }
	        set { classColor = value; }
	    }

	    Color enumColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Enum")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color EnumColor
	    {
	        get { return enumColor; }
	        set { enumColor = value; }
	    }

	    Color typeDefColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Typedef")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color TypeDefColor
	    {
	        get { return typeDefColor; }
	        set { typeDefColor = value; }
	    }

	    Color abstractColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Abstract")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color AbstractColor
	    {
	        get { return abstractColor; }
	        set { abstractColor = value; }
	    }

	    Color staticConstantColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Static constant")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color StaticConstantColor
	    {
	        get { return staticConstantColor; }
	        set { staticConstantColor = value; }
	    }

	    Color constantColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Constant")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color ConstantColor
	    {
	        get { return constantColor; }
	        set { constantColor = value; }
	    }

	    Color staticAccessorColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Static accessor")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color StaticAccessorColor
	    {
	        get { return staticAccessorColor; }
	        set { staticAccessorColor = value; }
	    }

	    Color staticVariableColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Static variable")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color StaticVariableColor
	    {
	        get { return staticVariableColor; }
	        set { staticVariableColor = value; }
	    }

	    Color accessorColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Accessor")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color AccessorColor
	    {
	        get { return accessorColor; }
	        set { accessorColor = value; }
	    }

	    Color variableColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Variable")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color VariableColor
	    {
	        get { return variableColor; }
	        set { variableColor = value; }
	    }

	    Color memberFunctionColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Member function")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color MemberFunctionColor
	    {
	        get { return memberFunctionColor; }
	        set { memberFunctionColor = value; }
	    }

	    Color localVariableColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Local variable")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color LocalVariableColor
	    {
	        get { return localVariableColor; }
	        set { localVariableColor = value; }
	    }

	    Color staticMethodColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Static method")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color StaticMethodColor
	    {
	        get { return staticMethodColor; }
	        set { staticMethodColor = value; }
	    }

	    Color methodColor = Color.Red;

	    [Category("Highlight references to symbol under cursor. Colors")]
	    [DisplayName("Method")]
	    [DefaultValue(typeof (Color), "Red")]
	    public Color MethodColor
	    {
	        get { return methodColor; }
	        set { methodColor = value; }
	    }
	}
}