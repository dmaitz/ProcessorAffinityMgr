﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;

namespace ProcessorAffinityMgr.Service
{
    public class ProcessAffinityMgrService : ServiceBase
    {
        public static EventLog ServiceEventLog;

        public static IntPtr PCoreAffinityMask = IntPtr.Zero;
        public static IntPtr ECoreAffinityMask = IntPtr.Zero;

        public static List<string> PCoreProcesses = new List<string>();

        public static ProcessWatcher ProcessWatcher;
        public static AffinityManager AffinityManager;
        internal static ConfigWatcher ConfigWatcher;

        public ProcessAffinityMgrService()
        {
            ServiceName = "ProcessAffinityMgrService";
        }

        protected override void OnStart(string[] args)
        {
            EventLog.Source = "ProcessAffinityMgrService";
            ServiceEventLog = EventLog;

            if (ProcessorInformationReader.GetCoreAffinityMasks())
            {
                EventLog.WriteEntry("No P-Cores detected. Service will stop.", EventLogEntryType.Error);
                Stop();
                return;
            }

            ProcessWatcher = new ProcessWatcher();
            ConfigWatcher = new ConfigWatcher();
            AffinityManager = new AffinityManager();
        }

        protected override void OnStop()
        {
            ProcessWatcher.Dispose();
            ConfigWatcher.Dispose();
        }
    }
}