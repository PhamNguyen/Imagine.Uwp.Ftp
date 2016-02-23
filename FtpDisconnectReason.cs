namespace FtpService
{
	public enum FtpDisconnectReason : byte
	{
		None = 0,
		QuitCommand = 1,
		SocketClosed = 2,
		SocketError = 3
	}
}