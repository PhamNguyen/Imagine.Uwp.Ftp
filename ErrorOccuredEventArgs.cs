using System;

namespace FtpService
{
	public class ErrorOccuredEventArgs : EventArgs
	{
		public Exception ExceptionObject
		{
			get;
			private set;
		}

		internal ErrorOccuredEventArgs(Exception ExceptionObject)
		{
			this.ExceptionObject = ExceptionObject;
		}
	}
}