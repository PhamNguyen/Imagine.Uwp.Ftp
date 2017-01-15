namespace Imagine.Uwp.Fpt
{
	public enum  FtpFileTransferFailureReason: byte
	{
		None,
		MemoryCardNotFound,
		FileDoesNotExist,
		InputOutputError
	}
}