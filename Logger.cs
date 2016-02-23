using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace FtpService
{
    public sealed class Logger
    {
        private static Logger s_Logger = null;
        public static Logger GetDefault(CoreDispatcher UIDispatcher)
        {
            if (s_Logger == null)
            {
                s_Logger = new Logger(UIDispatcher);
            }
            return s_Logger;
        }

        private CoreDispatcher UIDispatcher = null;

        private Logger(CoreDispatcher UIDispatcher)
        {
            Logs = new ObservableCollection<String>();
            this.UIDispatcher = UIDispatcher != null ? UIDispatcher : CoreApplication.MainView.CoreWindow.Dispatcher;
        }

        public ObservableCollection<String> Logs
        {
            get;
            private set;
        }

        public async void AddLog(String LogInfo)
        {
            await UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                System.Diagnostics.Debug.WriteLine(LogInfo);
                Logs.Add(LogInfo);
            });
        }
    }
}