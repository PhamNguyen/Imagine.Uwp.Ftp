using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Imagine.Uwp.Fpt
{
    public class FtpClient
    {
        String Username = String.Empty;
        String Password = String.Empty;
        TcpClientSocket FtpCommandSocket = null;
        FtpCommand ftpCommand = FtpCommand.None;
        FtpPassiveOperation ftpPassiveOperation = FtpPassiveOperation.None;
        FtpFileOperationInfo ftpFileInfo = null;
        StreamSocket FtpDataChannel = null;
        List<byte> fileListingData = null;
        String RemoteDirectory = String.Empty;
        Logger logger = null;
        CoreDispatcher UIDispatcher = null;

        public event EventHandler FtpConnected;
        public event EventHandler FtpAuthenticationSucceeded;
        public event EventHandler FtpAuthenticationFailed;
        public event EventHandler<FtpDirectoryChangedEventArgs> FtpDirectoryChangedSucceded;
        public event EventHandler<FtpDirectoryChangedEventArgs> FtpDirectoryChangedFailed;
        public event EventHandler<FtpDisconnectedEventArgs> FtpDisconnected;
        public event EventHandler<FtpPresentWorkingDirectoryEventArgs> FtpPresentWorkingDirectoryReceived;
        public event EventHandler<FtpFileTransferEventArgs> FtpFileUploadSucceeded;
        public event EventHandler<FtpFileTransferFailedEventArgs> FtpFileUploadFailed;
        public event EventHandler<FtpFileTransferEventArgs> FtpFileDownloadSucceeded;
        public event EventHandler<FtpFileTransferFailedEventArgs> FtpFileDownloadFailed;
        public event EventHandler<FtpDirectoryListedEventArgs> FtpDirectoryListed;
        public event EventHandler<FtpFileTransferProgressedEventArgs> FtpFileTransferProgressed;

        public FtpClient(String FtpServerIpAddress, CoreDispatcher UIDispatcher)
            : this(FtpServerIpAddress, "21", UIDispatcher)
        {
        }

        public FtpClient(String FtpServerIpAddress, String PortNumber, CoreDispatcher UIDispatcher)
        {
            this.UIDispatcher = UIDispatcher;
            logger = Logger.GetDefault(UIDispatcher);
            logger.AddLog(String.Format("FTP Server IP Address: {0} with port {1}", FtpServerIpAddress, PortNumber));
            FtpCommandSocket = new TcpClientSocket(FtpServerIpAddress, PortNumber, 512, "FTP Command Channel", UIDispatcher);
            FtpCommandSocket.DataReceived += FtpClientSocket_DataReceived;
            FtpCommandSocket.ErrorOccured += FtpClientSocket_ErrorOccured;
            FtpCommandSocket.SocketClosed += FtpClientSocket_SocketClosed;
            FtpCommandSocket.SocketConnected += FtpCommandSocket_SocketConnected;
            IsConnected = false;
        }

        public Boolean IsConnected
        {
            get;
            private set;
        }

        public Boolean IsBusy
        {
            get;
            private set;
        }

        public async Task ConnectAsync()
        {
            if (!IsConnected)
            {
                logger.AddLog("FTP Command Channel Initailized");
                await FtpCommandSocket.PrepareSocketAsync();
            }
        }

        async void FtpClientSocket_DataReceived(object sender, DataReceivedEventArgs e)
        {
            String Response = System.Text.Encoding.UTF8.GetString(e.GetData(), 0, e.GetData().Length);
            logger.AddLog(String.Format("FTPServer -> {0}", Response));
            switch (ftpPassiveOperation)
            {
                case FtpPassiveOperation.FileDownload:
                    ftpCommand = FtpCommand.None;
                    if (Response.StartsWith("150") || Response.StartsWith("125"))
                    {
                        IsBusy = true;
                        DataReader dataReader = new DataReader(FtpDataChannel.InputStream);
                        dataReader.InputStreamOptions = InputStreamOptions.Partial;
                        while (!(await dataReader.LoadAsync(32768)).Equals(0))
                        {
                            IBuffer databuffer = dataReader.DetachBuffer();
                            RaiseFtpFileTransferProgressedEvent(databuffer.Length, false);
                            await ftpFileInfo.LocalFileStream.WriteAsync(databuffer.ToArray(), 0, Convert.ToInt32(databuffer.Length));
                        }
                        await ftpFileInfo.LocalFileStream.FlushAsync();
                        dataReader.Dispose();
                        dataReader = null;
                        //FtpDataChannel.Dispose();
                        //FtpDataChannel = null;
                        RaiseFtpFileDownloadSucceededEvent(ftpFileInfo.LocalFileStream, ftpFileInfo.RemoteFile);
                    }
                    else if (Response.StartsWith("226"))
                    {
                        IsBusy = false;
                        ftpPassiveOperation = FtpPassiveOperation.None;
                    }
                    else
                    {
                        IsBusy = false;
                        ftpPassiveOperation = FtpPassiveOperation.None;
                        RaiseFtpFileDownloadFailedEvent(ftpFileInfo.LocalFileStream, ftpFileInfo.RemoteFile, FtpFileTransferFailureReason.FileDoesNotExist);
                    }
                    break;

                case FtpPassiveOperation.FileUpload:
                    ftpCommand = FtpCommand.None;
                    if (Response.StartsWith("150") || Response.StartsWith("125"))
                    {
                        IsBusy = true;
                        DataWriter dataWriter = new DataWriter(FtpDataChannel.OutputStream);
                        byte[] data = new byte[32768];
                        while (!(await ftpFileInfo.LocalFileStream.ReadAsync(data, 0, data.Length)).Equals(0))
                        {
                            dataWriter.WriteBytes(data);
                            await dataWriter.StoreAsync();
                            RaiseFtpFileTransferProgressedEvent(Convert.ToUInt32(data.Length), true);
                        }
                        await dataWriter.FlushAsync();
                        dataWriter.Dispose();
                        dataWriter = null;
                        FtpDataChannel.Dispose();
                        FtpDataChannel = null;
                    }
                    else if (Response.StartsWith("226"))
                    {
                        IsBusy = false;
                        ftpPassiveOperation = FtpPassiveOperation.None;
                        RaiseFtpFileUploadSucceededEvent(ftpFileInfo.LocalFileStream, ftpFileInfo.RemoteFile);
                        ftpFileInfo = null;
                    }
                    else
                    {
                        IsBusy = false;
                        ftpPassiveOperation = FtpPassiveOperation.None;
                        RaiseFtpFileUploadFailedEvent(ftpFileInfo.LocalFileStream, ftpFileInfo.RemoteFile, FtpFileTransferFailureReason.FileDoesNotExist);
                        ftpFileInfo = null;
                    }
                    break;

                case FtpPassiveOperation.ListDirectory:
                    ftpCommand = FtpCommand.None;
                    if (Response.StartsWith("150") || Response.StartsWith("125"))
                    {
                        IsBusy = true;
                        DataReader dataReader = new DataReader(FtpDataChannel.InputStream);
                        dataReader.InputStreamOptions = InputStreamOptions.Partial;
                        fileListingData = new List<byte>();
                        while (!(await dataReader.LoadAsync(1024)).Equals(0))
                        {
                            fileListingData.AddRange(dataReader.DetachBuffer().ToArray());
                        }
                        dataReader.Dispose();
                        dataReader = null;
                        FtpDataChannel.Dispose();
                        FtpDataChannel = null;
                        String listingData = System.Text.Encoding.UTF8.GetString(fileListingData.ToArray(), 0, fileListingData.ToArray().Length);
                        String[] listings = listingData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        List<String> Filenames = new List<String>();
                        List<String> Directories = new List<String>();
                        foreach (String listing in listings)
                        {
                            if (listing.StartsWith("drwx") || listing.Contains("<DIR>"))
                            {
                                Directories.Add(listing.Split(new char[] { ' ' }).Last());
                            }
                            else
                            {
                                Filenames.Add(listing.Split(new char[] { ' ' }).Last());
                            }
                        }
                        RaiseFtpDirectoryListedEvent(Directories.ToArray(), Filenames.ToArray());
                    }
                    else if (Response.StartsWith("226"))
                    {
                        IsBusy = false;
                        ftpFileInfo = null;
                        ftpPassiveOperation = FtpPassiveOperation.None;
                    }
                    break;

                case FtpPassiveOperation.None:
                    switch (ftpCommand)
                    {
                        case FtpCommand.Username:
                            if (Response.StartsWith("501"))
                            {
                                IsBusy = false;
                                RaiseFtpAuthenticationFailedEvent();
                                break;
                            }
                            this.ftpCommand = FtpCommand.Password;
                            logger.AddLog(String.Format("FTPClient -> PASS {0}\r\n", this.Password));
                            await FtpCommandSocket.SendDataAsync(String.Format("PASS {0}\r\n", this.Password));
                            break;

                        case FtpCommand.Password:
                            this.ftpCommand = FtpCommand.None;
                            IsBusy = false;
                            if (Response.Contains("530"))
                            {
                                RaiseFtpAuthenticationFailedEvent();
                            }
                            else
                            {
                                RaiseFtpAuthenticationSucceededEvent();
                            }
                            break;

                        case FtpCommand.ChangeWorkingDirectory:
                            IsBusy = false;
                            if (Response.StartsWith("550"))
                            {
                                RaiseFtpDirectoryChangedFailedEvent(this.RemoteDirectory);
                            }
                            else
                            {
                                RaiseFtpDirectoryChangedSuccededEvent(this.RemoteDirectory);
                            }
                            break;

                        case FtpCommand.PresentWorkingDirectory:
                            if (Response.StartsWith("257"))
                            {
                                IsBusy = false;
                                RaiseFtpPresentWorkingDirectoryReceivedEvent(Response.Split(new Char[] { ' ', '"' }, StringSplitOptions.RemoveEmptyEntries)[1]);
                            }
                            break;

                        case FtpCommand.Type:
                            ftpCommand = FtpCommand.Passive;
                            logger.AddLog("FTPClient -> PASV\r\n");
                            await FtpCommandSocket.SendDataAsync("PASV\r\n");
                            break;

                        case FtpCommand.Passive:
                            if (Response.StartsWith("227"))
                            {
                                await PrepareDataChannelAsync(Response);
                                if (ftpFileInfo != null)
                                {
                                    if (ftpFileInfo.IsUpload)
                                    {
                                        ftpPassiveOperation = FtpPassiveOperation.FileUpload;
                                        logger.AddLog(String.Format("FTPClient -> STOR {0}\r\n", ftpFileInfo.RemoteFile));
                                        await FtpCommandSocket.SendDataAsync(String.Format("STOR {0}\r\n", ftpFileInfo.RemoteFile));
                                    }
                                    else
                                    {
                                        ftpPassiveOperation = FtpPassiveOperation.FileDownload;
                                        logger.AddLog(String.Format("FTPClient -> RETR {0}\r\n", ftpFileInfo.RemoteFile));
                                        await FtpCommandSocket.SendDataAsync(String.Format("RETR {0}\r\n", ftpFileInfo.RemoteFile));
                                    }
                                }
                                else
                                {
                                    fileListingData = new List<byte>();
                                    ftpPassiveOperation = FtpPassiveOperation.ListDirectory;
                                    logger.AddLog("FTPClient -> LIST\r\n");
                                    await FtpCommandSocket.SendDataAsync("LIST\r\n");
                                }
                            }
                            break;

                        case FtpCommand.Logout:
                            ftpCommand = FtpCommand.None;
                            break;

                        case FtpCommand.None:
                            break;
                    }
                    break;
            }

        }

        void FtpCommandSocket_SocketConnected(object sender, EventArgs e)
        {
            IsConnected = true;
            RaiseFtpConnectedEvent();
        }

        void FtpClientSocket_ErrorOccured(object sender, ErrorOccuredEventArgs e)
        {
            logger.AddLog(e.ExceptionObject.Message);
            IsConnected = false;
            RaiseFtpDisconnectedEvent(FtpDisconnectReason.SocketError);
        }

        void FtpClientSocket_SocketClosed(object sender, SocketClosedEventArgs e)
        {
            IsConnected = false;
            if (!ftpCommand.Equals(FtpCommand.Logout))
            {
                RaiseFtpDisconnectedEvent(FtpDisconnectReason.SocketClosed);
            }
            else
            {
                RaiseFtpDisconnectedEvent(FtpDisconnectReason.QuitCommand);
            }
        }

        public async Task AuthenticateAsync()
        {
            await AuthenticateAsync("anonymous", "");	//TODO: test value only
        }

        public async Task AuthenticateAsync(String Username, String Password)
        {
            ftpCommand = FtpCommand.Username;
            this.Username = Username;
            this.Password = Password;
            logger.AddLog(String.Format("FTPClient -> USER {0}\r\n", Username));
            await FtpCommandSocket.SendDataAsync(String.Format("USER {0}\r\n", Username));
        }

        public async Task ChangeWorkingDirectoryAsync(String RemoteDirectory)
        {
            if (!IsBusy)
            {
                this.RemoteDirectory = RemoteDirectory;
                ftpCommand = FtpCommand.ChangeWorkingDirectory;
                logger.AddLog(String.Format("FTPClient -> CWD {0}\r\n", RemoteDirectory));
                await FtpCommandSocket.SendDataAsync(String.Format("CWD {0}\r\n", RemoteDirectory));
            }
        }

        public async Task GetPresentWorkingDirectoryAsync()
        {
            if (!IsBusy)
            {
                ftpCommand = FtpCommand.PresentWorkingDirectory;
                logger.AddLog("FTPClient -> PWD\r\n");
                await FtpCommandSocket.SendDataAsync("PWD\r\n");
            }
        }

        public async Task GetDirectoryListingAsync()
        {
            if (!IsBusy)
            {
                fileListingData = null;
                ftpFileInfo = null;
                IsBusy = true;
                ftpCommand = FtpCommand.Passive;
                logger.AddLog("FTPClient -> PASV\r\n");
                await FtpCommandSocket.SendDataAsync("PASV\r\n");
            }
        }

        public async Task UploadFileAsync(System.IO.Stream LocalFileStream, String RemoteFilename)
        {
            if (!IsBusy)
            {
                ftpFileInfo = null;
                IsBusy = true;
                ftpFileInfo = new FtpFileOperationInfo(LocalFileStream, RemoteFilename, true);
                ftpCommand = FtpCommand.Type;
                logger.AddLog("FTPClient -> TYPE I\r\n");
                await FtpCommandSocket.SendDataAsync("TYPE I\r\n");
            }
        }

        public async Task DownloadFileAsync(System.IO.Stream LocalFileStream, String RemoteFilename)
        {
            if (!IsBusy)
            {
                ftpFileInfo = null;
                IsBusy = true;
                ftpFileInfo = new FtpFileOperationInfo(LocalFileStream, RemoteFilename, false);
                ftpCommand = FtpCommand.Type;
                logger.AddLog("FTPClient -> TYPE I\r\n");
                await FtpCommandSocket.SendDataAsync("TYPE I\r\n");
            }
        }

        public async Task DisconnectAsync()
        {
            ftpCommand = FtpCommand.Logout;
            logger.AddLog("FTPClient -> QUIT\r\n");
            await FtpCommandSocket.SendDataAsync("QUIT\r\n");
        }

        private async Task PrepareDataChannelAsync(String ChannelInfo)
        {
            ChannelInfo = ChannelInfo.Remove(0, "227 Entering Passive Mode".Length);
            int start = ChannelInfo.IndexOf("(") + 1;
            int length = ChannelInfo.IndexOf(")") - start;
            ChannelInfo = ChannelInfo.Substring(start, length);

            String[] Splits = ChannelInfo.Split(new char[] { ',', ' ', }, StringSplitOptions.RemoveEmptyEntries);
            String Ipaddr = String.Join(".", Splits, 0, 4);

            //Configure the IP Address
            //Calculate the Data Port
            Int32 port = Convert.ToInt32(Splits[4]);
            port = (port * 256) + Convert.ToInt32(Splits[5]);
            logger.AddLog(String.Format("FTP Data Channel IPAddress: {0}, Port: {1}", Ipaddr, port));
            FtpDataChannel = new StreamSocket();
            await FtpDataChannel.ConnectAsync(new Windows.Networking.HostName(Ipaddr), port.ToString());
            logger.AddLog("FTP Data Channel connected");
        }

        private void RaiseFtpAuthenticationSucceededEvent()
        {
            if (FtpAuthenticationSucceeded != null)
            {
                FtpAuthenticationSucceeded(this, EventArgs.Empty);
            }
        }

        private void RaiseFtpAuthenticationFailedEvent()
        {
            if (FtpAuthenticationFailed != null)
            {
                FtpAuthenticationFailed(this, EventArgs.Empty);
            }
        }

        private void RaiseFtpDirectoryChangedSuccededEvent(String RemoteDirectory)
        {
            if (FtpDirectoryChangedSucceded != null)
            {
                FtpDirectoryChangedSucceded(this, new FtpDirectoryChangedEventArgs(RemoteDirectory));
            }
        }

        private void RaiseFtpDirectoryChangedFailedEvent(String RemoteDirectory)
        {
            if (FtpDirectoryChangedFailed != null)
            {
                FtpDirectoryChangedFailed(this, new FtpDirectoryChangedEventArgs(RemoteDirectory));
            }
        }

        private void RaiseFtpPresentWorkingDirectoryReceivedEvent(String PresentWorkingDirectory)
        {
            if (FtpPresentWorkingDirectoryReceived != null)
            {
                FtpPresentWorkingDirectoryReceived(this, new FtpPresentWorkingDirectoryEventArgs(PresentWorkingDirectory));
            }
        }

        private void RaiseFtpFileUploadSucceededEvent(Stream LocalFileStream, String RemoteFile)
        {
            if (FtpFileUploadSucceeded != null)
            {
                FtpFileUploadSucceeded(this, new FtpFileTransferEventArgs(LocalFileStream, RemoteFile));
            }
        }

        private void RaiseFtpFileUploadFailedEvent(Stream LocalFileStream, String RemoteFile, FtpFileTransferFailureReason FileTransferFailReason)
        {
            if (FtpFileUploadFailed != null)
            {
                FtpFileUploadFailed(this, new FtpFileTransferFailedEventArgs(LocalFileStream, RemoteFile, FileTransferFailReason));
            }
        }

        private void RaiseFtpFileDownloadSucceededEvent(Stream LocalFileStream, String RemoteFile)
        {
            if (FtpFileDownloadSucceeded != null)
            {
                FtpFileDownloadSucceeded(this, new FtpFileTransferEventArgs(LocalFileStream, RemoteFile));
            }
        }

        private void RaiseFtpFileDownloadFailedEvent(Stream LocalFileStream, String RemoteFile, FtpFileTransferFailureReason FileTransferFailReason)
        {
            if (FtpFileDownloadFailed != null)
            {
                FtpFileDownloadFailed(this, new FtpFileTransferFailedEventArgs(LocalFileStream, RemoteFile, FileTransferFailReason));
            }
        }

        private void RaiseFtpConnectedEvent()
        {
            if (FtpConnected != null)
            {
                FtpConnected(this, EventArgs.Empty);
            }
        }

        private void RaiseFtpDisconnectedEvent(FtpDisconnectReason DisconnectReason)
        {
            if (FtpDisconnected != null)
            {
                FtpDisconnected(this, new FtpDisconnectedEventArgs(DisconnectReason));
            }
        }

        private void RaiseFtpDirectoryListedEvent(String[] Directories, String[] Filenames)
        {
            if (FtpDirectoryListed != null)
            {
                FtpDirectoryListed(this, new FtpDirectoryListedEventArgs(Directories, Filenames));
            }
        }

        private void RaiseFtpFileTransferProgressedEvent(UInt32 BytesTransfered, Boolean IsUpload)
        {
            if (FtpFileTransferProgressed != null)
            {
                FtpFileTransferProgressed(this, new FtpFileTransferProgressedEventArgs(BytesTransfered, IsUpload));
            }
        }
    }
}