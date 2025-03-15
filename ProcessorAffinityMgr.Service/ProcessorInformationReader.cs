using System;
using System.Runtime.InteropServices;

namespace ProcessorAffinityMgr.Service
{
    public static class ProcessorInformationReader
    {
        public static bool GetCoreAffinityMasks()
        {
            var length = 0;
            GetLogicalProcessorInformationEx(0, IntPtr.Zero, ref length);
            var buffer = Marshal.AllocHGlobal(length);
            var pCoreMask = IntPtr.Zero;
            var eCoreMask = IntPtr.Zero;

            try
            {
                if (GetLogicalProcessorInformationEx(0, buffer, ref length))
                {
                    var ptr = buffer;
                    var offset = 0;
                    var core = 0;
                    long pMask = 0;
                    long eMask = 0;

                    while (offset < length)
                    {
                        var info = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(ptr);

                        if (info.Processor.EfficiencyClass == 1)
                            pMask |= 1L << core;
                        else
                            eMask |= 1L << core;

                        core += 1;
                        offset += info.Size;
                        ptr = IntPtr.Add(buffer, offset);
                    }

                    if (pMask != 0) pCoreMask = (IntPtr)pMask;
                    if (eMask != 0) eCoreMask = (IntPtr)eMask;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            ProcessAffinityMgrService.PCoreAffinityMask = pCoreMask;
            ProcessAffinityMgrService.ECoreAffinityMask = eCoreMask;

            return pCoreMask == IntPtr.Zero;
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
    }
}