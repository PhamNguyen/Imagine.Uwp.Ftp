using System;
using System.IO;

namespace Imagine.Uwp.Fpt
{
	public class FtpFileTransferEventArgs : EventArgs
	{
		internal FtpFileTransferEventArgs(Stream LocalFileStream, String RemoteFile)
		{
			this.LocalFileStream = LocalFileStream;
			this.RemoteFile = RemoteFile;
		}

		public Stream LocalFileStream
		{
			get;
			private set;
		}

		public String RemoteFile
		{
			get;
			private set;
		}
	}
}