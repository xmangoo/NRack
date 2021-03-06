﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRack.Base;
using NRack.Base.Configuration;
using NRack.Base.Metadata;
using NRack.Base.Provider;

namespace NRack.Server.Recycle
{
    [Export(typeof(IRecycleTrigger))]
    [ProviderMetadata(TriggerName)]
    public class MemoryRecycleTrigger : IRecycleTrigger
    {
        private long m_MaxMemoryUsage;

        internal const string TriggerName = "MemoryTrigger";

        public string Name
        {
            get
            {
                return TriggerName;
            }
        }

        public bool Initialize(NameValueCollection options)
        {
            if (long.TryParse(options.GetValue("maxMemoryUsage"), out m_MaxMemoryUsage) || m_MaxMemoryUsage <= 0)
                return false;

            return true;
        }

        public bool NeedBeRecycled(IManagedApp app, StatusInfoCollection status)
        {
            var memoryUsage = status[StatusInfoKeys.MemoryUsage];

            if (memoryUsage == null)
                return false;

            return (long)memoryUsage >= m_MaxMemoryUsage;
        }
    }
}
