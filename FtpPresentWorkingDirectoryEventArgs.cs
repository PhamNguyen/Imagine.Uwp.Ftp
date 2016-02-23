using System;

namespace FtpService
{
	public class FtpPresentWorkingDirectoryEventArgs : EventArgs
	{
		public String PresentWorkingDirectory
		{
			get;
			private set;
		}

		internal FtpPresentWorkingDirectoryEventArgs(String PresentWorkingDirectory)
		{
			this.PresentWorkingDirectory = PresentWorkingDirectory;
		}
	}
}