using System;
using System.Runtime.InteropServices;

namespace ProcessorAffinityMgr.Service
{
    public static class ProcessorInformationReader
    {
        public static bool GetCoreAffinityMasks()
        {
            int logicalProcessorCount = Environment.ProcessorCount;
            long pCoreMask = 0L;
            long eCoreMask = 0L;

            for (int i = 0; i < logicalProcessorCount; i++)
            {
                if (i < ProcessAffinityMgrService.Config.PCoreCount)
                    pCoreMask |= (1L << i);
                else
                    eCoreMask |= (1L << i);
            }

            ProcessAffinityMgrService.ServiceEventLog.WriteEntry($"P-Cores: {Convert.ToString(pCoreMask, 2)}\nE-Cores: {Convert.ToString(eCoreMask, 2)}");
            
            ProcessAffinityMgrService.PCoreAffinityMask = (IntPtr)pCoreMask;
            ProcessAffinityMgrService.ECoreAffinityMask = (IntPtr)eCoreMask;

            return (pCoreMask == 0);
        }

    }
}