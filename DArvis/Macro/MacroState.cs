﻿using System;
using System.Threading;
using System.Threading.Tasks;

using DArvis.Common;
using DArvis.Models;
using DArvis.Settings;

namespace DArvis.Macro
{
    public enum MacroStatus
    {
        Idle,
        Running,
        Paused,
        Stopped,
        Error = -1
    }

    public abstract class MacroState : ObservableObject, IDisposable
    {
        private bool isDisposed;
        protected string name;
        protected Player client;
        protected MacroStatus status;
        protected CancellationTokenSource cancelSource;
        protected Task task;
        protected int lastKnownMapNumber;
        protected string lastKnownMapName;
        protected int lastKnownXCoordinate;
        protected int lastKnownYCoordinate;

        protected MacroLocalStorage localStorage = new();

        public event MacroStatusEventHandler StatusChanged;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public Player Client
        {
            get => client;
            set => SetProperty(ref client, value);
        }

        public MacroLocalStorage LocalStorage => localStorage;

        public MacroStatus Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public MacroState(Player client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        ~MacroState() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                CancelTask();
                cancelSource.Dispose();
            }

            isDisposed = true;
        }

        public void Start()
        {
            if (Status == MacroStatus.Running)
                return;

            SaveKnownState();

            if (Status == MacroStatus.Paused)
                ResumeMacro();
            else
                StartMacro();

            Status = MacroStatus.Running;
            OnStatusChanged(Status);
        }

        public void Stop()
        {
            if (Status == MacroStatus.Stopped)
                return;

            StopMacro();

            Status = MacroStatus.Stopped;
            OnStatusChanged(Status);
        }

        public void Pause()
        {
            if (Status == MacroStatus.Paused)
                return;

            PauseMacro();

            Status = MacroStatus.Paused;
            OnStatusChanged(Status);
        }

        protected virtual void StartMacro(object state = null)
        {
            cancelSource = new CancellationTokenSource();
            task = Task.Factory.StartNew((arg) =>
            {
                while (true)
                {
                    try
                    {
                        UpdateClientMacroStatus();

                        if (cancelSource.Token.IsCancellationRequested)
                            break;

                        if (Status == MacroStatus.Running)
                        {
                            CheckKnownState();
                            MacroLoop(arg);
                        }
                        else
                        {
                            Thread.Sleep(16);
                        }
                    }
                    finally
                    {
                        if (!cancelSource.IsCancellationRequested)
                            Thread.Sleep(16);

                        if (cancelSource.IsCancellationRequested)
                            Stop();

                        if (!client.IsLoggedIn)
                            Stop();
                    }
                }
                
                client.Status = null;

            }, state, cancelSource.Token);
        }

        protected virtual void ResumeMacro() => UpdateClientMacroStatus();

        protected virtual void StopMacro()
        {
            CancelTask();
            UpdateClientMacroStatus();
        }

        protected virtual void PauseMacro() => UpdateClientMacroStatus();

        protected void UpdateClientMacroStatus()
        {
            client.IsMacroRunning = Status == MacroStatus.Running;
            client.IsMacroPaused = Status == MacroStatus.Paused;
            client.IsMacroStopped = Status == MacroStatus.Stopped;
        }

        protected abstract void MacroLoop(object argument);

        protected virtual bool CancelTask(bool waitForTask = false)
        {
            if (cancelSource != null)
                cancelSource.Cancel();
            else
                return false;

            if (task != null && waitForTask && !task.IsCompleted)
                task.Wait();

            return true;
        }

        protected virtual void OnStatusChanged(MacroStatus status)
        {
            if (status == MacroStatus.Idle || status == MacroStatus.Stopped)
                client.Status = null;
            else
                client.Status = status.ToString();

            RaiseStatusChanged(status);
            UpdateClientMacroStatus();
        }

        protected void RaiseStatusChanged(MacroStatus status) => StatusChanged?.Invoke(this, new MacroStatusEventArgs(status));

        protected virtual void OnMapChanged()
        {
            var action = UserSettingsManager.Instance.Settings.MapChangeAction;
            TakeAction(action);
        }

        protected virtual void OnXYChanged()
        {
            var action = UserSettingsManager.Instance.Settings.CoordsChangeAction;
            TakeAction(action);
        }

        protected virtual void TakeAction(MacroAction action)
        {
            switch (action)
            {
                case MacroAction.Start:
                    Start();
                    break;
                case MacroAction.Resume:
                    Start();
                    break;
                case MacroAction.Restart:
                    Stop();
                    Start();
                    break;
                case MacroAction.Pause:
                    Pause();
                    break;
                case MacroAction.Stop:
                    Stop();
                    break;
                case MacroAction.ForceQuit:
                    Stop();
                    client.Terminate();
                    break;
            }
        }

        protected virtual void SaveKnownState()
        {
            lastKnownMapName = client.Location.Attributes.MapName;
            lastKnownMapNumber = client.Location.Attributes.MapNumber;
            lastKnownXCoordinate = client.Location.X;
            lastKnownYCoordinate = client.Location.Y;
        }

        protected virtual void CheckKnownState(bool saveStateAfterCheck = true)
        {
            if (!string.Equals(client.Location.Attributes.MapName, lastKnownMapName) ||
               client.Location.Attributes.MapNumber != lastKnownMapNumber)
            {
                OnMapChanged();
            }

            if (client.Location.X != lastKnownXCoordinate ||
               client.Location.Y != lastKnownYCoordinate)
            {
                OnXYChanged();
            }

            if (saveStateAfterCheck)
            {
                lastKnownMapName = client.Location.Attributes.MapName;
                lastKnownMapNumber = client.Location.Attributes.MapNumber;
                lastKnownXCoordinate = client.Location.X;
                lastKnownYCoordinate = client.Location.Y;
            }
        }
    }
}
