using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProcessorAffinityMgr.Service
{
    public static class ProcessorInformationReader
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetLogicalProcessorInformationEx(
        LOGICAL_PROCESSOR_RELATIONSHIP relationshipType,
        IntPtr buffer,
        ref int returnedLength);

        enum LOGICAL_PROCESSOR_RELATIONSHIP
        {
            RelationProcessorCore = 0,
            RelationNumaNode,
            RelationCache,
            RelationProcessorPackage,
            RelationGroup
        }

        [StructLayout(LayoutKind.Sequential)]
        struct GROUP_AFFINITY
        {
            public ulong Mask;
            public ushort Group;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public ushort[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESSOR_RELATIONSHIP
        {
            public byte Flags;
            public byte EfficiencyClass;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Reserved;
            public short GroupCount;
            public GROUP_AFFINITY GroupMask;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
            public int Size;
            public PROCESSOR_RELATIONSHIP Processor;
        }

        public static bool GetCoreAffinityMasksAutodetect()
        {
            long pCoreMask = 0L;
            long eCoreMask = 0L;
            int coreCount = 0;

            int bufferSize = 0;
            GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, IntPtr.Zero, ref bufferSize);

            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                if (GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, buffer, ref bufferSize))
                {
                    IntPtr ptr = buffer;
                    int bytesRemaining = bufferSize;
                    List<string> coreInfoList = new List<string>();

                    while (bytesRemaining > 0)
                    {
                        SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX info = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(ptr);
                        string coreType = (info.Processor.EfficiencyClass == 0) ? "E-Core" : "P-Core";
                        bool isHyperThreaded = (info.Processor.Flags & 0x1) != 0;
                        string hyperThreading = isHyperThreaded ? "HT Enabled" : "No HT";
                        coreInfoList.Add($"{coreType} - {hyperThreading}");

                        if (info.Processor.EfficiencyClass > 0)
                        {
                            pCoreMask |= (1L << coreCount);
                            coreCount++;

                            if (isHyperThreaded)
                            {
                                pCoreMask |= (1L << coreCount);
                                coreCount++;
                            }
                        }

                        if (info.Processor.EfficiencyClass == 0)
                        {
                            eCoreMask |= (1L << coreCount);
                            coreCount++;

                            if (isHyperThreaded)
                            {
                                eCoreMask |= (1L << coreCount);
                                coreCount++;
                            }
                        }

                        ptr += info.Size;
                        bytesRemaining -= info.Size;
                    }

                    ProcessAffinityMgrService.ServiceEventLog.WriteEntry($"Autodetect:\nP-Cores: {Convert.ToString(pCoreMask, 2)}\nE-Cores: {Convert.ToString(eCoreMask, 2)}\n\n{string.Join("\n", coreInfoList)}");

                    ProcessAffinityMgrService.PCoreAffinityMask = (IntPtr)pCoreMask;
                    ProcessAffinityMgrService.ECoreAffinityMask = (IntPtr)eCoreMask;

                }
                else
                {
                    Console.WriteLine("Failed to retrieve processor information.");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return (pCoreMask == 0 || eCoreMask == 0);
        }
        
        private static bool GetCoreAffinityMasksFromConfig()
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

            ProcessAffinityMgrService.ServiceEventLog.WriteEntry($"Configuration:\nP-Cores: {Convert.ToString(pCoreMask, 2)}\nE-Cores: {Convert.ToString(eCoreMask, 2)}\nbased on configuration.");
            
            ProcessAffinityMgrService.PCoreAffinityMask = (IntPtr)pCoreMask;
            ProcessAffinityMgrService.ECoreAffinityMask = (IntPtr)eCoreMask;

            return (pCoreMask == 0 || eCoreMask == 0);
        }

        public static bool GetCoreAffinityMasks()
        {
            if (ProcessAffinityMgrService.Config.PCoreCount > 0)
                return GetCoreAffinityMasksFromConfig();

            return GetCoreAffinityMasksAutodetect();
        }
    }
}