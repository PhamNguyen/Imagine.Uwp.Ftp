using System;

namespace FtpService
{
    public class DataReceivedEventArgs : EventArgs
    {
		private Byte[] data = null;

		internal DataReceivedEventArgs(Byte[] data)
		{
			this.data = data;			
		}

		public Byte[] GetData()
		{
			return data;
		}
    }
}