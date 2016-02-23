using System;

namespace FtpService
{
    public class FtpFileTransferProgressedEventArgs : EventArgs
    {
        public UInt32 BytesTransferred
        {
            get;
            private set;
        }

        public Boolean IsUpload
        {
            get;
            private set;
        }

        internal FtpFileTransferProgressedEventArgs(UInt32 BytesTransferred, Boolean IsUpload)
        {
            this.BytesTransferred = BytesTransferred;
            this.IsUpload = IsUpload;
        }
    }
}