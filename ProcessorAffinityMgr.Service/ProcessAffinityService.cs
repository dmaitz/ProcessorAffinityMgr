using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using Newtonsoft.Json;

namespace ProcessorAffinityMgr.Service
{
    public partial class ProcessAffinityService : ServiceBase
    {
        private static readonly string ConfigFilePath = AppDomain.CurrentDomain.BaseDirectory + "config.json";

        private static IntPtr _pCoreAffinityMask = IntPtr.Zero;
        private static List<string> _processNames = new List<string>();
        private FileSystemWatcher _configWatcher;
        private ManagementEventWatcher _watcher;

        protected override void OnStart(string[] args)
        {
            EventLog.Source = "ProcessAffinityService";

            _pCoreAffinityMask = GetPCoreAffinityMask();
            if (_pCoreAffinityMask == IntPtr.Zero)
            {
                EventLog.WriteEntry("No P-Cores detected. Service will stop.", EventLogEntryType.Error);
                Stop();
                return;
            }

            LoadConfiguration();
            SetupConfigWatcher();

            _watcher = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");
            _watcher.EventArrived += OnProcessStarted;
            _watcher.Start();

            EventLog.WriteEntry("Process-Monitoring started.");
        }

        protected override void OnStop()
        {
            _watcher?.Stop();
            _watcher?.Dispose();
            _configWatcher?.Dispose();
            EventLog.WriteEntry("Process-Monitoring stopped.");
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);

                    var config = JsonConvert.DeserializeObject<ConfigData>(json);
                    if (config?.Processes != null)
                    {
                        _processNames = config.Processes;
                        EventLog.WriteEntry($"config.json loaded: {string.Join(", ", _processNames)}");
                    }
                }
                else
                {
                    EventLog.WriteEntry("config.json not found.", EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Error loading configuration: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private void SetupConfigWatcher()
        {
            _configWatcher =
                new FileSystemWatcher(Path.GetDirectoryName(ConfigFilePath), Path.GetFileName(ConfigFilePath))
                {
                    NotifyFilter = NotifyFilters.LastWrite
                };
            _configWatcher.Changed += (s, e) =>
            {
                Thread.Sleep(1000);
                LoadConfiguration();
            };
            _configWatcher.EnableRaisingEvents = true;
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            var startedProcess = e.NewEvent["ProcessName"].ToString();
            var processId = Convert.ToInt32(e.NewEvent["ProcessID"]);

            if (_processNames.Contains(startedProcess, StringComparer.OrdinalIgnoreCase))
            {
                SetProcessAffinity(processId, _pCoreAffinityMask);
                EventLog.WriteEntry($"P-Core-Affinity set for {startedProcess} (PID: {processId}).");
            }
        }

        private static IntPtr GetPCoreAffinityMask()
        {
            var length = 0;
            GetLogicalProcessorInformationEx(0, IntPtr.Zero, ref length);
            var buffer = Marshal.AllocHGlobal(length);
            var pCoreMask = IntPtr.Zero;

            try
            {
                if (GetLogicalProcessorInformationEx(0, buffer, ref length))
                {
                    var ptr = buffer;
                    var offset = 0;
                    var core = 0;
                    long mask = 0;

                    while (offset < length)
                    {
                        var info = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(ptr);

                        if (info.Processor.EfficiencyClass == 1)
                            mask |= 1L << core;

                        core += 1;
                        offset += info.Size;
                        ptr = IntPtr.Add(buffer, offset);
                    }

                    if (mask != 0) pCoreMask = (IntPtr)mask;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return pCoreMask;
        }

        private void SetProcessAffinity(int processId, IntPtr affinityMask)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                process.ProcessorAffinity = affinityMask;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Error on {processId}: {ex.Message}", EventLogEntryType.Error);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool
            GetLogicalProcessorInformationEx(int relationshipType, IntPtr buffer, ref int length);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            public int Relationship;
            public int Size;
            public PROCESSOR_RELATIONSHIP Processor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESSOR_RELATIONSHIP
        {
            public byte Flags;
            public byte EfficiencyClass;
            public byte Reserved1;
            public byte Reserved2;
            public IntPtr GroupMask;
        }

        private class ConfigData
        {
            public List<string> Processes { get; set; }
        }
    }
}