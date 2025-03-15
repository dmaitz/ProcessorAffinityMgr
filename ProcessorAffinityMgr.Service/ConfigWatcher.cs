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

                    var config = JsonConvert.DeserializeObject<ConfigData>(json);
                    if (config?.Processes != null)
                    {
                        ProcessAffinityMgrService.PCoreProcesses = config.Processes;
                        ProcessAffinityMgrService.ServiceEventLog.WriteEntry(
                            $"config.json loaded: {string.Join(", ", config.Processes)}");
                    }
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

        private class ConfigData
        {
            public List<string> Processes { get; set; }
        }
    }
}