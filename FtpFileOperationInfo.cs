using System;
using System.IO;

namespace FtpService
{
	internal class FtpFileOperationInfo
	{
		internal FtpFileOperationInfo(Stream LocalFileStream, String RemoteFile, Boolean IsUpload)
		{
			this.LocalFileStream = LocalFileStream;
			this.RemoteFile = RemoteFile;
			this.IsUpload = IsUpload;
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

		public Boolean IsUpload
		{
			get;
			private set;
		}
	}
}