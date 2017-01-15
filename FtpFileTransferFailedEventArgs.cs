using System;
using System.IO;

namespace Imagine.Uwp.Fpt
{
	public class FtpFileTransferFailedEventArgs : EventArgs
	{
		internal FtpFileTransferFailedEventArgs(Stream LocalFileStream, String RemoteFile, FtpFileTransferFailureReason FileTransferFailReason)
		{
			this.LocalFileStream = LocalFileStream;
			this.RemoteFile = RemoteFile;
			this.FileTransferFailReason = FileTransferFailReason;
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

		public FtpFileTransferFailureReason FileTransferFailReason
		{
			get;
			private set;
		}
	}
}