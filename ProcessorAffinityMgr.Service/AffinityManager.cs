using System;
using System.Diagnostics;
using System.Linq;

namespace ProcessorAffinityMgr.Service
{
    public class AffinityManager
    {
        public AffinityManager()
        {
            ProcessAffinityMgrService.ProcessWatcher.ProcessStarted += ProcessWatcher_ProcessStarted;
        }

        private void ProcessWatcher_ProcessStarted(object sender, ProcessWatcher.ProcessStartedInfoEventArgs e)
        {
            if (ProcessAffinityMgrService.PCoreProcesses.Contains(e.Name, StringComparer.OrdinalIgnoreCase))
            {
                SetProcessAffinity(e.Id, ProcessAffinityMgrService.PCoreAffinityMask);
                ProcessAffinityMgrService.ServiceEventLog.WriteEntry(
                    $"P-Core-Affinity set for {e.Name} (PID: {e.Id}).");
            }
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
                ProcessAffinityMgrService.ServiceEventLog.WriteEntry($"Error on {processId}: {ex.Message}",
                    EventLogEntryType.Error);
            }
        }
    }
}