namespace Imagine.Uwp.Fpt
{
	internal enum FtpCommand : byte
	{
		None = 0,
		Username = 1,
		Password = 2,
		ChangeWorkingDirectory = 3,
		PresentWorkingDirectory = 4,
		Type = 5,
		Passive = 6,
		Logout = 7
	}
}