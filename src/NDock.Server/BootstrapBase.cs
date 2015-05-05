﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using NDock.Base;
using NDock.Base.Config;

namespace NDock.Server
{
    abstract class BootstrapBase : IBootstrap
    {
        private IConfigSource m_ConfigSource;

        protected List<IManagedApp> ManagedApps { get; private set; }

        protected ExportProvider ExportProvider { get; private set; }

        public BootstrapBase(IConfigSource configSource)
        {
            if (configSource == null)
                throw new ArgumentNullException("configSource");

            m_ConfigSource = configSource;
            ManagedApps = new List<IManagedApp>();
            ExportProvider = CreateExportProvider();
        }

        protected virtual ExportProvider CreateExportProvider()
        {
            var catalog = new DirectoryCatalog(System.AppDomain.CurrentDomain.BaseDirectory);
            return new CompositionContainer(catalog);
        }

        public virtual void Start()
        {
            foreach (var app in ManagedApps)
            {
                app.Start();
            }
        }

        public virtual void Stop()
        {
            foreach (var app in ManagedApps)
            {
                app.Stop();
            }
        }
    }
}
