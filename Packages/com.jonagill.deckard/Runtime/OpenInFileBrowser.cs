﻿namespace Deckard
{

	public static class OpenInFileBrowser
	{
		private static void OpenInMac(string path)
		{
			bool openInsidesOfFolder = false;

			// try mac
			string macPath = path.Replace("\\", "/"); // mac finder doesn't like backward slashes

			if (System.IO.Directory.Exists(macPath)
			) // if path requested is a folder, automatically open insides of that folder
			{
				openInsidesOfFolder = true;
			}

			if (!macPath.StartsWith("\""))
			{
				macPath = "\"" + macPath;
			}

			if (!macPath.EndsWith("\""))
			{
				macPath = macPath + "\"";
			}

			string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;

			try
			{
				System.Diagnostics.Process.Start("open", arguments);
			}
			catch (System.ComponentModel.Win32Exception e)
			{
				// tried to open mac finder in windows
				// just silently skip error
				// we currently have no platform define for the current OS we are in, so we resort to this
				e.HelpLink = ""; // do anything with this variable to silence warning about not using it
			}
		}

		private static void OpenInWin(string path)
		{
			bool openInsidesOfFolder = false;

			// try windows
			string winPath = path.Replace("/", "\\"); // windows explorer doesn't like forward slashes

			if (System.IO.Directory.Exists(winPath)
			) // if path requested is a folder, automatically open insides of that folder
			{
				openInsidesOfFolder = true;
			}

			try
			{
				System.Diagnostics.Process.Start("explorer.exe",
					(openInsidesOfFolder ? "/root," : "/select,") + winPath);
			}
			catch (System.ComponentModel.Win32Exception e)
			{
				// tried to open win explorer in mac
				// just silently skip error
				// we currently have no platform define for the current OS we are in, so we resort to this
				e.HelpLink = ""; // do anything with this variable to silence warning about not using it
			}
		}

		public static void Open(string path)
		{
			#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
				OpenInWin(path);
			#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
				OpenInMac(path);
			#else
				OpenInWin(path);
				OpenInMac(path);
			#endif
		}
	}
}