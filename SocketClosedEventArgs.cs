using System;

namespace Imagine.Uwp.Fpt
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