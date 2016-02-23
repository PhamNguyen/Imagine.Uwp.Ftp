using System;

namespace FtpService
{
    public class SocketClosedEventArgs : EventArgs
    {
        private SocketCloseReason closeReason = SocketCloseReason.None;

        internal SocketClosedEventArgs(SocketCloseReason closeReason)
            : base()
        {
            this.closeReason = closeReason;
        }

        public SocketCloseReason CloseReason
        {
            get
            {
                return closeReason;
            }
        }
    }
}