using System;
using System.Management;

namespace ProcessorAffinityMgr.Service
{
    public class ProcessWatcher : IDisposable
    {
        public event EventHandler<ProcessStartedInfoEventArgs> ProcessStarted;
        
        private readonly ManagementEventWatcher _watcher;

        public ProcessWatcher()
        {
            _watcher = new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'");
            _watcher.EventArrived += OnProcessStarted;
            _watcher.Start();

            ProcessAffinityMgrService.ServiceEventLog.WriteEntry("Process-Monitoring started.");
        }

        public void Dispose()
        {
            _watcher?.Stop();
            _watcher?.Dispose();

            ProcessAffinityMgrService.ServiceEventLog.WriteEntry("Process-Monitoring stopped.");
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            var process = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            var processStartedInfo = new ProcessStartedInfoEventArgs
            {
                Id = Convert.ToInt32(process["ProcessId"]),
                Name = process["Name"].ToString(),
                CommandLine = process["CommandLine"]?.ToString() ?? ""
            };

            ProcessStarted?.Invoke(this, processStartedInfo);
        }

        public class ProcessStartedInfoEventArgs : EventArgs
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string CommandLine { get; set; }
        }
    }
}