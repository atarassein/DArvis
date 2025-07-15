﻿using System;
using System.Text;

using DArvis.Common;
using DArvis.IO.Process;
using DArvis.Win32;

namespace DArvis.Models
{
    public sealed class ClientProcess : UpdatableObject
    {
        private const int ViewportWidth = 640;
        private const int ViewportHeight = 480;

        private int processId;
        private nint windowHandle;
        private string windowClassName = string.Empty;
        private string windowTitle = string.Empty;
        private int windowWidth = ViewportWidth;
        private int windowHeight = ViewportHeight;
        private DateTime creationTime;

        public int ProcessId
        {
            get => processId;
            set => SetProperty(ref processId, value);
        }

        public nint WindowHandle
        {
            get => windowHandle;
            set => SetProperty(ref windowHandle, value);
        }

        public string WindowClassName
        {
            get => windowClassName;
            set => SetProperty(ref windowClassName, value);
        }

        public string WindowTitle
        {
            get => windowTitle;
            set => SetProperty(ref windowTitle, value);
        }

        public int WindowWidth
        {
            get => windowWidth;
            set => SetProperty(ref windowWidth, value, onChanged: (p) => { RaisePropertyChanged(nameof(WindowScaleX)); });
        }

        public int WindowHeight
        {
            get => windowHeight;
            set => SetProperty(ref windowHeight, value, onChanged: (p) => { RaisePropertyChanged(nameof(WindowScaleY)); });
        }

        public double WindowScaleX => WindowWidth / 640.0;

        public double WindowScaleY => WindowHeight / 480.0;

        public DateTime CreationTime
        {
            get => creationTime;
            set => SetProperty(ref creationTime, value);
        }

        public ClientProcess() { }

        protected override void OnUpdate()
        {
            var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
            var windowTextBuffer = new StringBuilder(windowTextLength + 1);
            windowTextLength = NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);

            WindowTitle = windowTextBuffer.ToString(0, windowTextLength);

            if (NativeMethods.GetClientRect(windowHandle, out var clientRect))
            {
                WindowWidth = clientRect.Width;
                WindowHeight = clientRect.Height;
            }

            if (CreationTime == DateTime.MinValue)
                UpdateProcessTime();
        }

        private void UpdateProcessTime()
        {
            using var accessor = new ProcessMemoryAccessor(processId, ProcessAccess.Read);

            if (NativeMethods.GetProcessTimes(accessor.ProcessHandle, out var creationTime, out _, out _, out _))
                CreationTime = creationTime.FiletimeToDateTime();
        }
    }
}
