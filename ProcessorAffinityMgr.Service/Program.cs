using System.ServiceProcess;

namespace ProcessorAffinityMgr.Service
{
    internal static class Program
    {
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ProcessAffinityService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}