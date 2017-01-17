﻿// ******************************************************************
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

namespace Imagine.Uwp.Fpt
{
	public class FtpDirectoryListedEventArgs : EventArgs
	{
		private String[] Filenames = null;
		private String[] Directories = null;

		internal FtpDirectoryListedEventArgs(String[] Directories, String[] Filenames)
		{			
			this.Directories = Directories;
            this.Filenames = Filenames;
		}

		public String[] GetDirectories()
		{
			return this.Directories;
		}

		public String[] GetFilenames()
		{
			return this.Filenames;
		}
	}
}