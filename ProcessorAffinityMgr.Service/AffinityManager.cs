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
            
            var matchingRules = ProcessAffinityMgrService.Config.ProcessRules
                .Where(rule => rule.ProcessName.Equals(e.Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(rule => rule.Arguments.Length) 
                .ToList();

            foreach (var rule in matchingRules)
            {
                if (rule.Arguments == "" || e.CommandLine.ToLower().Contains(rule.Arguments.ToLower()))
                {
                    ProcessAffinityMgrService.ServiceEventLog.WriteEntry($"Set processor-affinity for {e.Name} (PID: {e.Id}) to: {rule.CoreType}", EventLogEntryType.Information);

                    switch (rule.CoreType)
                    {
                        case "p-core": 
                            SetProcessAffinity(e.Id, ProcessAffinityMgrService.PCoreAffinityMask);
                            break;
                        
                        case "e-core":
                            SetProcessAffinity(e.Id, ProcessAffinityMgrService.ECoreAffinityMask);
                            break;
                    }

                    return;
                }
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