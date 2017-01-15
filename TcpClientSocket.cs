using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace Imagine.Uwp.Fpt
{
    public sealed class TcpClientSocket : IDisposable
    {
        String IpAddress = String.Empty;
        String Port = String.Empty;
        StreamSocket streamSocket = null;
        String SocketName = String.Empty;
        DataWriter TcpStreamWriter = null;
        Boolean IsConnectionClosedByRemoteHost = false;
        UInt32 ReadBufferLength = 0;
        Boolean IsSocketConnected = false;
        Logger logger = null;
        CoreDispatcher UIDispatcher = null;
        DataReader TcpStreamReader = null;

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<SocketClosedEventArgs> SocketClosed;
        public event EventHandler<ErrorOccuredEventArgs> ErrorOccured;
        public event EventHandler SocketConnected;
        
        public TcpClientSocket(String IpAddress, String Port, CoreDispatcher UIDispatcher)
            : this(IpAddress, Port, 512, "NoName", UIDispatcher)
        {
        }

        public TcpClientSocket(String IpAddress, String Port, UInt32 ReadBufferLength, CoreDispatcher UIDispatcher)
            : this(IpAddress, Port, ReadBufferLength, "NoName", UIDispatcher)
        {
        }

        public TcpClientSocket(String IpAddress, String Port, String SocketName, CoreDispatcher UIDispatcher)
            : this(IpAddress, Port, 512, SocketName, UIDispatcher)
        {
        }

        public TcpClientSocket(String IpAddress, String Port, UInt32 ReadBufferLength, String SocketName, CoreDispatcher UIDispatcher)
        {
            this.UIDispatcher = UIDispatcher != null ? UIDispatcher : CoreApplication.MainView.CoreWindow.Dispatcher;
            logger = Logger.GetDefault(UIDispatcher);
            this.IpAddress = IpAddress;
            this.Port = Port;
            this.SocketName = SocketName;
            this.ReadBufferLength = ReadBufferLength;
            //logger.AddLog(String.Format("Creating new TCP Socket {0}. IPAddress: {1}, Port: {2}", SocketName, IpAddress, Port));
        }

        public async Task PrepareSocketAsync()
        {
            streamSocket = new StreamSocket();

            //logger.AddLog(String.Format("Connecting {0} socket at IPAddress: {1}, Port: {2}", SocketName, IpAddress, Port));
            try
            {
                await streamSocket.ConnectAsync(new Windows.Networking.HostName(IpAddress), Port);
            }
            catch (Exception ex)
            {
                //logger.AddLog("Unable to connect to remote connection");
                RaiseErrorOccuredEvent(ex);
                return;
            }
            //logger.AddLog("Connected");
            TcpStreamWriter = new DataWriter(streamSocket.OutputStream);
            IsSocketConnected = true;
            try
            {
                await Task.Factory.StartNew(stream =>
                {
                    TcpStreamReader = new DataReader((IInputStream)stream);
                    TcpStreamReader.InputStreamOptions = InputStreamOptions.Partial;
                    try
                    {
                        DataReaderLoadOperation loadOperation = TcpStreamReader.LoadAsync(ReadBufferLength);
                        loadOperation.Completed = new Windows.Foundation.AsyncOperationCompletedHandler<UInt32>(LoadCompleted);
                    }
                    catch (Exception ex)
                    {
                        RaiseErrorOccuredEvent(ex);
                    }
                }, streamSocket.InputStream);
            }
            catch
            {
                //logger.AddLog("Asynchronous Read Operation Canceled");
            }
            RaiseSocketConnectedEvent();
        }

        private void LoadCompleted(Windows.Foundation.IAsyncOperation<uint> asyncInfo, Windows.Foundation.AsyncStatus asyncStatus)
        {
            switch (asyncStatus)
            {
                case Windows.Foundation.AsyncStatus.Canceled:
                    //logger.AddLog("Data load operation canceled");
                    break;

                case Windows.Foundation.AsyncStatus.Completed:
                    //logger.AddLog("Data load operation completed");
                    if (TcpStreamReader.UnconsumedBufferLength.Equals(0))
                    {
                        if (IsSocketConnected)
                        {
                            //logger.AddLog("Connection closed by remote host. Exiting");
                            IsSocketConnected = false;
                            IsConnectionClosedByRemoteHost = true;
                            CloseSocket();
                        }
                    }
                    else
                    {
                        IBuffer buffer = TcpStreamReader.DetachBuffer();
                        RaiseDataReceivedEvent(buffer.ToArray());
                        DataReaderLoadOperation loadOperation = TcpStreamReader.LoadAsync(ReadBufferLength);
                        loadOperation.Completed = new Windows.Foundation.AsyncOperationCompletedHandler<UInt32>(LoadCompleted);
                    }
                    break;

                case Windows.Foundation.AsyncStatus.Error:
                    //logger.AddLog("Exception in data load operation");
                    IsSocketConnected = false;
                    if (asyncInfo.ErrorCode.HResult.Equals(-2147014842))
                    {
                        IsConnectionClosedByRemoteHost = true;
                    }
                    else
                    {
                        RaiseErrorOccuredEvent(asyncInfo.ErrorCode);
                    }
                    CloseSocket();
                    break;

                case Windows.Foundation.AsyncStatus.Started:
                    //logger.AddLog("Data load operation started");
                    break;
            }
        }

        public async Task SendDataAsync(byte[] data)
        {
            if (TcpStreamWriter != null)
            {
                TcpStreamWriter.WriteBytes(data);
                await TcpStreamWriter.StoreAsync();
            }
        }

        public async Task SendDataAsync(String data)
        {
            if (TcpStreamWriter != null)
            {
                TcpStreamWriter.WriteString(data);
                await TcpStreamWriter.StoreAsync();
            }
        }

        [System.Security.SecuritySafeCritical()]
        public void CloseSocket()
        {
            IsSocketConnected = false;
            //logger.AddLog(String.Format("Closing {0} socket", SocketName));
            if (TcpStreamWriter != null)
            {
                //loadOperation.Close();
                TcpStreamWriter.Dispose();
                TcpStreamWriter = null;
            }

            if (streamSocket != null)
            {
                DataReceived = null;
                ErrorOccured = null;
                if (!IsConnectionClosedByRemoteHost)
                {
                    //loadOperation.Cancel();
                    RaiseSocketClosedEvent(SocketCloseReason.ClosedFromLocalHost);
                }
                else
                {
                    RaiseSocketClosedEvent(SocketCloseReason.ClosedByRemoteHost);
                }
                SocketClosed = null;
                streamSocket.Dispose();
                streamSocket = null;
            }
        }

        private void RaiseDataReceivedEvent(Byte[] data)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new DataReceivedEventArgs(data));
            }
        }

        private void RaiseSocketClosedEvent(SocketCloseReason CloseReason)
        {
            if (SocketClosed != null)
            {
                SocketClosed(this, new SocketClosedEventArgs(CloseReason));
            }
        }

        private void RaiseErrorOccuredEvent(Exception ExceptionObject)
        {
            if (ErrorOccured != null)
            {
                ErrorOccured(this, new ErrorOccuredEventArgs(ExceptionObject));
            }
        }

        private void RaiseSocketConnectedEvent()
        {
            if (SocketConnected != null)
            {
                SocketConnected(this, EventArgs.Empty);
            }
        }

        public async void Dispose()
        {
            if (TcpStreamWriter != null)
            {
                await TcpStreamWriter.FlushAsync();
                TcpStreamWriter.Dispose();
            }

            if (streamSocket != null)
            {
                streamSocket.Dispose();
            }
        }
    }
}