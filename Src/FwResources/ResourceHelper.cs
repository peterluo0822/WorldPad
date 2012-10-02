// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ResourceHelper.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.Resources
{
	#region File Filter Types
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Enumeration of standard file types for which the ResourceHelper can provide a file open/
	/// save Filter specification.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum FileFilterType
	{
		/// <summary>*.*</summary>
		AllFiles,
		/// <summary>*.sf</summary>
		DefaultStandardFormat,
		/// <summary>*.db, *.sf, *.sfm, *.txt</summary>
		AllScriptureStandardFormat,
		/// <summary>*.db, *.sf, *.sfm</summary>
		AllShoeboxScriptureDatabases,
		/// <summary>*.xml</summary>
		XML,
		/// <summary>*.rtf</summary>
		RichTextFormat,
		/// <summary>*.pdf</summary>
		PDF,
		/// <summary>Open XML for Editing Scripture (*.oxes)</summary>
		OXES,
		/// <summary>Open XML for Exchanging Scripture Annotations (*.oxesa)</summary>
		OXESA,
		/// <summary>*.txt</summary>
		Text,
		/// <summary>Open Office Files (*.odt)</summary>
		OpenOffice,
		/// <summary>*.xhtml</summary>
		XHTML,
		/// <summary>*.htm (see also HTML)</summary>
		HTM,
		/// <summary>*.html (see also HTM)</summary>
		HTML,
		/// <summary>*.tec</summary>
		TECkitCompiled,
		/// <summary>*.map</summary>
		TECkitMapping,
		/// <summary>*.map</summary>
		ImportMapping,
		/// <summary>Consistent Changes Table (*.cc, *.cct)</summary>
		AllCCTable,
		/// <summary>*.bmp, *.jpg, *.jpeg, *.gif, *.png, *.tif, *.tiff, *.ico, *.wmf, *.pcx, *.cgm</summary>
		AllImage,
		/// <summary>*.wav, *.snd, *.au, *.aif, *.aifc, *.aiff, *.wma, *.mp3</summary>
		AllAudio,
		/// <summary>*.avi, *.wmv, *.wvx, *.mpeg, *.mpg, *.mpe, *.m1v, *.mp2, *.mpv2, *.mpa</summary>
		AllVideo,
		/// <summary>Lift (*.lift)</summary>
		LIFT,
		/// <summary>*.mdf, *.di, *.dic, *.db, *.sfm, *.sf</summary>
		AllShoeboxDictionaryDatabases,
		/// <summary>*.lng</summary>
		ToolboxLanguageFiles,
		/// <summary>*.lds</summary>
		ParatextLanguageFiles,
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ResourceHelper.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ResourceHelper : Form
	{
		#region Member variables
		private static ResourceHelper s_form = null;
		private static ResourceManager s_stringResources = null;
		private static ResourceManager s_helpResources = null;
		private static Cursor s_horizontalIBeamCursor;
		private static Dictionary<FileFilterType, string> m_fileFilterExtensions;
		#endregion

		#region Construction and destruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for ResourceHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ResourceHelper()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for ResourceHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static ResourceHelper()
		{
			m_fileFilterExtensions = new Dictionary<FileFilterType, string>();
			m_fileFilterExtensions[FileFilterType.AllFiles] = "*.*";
			m_fileFilterExtensions[FileFilterType.DefaultStandardFormat] = "*.sf";
			m_fileFilterExtensions[FileFilterType.AllScriptureStandardFormat] = "*.db; *.sf; *.sfm; *.txt";
			m_fileFilterExtensions[FileFilterType.AllShoeboxScriptureDatabases] = "*.db; *.sf; *.sfm";
			m_fileFilterExtensions[FileFilterType.XML] = "*.xml";
			m_fileFilterExtensions[FileFilterType.RichTextFormat] = "*.rtf";
			m_fileFilterExtensions[FileFilterType.PDF] = "*.pdf";
			m_fileFilterExtensions[FileFilterType.OXES] = "*.oxes";
			m_fileFilterExtensions[FileFilterType.OXESA] = "*.oxesa";
			m_fileFilterExtensions[FileFilterType.Text] = "*.txt";
			m_fileFilterExtensions[FileFilterType.OpenOffice] = "*.odt";
			m_fileFilterExtensions[FileFilterType.XHTML] = "*.xhtml";
			m_fileFilterExtensions[FileFilterType.HTM] = "*.htm";
			m_fileFilterExtensions[FileFilterType.HTML] = "*.html";
			m_fileFilterExtensions[FileFilterType.TECkitCompiled] = "*.tec";
			m_fileFilterExtensions[FileFilterType.TECkitMapping] = "*.map";
			m_fileFilterExtensions[FileFilterType.ImportMapping] = "*.map";
			m_fileFilterExtensions[FileFilterType.AllCCTable] = "*.cc; *.cct";
			m_fileFilterExtensions[FileFilterType.AllImage] = "*.bmp; *.jpg; *.jpeg; *.gif; *.png; *.tif; *.tiff; *.ico; *.wmf; *.pcx; *.cgm";
			m_fileFilterExtensions[FileFilterType.AllAudio] = "*.wav; *.snd; *.au; *.aif; *.aifc; *.aiff; *.wma; *.mp3";
			m_fileFilterExtensions[FileFilterType.AllVideo] = "*.avi; *.wmv; *.wvx; *.mpeg; *.mpg; *.mpe; *.m1v; *.mp2; *.mpv2; *.mpa";
			m_fileFilterExtensions[FileFilterType.LIFT] = "*.lift";
			m_fileFilterExtensions[FileFilterType.AllShoeboxDictionaryDatabases] = "*.mdf; *.di; *.dic; *.db; *.sfm; *.sf";
			m_fileFilterExtensions[FileFilterType.ToolboxLanguageFiles] = "*.lng";
			m_fileFilterExtensions[FileFilterType.ParatextLanguageFiles] = "*.lds";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shut down the one instance of ResourceHelper.
		/// </summary>
		/// <remarks>
		/// This should be called once when the application shuts down.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public static void ShutdownHelper()
		{
			if (s_stringResources != null)
				s_stringResources.ReleaseAllResources();
			s_stringResources = null;
			if (s_helpResources != null)
				s_helpResources.ReleaseAllResources();
			s_helpResources = null;
			if (s_form != null)
				s_form.Dispose();
			s_form = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		#endregion

		private static ResourceHelper Helper
		{
			get
			{
				if (s_form == null)
					s_form = new ResourceHelper();
				return s_form;
			}
		}

		#region Public methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Function to create appropriate labels for Undo tasks, with the action names coming
		/// from the stid.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <param name="stUndo">Returns string for Undo task</param>
		/// <param name="stRedo">Returns string for Redo task</param>
		/// -----------------------------------------------------------------------------------
		public static void MakeUndoRedoLabels(string stid, out string stUndo,
			out string stRedo)
		{
			string stRes = GetResourceString(stid);

			// If we get here from a test, it might not find the correct resource.
			// Just ignore it and set some dummy values
			if (stRes == null || stRes == string.Empty)
			{
				stUndo = "Resource not found for Undo";
				stRedo = "Resource not found for Redo";
				return;
			}
			string[] stStrings = stRes.Split('\n');
			if (stStrings.Length > 1)
			{
				// The resource string contains two separate strings separated by a new-line.
				// The first half is for Undo and the second for Redo.
				stUndo = stStrings[0];
				stRedo = stStrings[1];
			}
			else
			{
				// Insert the string (describing the task) into the undo/redo frames.
				stUndo =
					string.Format(ResourceHelper.GetResourceString("kstidUndoFrame"), stRes);
				stRedo =
					string.Format(ResourceHelper.GetResourceString("kstidRedoFrame"), stRes);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		static public string GetResourceString(string stid)
		{
			if (s_stringResources == null)
			{
				s_stringResources = new System.Resources.ResourceManager(
					"SIL.FieldWorks.Resources.FwStrings", Assembly.GetExecutingAssembly());
			}

			return (stid == null ? "NullStringID" : s_stringResources.GetString(stid));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID, with formatting placeholders replaced by the
		/// supplied parameters.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <param name="parameters">zero or more parameters to format the resource string</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		static public string FormatResourceString(string stid, params object[] parameters)
		{
			return String.Format(GetResourceString(stid), parameters);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a help topic or help file path.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		static public string GetHelpString(string stid)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new System.Resources.ResourceManager(
					"SIL.FieldWorks.Resources.HelpTopicPaths", Assembly.GetExecutingAssembly());
			}

			return (stid == null ? "NullStringID" : s_helpResources.GetString(stid));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the URL to the "Help topic does not exist" topic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string NoHelpTopicURL
		{
			get { return GetHelpString("khtpNoHelpTopic"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the two column selected icon for page layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image TwoColumnSelectedIcon
		{
			get { return Helper.m_imgLst53x43.Images[9]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the two column icon for page layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image TwoColumnIcon
		{
			get { return Helper.m_imgLst53x43.Images[8]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the one column selected icon for page layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image OneColumnSelectedIcon
		{
			get { return Helper.m_imgLst53x43.Images[7]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the one column icon for page layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image OneColumnIcon
		{
			get { return Helper.m_imgLst53x43.Images[6]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the portrait page layout icon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image PortraitIcon
		{
			get { return Helper.m_imgLst53x43.Images[5]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the portrait page layout selected icon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image PortraitSelectedIcon
		{
			get { return Helper.m_imgLst53x43.Images[4]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the landscape page layout icon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image LandscapeIcon
		{
			get { return Helper.m_imgLst53x43.Images[3]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the landscape page layout selected icon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image LandscapeSelectedIcon
		{
			get { return Helper.m_imgLst53x43.Images[2]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book fold page layout icon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BookFoldIcon
		{
			get { return Helper.m_imgLst53x43.Images[1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book fold page layout selected icon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BookFoldSelectedIcon
		{
			get { return Helper.m_imgLst53x43.Images[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon displayed in the styles combo box for paragraph styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image ParaStyleIcon
		{
			get { return Helper.m_imgLst16x16.Images[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon displayed in the styles combo box for the selected paragraph style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image SelectedParaStyleIcon
		{
			get { return Helper.m_imgLst16x16.Images[2]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon displayed in the styles combo box for character styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image CharStyleIcon
		{
			get { return Helper.m_imgLst16x16.Images[1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon displayed in the styles combo box for the selected character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image SelectedCharStyleIcon
		{
			get { return Helper.m_imgLst16x16.Images[3]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon displayed in the styles combo box for data property pseudo-styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image DataPropStyleIcon
		{
			get { return Helper.m_imgLst16x16.Images[4]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon displayed in the styles combo box for data property pseudo-styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image UpArrowIcon
		{
			get {return Helper.m_imgLst16x16.Images[5];}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon displayed on the MoveHere buttons in Discourse Constituent Chart layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image MoveUpArrowIcon
		{
			get { return Helper.m_imgLst16x16.Images[8]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon that looks like ABC with check mark. (Review: this is from the MSVS2005
		/// image library...can we make it part of an OS project like this?)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image SpellingIcon
		{
			get { return Helper.m_imgLst16x16.Images[9]; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icon that looks like two green arrows circling back on each other, typically
		/// used to indicate that something should be refreshed (see e.g. Change spelling dialog)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image RefreshIcon
		{
			get { return Helper.m_imgLst16x16.Images[10]; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the double down arrow icon displayed on "More" buttons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image MoreButtonDoubleArrowIcon
		{
			get { return Helper.m_imgLst11x7.Images[1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the double up arrow icon displayed on "Less" buttons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image LessButtonDoubleArrowIcon
		{
			get { return Helper.m_imgLst11x7.Images[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the down arrow icon used on buttons that display popup menus when clicked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image ButtonMenuArrowIcon
		{
			get { return Helper.m_imgLst11x7.Images[2]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a slightly different version of the down arrow icon used on buttons that
		/// display popup menus when clicked. This one is used in the FwComboBox widget.
		/// To get the right appearance, the arrow needs to be one pixel further left than
		/// in ButtonMenuArrowIcon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image ComboMenuArrowIcon
		{
			get { return Helper.m_imgLst11x7.Images[3]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a pull-down arrow on a yellow background with a black border, used in IText.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image InterlinPopupArrow
		{
			get { return Helper.m_imgLst11x12.Images[0]; }
		}

		/// <summary>
		/// Icon for linking words into phrases.
		/// </summary>
		public static Image InterlinLinkWords
		{
			get { return Helper.m_imgLst16x16.Images["Link16x16.bmp"]; }
		}

		/// <summary>
		/// Icon for breaking phrases into words.
		/// </summary>
		public static Image InterlinBreakPhrase
		{
			get { return Helper.m_imgLst16x16.Images["LinkBreak16x16.bmp"]; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Pull-down arrow used for Some context menus
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BlueCircleDownArrow
		{
			get
			{
				return Helper.m_imgLst11x11.Images[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Same arrow, but with explicit white background suitable for use in views.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BlueCircleDownArrowForView
		{
			get
			{
				return Helper.m_imgLst11x11.Images[3];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Pull-down arrow used for Column configuration
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image ColumnChooser
		{
			get
			{
				return Helper.m_imgLst11x11.Images[1];
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Pull-down arrow used for bulk edit check marks
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image CheckMarkHeader
		{
			get
			{
				return Helper.m_imgLst11x11.Images[2];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image of a box to use for an unexpanded item in a tree.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image PlusBox
		{
			get { return Helper.m_imgLst9x9.Images[1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image of a box to use for an expanded item in a tree.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image MinusBox
		{
			get { return Helper.m_imgLst9x9.Images[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image of a button to bring up the chooser dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image ChooserButton
		{
			get { return Helper.m_imgLst14x13.Images[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image exactly matching a standard checked checkbox. To look right it should
		/// be on a light grey background or have some border around it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image CheckedCheckBox
		{
			get { return Helper.m_imgLst13x13.Images[1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image exactly matching a standard unchecked checkbox. To look right it should
		/// be on a light grey background or have some border around it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image UncheckedCheckBox
		{
			get { return Helper.m_imgLst13x13.Images[2]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image exactly matching a standard unchecked checkbox. To look right it should
		/// be on a light grey background or have some border around it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image DisabledCheckBox
		{
			get { return Helper.m_imgLst13x13.Images[7]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image of a red X (typically used for 'wrong') on a transparent background.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image RedX
		{
			get { return Helper.m_imgLst13x13.Images[3]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image of a red X (typically used to indicate that an image cannot be
		/// displayed) on a transparent background.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image ImageNotFoundX
		{
			get { return Helper.m_imgLst256x226.Images[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image of a green check (typically used for 'righ') on a transparent
		/// background.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image GreenCheck
		{
			get { return Helper.m_imgLst13x13.Images[4]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image of a blue question mark (typically used for 'not sure') on a
		/// transparent background.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BlueQuestionMark
		{
			get { return Helper.m_imgLst13x13.Images[5]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a completely transparent (13 x 13) icon. This may be another way to indicate
		/// unchecked, or unsure, contrasting say with the green check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BlankCheck
		{
			get { return Helper.m_imgLst13x13.Images[6]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the language project file box icon that is used on the general tab of the
		/// project properties dialog. It may also be used other places as well. Image is
		/// 32 x 32.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image LangProjFileBoxIcon
		{
			get { return Helper.m_imgLst32x32.Images[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace all of the transparent pixels in the image with the given color
		/// </summary>
		/// <param name="img"></param>
		/// <param name="replaceColor"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Bitmap ReplaceTransparentColor(Image img, Color replaceColor)
		{
			Bitmap bmp = new Bitmap(img);
			for (int x = 0; x < bmp.Width; x++)
				for (int y = 0; y < bmp.Height; y++)
					if (bmp.GetPixel(x, y).A == 0)
						bmp.SetPixel(x, y, replaceColor);

			return bmp;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HorizontalIbeamCursor (loads it from resources if necessary)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Cursor HorizontalIBeamCursor
		{
			get
			{
				if (s_horizontalIBeamCursor == null)
				{
					try
					{
						// Read cursor from embedded resource
						Assembly assembly = Assembly.GetAssembly(typeof(ResourceHelper));
						System.IO.Stream stream = assembly.GetManifestResourceStream(
							"SIL.FieldWorks.Resources.HORIZONTAL_IBEAM.CUR");
						s_horizontalIBeamCursor = new Cursor(stream);
					}
					catch
					{
						s_horizontalIBeamCursor = Cursors.IBeam;
					}
				}
				return s_horizontalIBeamCursor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a filter specification for multiple file types for a SaveFileDialog or
		/// OpenFileDialog.
		/// </summary>
		/// <param name="types">The types of files to include in the filter, in the order they
		/// should be included. Do not use any of the enumeration values starting with "All"
		/// for a filter intended to be used in a SaveFileDialog.</param>
		/// <returns>A string suitable for setting the Filter property of a SaveFileDialog or
		/// OpenFileDialog</returns>
		/// ------------------------------------------------------------------------------------
		public static string BuildFileFilter(IEnumerable<FileFilterType> types)
		{
			StringBuilder bldr = new StringBuilder();
			foreach (FileFilterType type in types)
				bldr.AppendFormat("{0}|", FileFilter(type));
			bldr.Length--;
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a filter specification for a single file type for SaveFileDialog or
		/// OpenFileDialog.
		/// </summary>
		/// <param name="type">The type of files to include in the filter. Do not use any of the
		/// enumeration values starting with "All" for a filter intended to be used in a
		/// SaveFileDialog.</param>
		/// <returns>A string suitable for setting the Filter property of a SaveFileDialog or
		/// OpenFileDialog</returns>
		/// ------------------------------------------------------------------------------------
		public static string FileFilter(FileFilterType type)
		{
			return String.Format("{0} ({1})|{1}",
				GetResourceString("kstid" + type.ToString()),
				m_fileFilterExtensions[type]);
		}
		#endregion
	}
}