using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace ProcessorAffinityMgr.Service
{
    internal class ConfigWatcher : IDisposable
    {
        private static readonly string ConfigFilePath = AppDomain.CurrentDomain.BaseDirectory + "config.json";

        private readonly FileSystemWatcher _configWatcher;

        public ConfigWatcher()
        {
            LoadConfiguration();

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

        public void Dispose()
        {
            _configWatcher?.Dispose();
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);

                    ProcessAffinityMgrService.Config = JsonConvert.DeserializeObject<AffinityMgrConfig>(json);
                    ProcessAffinityMgrService.ServiceEventLog.WriteEntry("config.json loaded.");
                }
                else
                {
                    ProcessAffinityMgrService.ServiceEventLog.WriteEntry("config.json not found.",
                        EventLogEntryType.Error);
                }
            }
            catch (Exception ex)
            {
                ProcessAffinityMgrService.ServiceEventLog.WriteEntry($"Error loading configuration: {ex.Message}",
                    EventLogEntryType.Error);
            }
        }
    }

    public class AffinityMgrConfig
    {
        public int PCoreCount { get; set; }
        public List<ProcessRule> ProcessRules { get; set; }

    }

    public class ProcessRule
    {
        public string ProcessName { get; set; }
        public string Arguments { get; set; } = "";
        public string CoreType { get; set; }
    }
}