﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDock.Base;
using NDock.Base.Config;
using NDock.Base.Metadata;

namespace NDock.Server.Isolation
{
    abstract class IsolationApp : IManagedApp
    {
        private AppServerMetadataAttribute m_Metadata;

        protected IManagedApp ManagedApp { get; private set; }

        protected const string WorkingDir = "AppRoot";

        protected IsolationApp(AppServerMetadataAttribute metadata)
        {
            State = ServerState.NotInitialized;
            m_Metadata = metadata;
        }

        public string Name { get; private set; }

        public bool Setup(IServerConfig config)
        {
            State = ServerState.Initializing;
            Config = config;
            Name = config.Name;
            State = ServerState.NotStarted;
            return true;
        }

        public IServerConfig Config { get; private set; }

        public AppServerMetadataAttribute GetMetadata()
        {
            return m_Metadata;
        }

        protected abstract IManagedApp CreateAndStartServerInstance();

        public bool Start()
        {
            State = ServerState.Starting;

            ManagedApp = CreateAndStartServerInstance();

            if (ManagedApp != null)
            {
                State = ServerState.Running;
                return true;
            }
            else
            {
                State = ServerState.NotStarted;
                return false;
            }
        }

        void IManagedAppBase.Stop()
        {
            var app = ManagedApp;

            if (app == null)
                return;

            State = ServerState.Stopping;
            app.Stop();

            m_StopTaskSrc = new TaskCompletionSource<bool>();

            var stopTask = m_StopTaskSrc.Task;

            Stop();

            stopTask.Wait();
        }

        private TaskCompletionSource<bool> m_StopTaskSrc;

        protected virtual void OnStopped()
        {
            State = ServerState.NotStarted;
            ManagedApp = null;
            m_StopTaskSrc.SetResult(true);
        }

        protected abstract void Stop();

        public ServerState State { get; protected set; }

        public event EventHandler<ErrorEventArgs> ExceptionThrown;

        protected void OnExceptionThrown(Exception exc)
        {
            var handler = ExceptionThrown;

            if (handler == null)
                return;

            handler(this, new ErrorEventArgs(exc));
        }
    }
}
