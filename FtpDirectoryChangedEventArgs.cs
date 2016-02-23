using System;

namespace FtpService
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