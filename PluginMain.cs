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

namespace HighlightSelection
{
	public class PluginMain : IPlugin
	{
		private String pluginName = "Highlight Selection";
		private String pluginGuid = "1f387fab-421b-42ac-a985-72a03534f731";
		private String pluginHelp = "";
		private String pluginDesc = "A plugin to highlight your selected text";
		private String pluginAuth = "mike.cann@gmail.com";
		private String settingFilename;
		private Settings settingObject;
		//private DockContent pluginPanel;
		//private Image pluginImage;

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
		public String Name
		{
			get { return pluginName; }
		}

		/// <summary>
		/// GUID of the plugin
		/// </summary>
		public String Guid
		{
			get { return pluginGuid; }
		}

		/// <summary>
		/// Author of the plugin
		/// </summary> 
		public String Author
		{
			get { return pluginAuth; }
		}

		/// <summary>
		/// Description of the plugin
		/// </summary> 
		public String Description
		{
			get { return pluginDesc; }
		}

		/// <summary>
		/// Web address for help
		/// </summary> 
		public String Help
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
		}


		/// <summary>
		/// Disposes the plugin
		/// </summary>
		public void Dispose()
		{
		    SaveSettings();
		}


		/// <summary>
		/// Handles the incoming events
		/// </summary>
		public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority)
		{
			switch (e.Type)
			{
				// Catches FileSwitch event and displays the filename it in the PluginUI.
				case EventType.FileSwitch:

					Console.WriteLine(PluginBase.MainForm.CurrentDocument.IsEditable);

					if (PluginBase.MainForm.CurrentDocument.IsEditable)
					{
						PluginBase.MainForm.CurrentDocument.SciControl.DoubleClick += new ScintillaNet.DoubleClickHandler(SciControl_DoubleClick);
						PluginBase.MainForm.CurrentDocument.SciControl.Modified += new ScintillaNet.ModifiedHandler(SciControl_Modified);
					}
				break;
				case EventType.FileSave:
					RemoveHighlights(PluginBase.MainForm.CurrentDocument.SciControl);
				break;
			}
		}

		/// <summary>
		/// DoubleClick handler
		/// </summary>
		public void SciControl_DoubleClick(ScintillaNet.ScintillaControl sender)
		{
			RemoveHighlights(sender);
			AddHighlights(sender, GetResults(sender, sender.SelText.Trim()));
		}

		/// <summary>
		/// Modified Handler
		/// </summary>
		public void SciControl_Modified(ScintillaNet.ScintillaControl sender, int position, int modificationType, string text, int length, int linesAdded, int line, int intfoldLevelNow, int foldLevelPrev)
		{
			RemoveHighlights(sender);
		}

		/// <summary>
		/// Removes the highlights from the correct sci control
		/// </summary>
		private void RemoveHighlights(ScintillaControl sci)
		{
			Int32 es = sci.EndStyled;
			Int32 mask = (1 << sci.StyleBits);
			sci.StartStyling(0, mask);
			sci.SetStyling(sci.TextLength, 0);
			sci.StartStyling(es, mask - 1);

			if ( settingObject.addLineMarker)
			{
				sci.MarkerDeleteAll(2);
			}
		}

		/// <summary>
		/// Gets search results for a sci control
		/// </summary>
		private List<SearchMatch> GetResults(ScintillaControl sci, String text)
		{
			if (text != String.Empty && IsAlphaNumeric(text))
			{
				String pattern = text;
				FRSearch search = new FRSearch(pattern);
				search.WholeWord =  settingObject.wholeWords;
				search.NoCase = ! settingObject.matchCase;
				search.Filter = SearchFilter.None;
				return search.Matches(sci.Text);
			}

			return null;
		}

		/// <summary>
		/// Adds highlights to the correct sci control
		/// </summary>
		private void AddHighlights(ScintillaControl sci, List<SearchMatch> matches)
		{
			if (matches == null)
			{
				return;
			}

			foreach (SearchMatch match in matches)
			{
				Int32 start = sci.MBSafePosition(match.Index);
				Int32 end = start + sci.MBSafeTextLength(match.Value);
				Int32 line = sci.LineFromPosition(start);
				Int32 position = start;
				Int32 es = sci.EndStyled;
				Int32 mask = 1 << sci.StyleBits;

				sci.SetIndicStyle(0, (Int32)ScintillaNet.Enums.IndicatorStyle.Max);
				sci.SetIndicFore(0, DataConverter.ColorToInt32(settingObject.highlightColor));
				sci.StartStyling(position, mask);
				sci.SetStyling(end - start, mask);
				sci.StartStyling(es, mask - 1);

				if (settingObject.addLineMarker)
				{
					sci.MarkerAdd(line, 2);
					sci.MarkerSetBack(2, DataConverter.ColorToInt32(settingObject.highlightColor));
				}
			}
		}

		/// <summary>
		/// Check string is alphanumeric
		/// </summary>
		private Boolean IsAlphaNumeric(String strToCheck)
		{
			Regex objAlphaNumericPattern = new Regex("[^a-zA-Z0-9_]");

			return !objAlphaNumericPattern.IsMatch(strToCheck);
		}

		#endregion

		#region Custom Methods

		/// <summary>
		/// Initializes important variables
		/// </summary>
		public void InitBasics()
		{
			String dataPath = Path.Combine(PathHelper.DataDir, "HighlightSelection");
			if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
			settingFilename = Path.Combine(dataPath, "Settings.fdb");
			//pluginImage = PluginBase.MainForm.FindImage("100");
		}

		/// <summary>
		/// Adds the required event handlers
		/// </summary> 
		public void AddEventHandlers()
		{
			// Set events you want to listen (combine as flags)
			EventManager.AddEventHandler(this, EventType.FileSwitch);
			EventManager.AddEventHandler(this, EventType.FileSave);
		}

		/// <summary>
		/// Loads the plugin settings
		/// </summary>
		public void LoadSettings()
		{
			settingObject = new Settings();
			if (!File.Exists(settingFilename)) SaveSettings();
			else
			{
				Object obj = ObjectSerializer.Deserialize(settingFilename, settingObject);
				settingObject = (Settings)obj;
			}
		}

		/// <summary>
		/// Saves the plugin settings
		/// </summary>
		public void SaveSettings()
		{
			ObjectSerializer.Serialize(settingFilename, settingObject);
		}

		#endregion
    }
}