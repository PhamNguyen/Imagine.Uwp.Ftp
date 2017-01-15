using System;

namespace Imagine.Uwp.Fpt
{
	public class FtpDirectoryChangedEventArgs
	{
		internal FtpDirectoryChangedEventArgs(String RemoteDirectory)
		{
			this.RemoteDirectory = RemoteDirectory;
		}

		public String RemoteDirectory
		{
			get;
			private set;
		}
	}
}