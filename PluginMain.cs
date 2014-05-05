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
		private string pluginName = "Highlight Selection";
		private string pluginGuid = "1f387fab-421b-42ac-a985-72a03534f731";
		private string pluginHelp = "";
		private string pluginDesc = "A plugin to highlight your selected text";
		private string pluginAuth = "mike.cann@gmail.com";
		private string settingFilename;
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
			int es = sci.EndStyled;
			int mask = (1 << sci.StyleBits);
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
		private List<SearchMatch> GetResults(ScintillaControl sci, string text)
		{
			if (text != String.Empty && IsAlphaNumeric(text))
			{
				string pattern = text;
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
				int start = sci.MBSafePosition(match.Index);
				int end = start + sci.MBSafeTextLength(match.Value);
				int line = sci.LineFromPosition(start);
				int position = start;
				int es = sci.EndStyled;
				int mask = 1 << sci.StyleBits;

				sci.SetIndicStyle(0, (int)ScintillaNet.Enums.IndicatorStyle.Max);
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
		private Boolean IsAlphaNumeric(string strToCheck)
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
			string dataPath = Path.Combine(PathHelper.DataDir, "HighlightSelection");
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