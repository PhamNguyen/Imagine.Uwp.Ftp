namespace Imagine.Uwp.Fpt
{
	public class FtpDisconnectedEventArgs
	{
		internal FtpDisconnectedEventArgs(FtpDisconnectReason DisconnectReason)
		{
			this.DisconnectReason = DisconnectReason;
		}

		public FtpDisconnectReason DisconnectReason
		{
			get;
			private set;
		}
	}
}