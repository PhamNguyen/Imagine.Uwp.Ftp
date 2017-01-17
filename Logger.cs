// ******************************************************************
// Copyright (c) 2017 by Nguyen Pham. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************


using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Imagine.Uwp.Fpt
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