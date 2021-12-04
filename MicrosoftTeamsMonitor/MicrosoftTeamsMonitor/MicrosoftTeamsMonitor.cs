using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Configs;
using IOTLinkAddon.Common.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.HomeAssistant;
using System.Diagnostics;

namespace MicrosoftTeamsMonitor
{
    public class TeamsStatus : ServiceAddon
    {
        private string _TeamsStatusTopic;
        private string _configPath;
        private Configuration _config;
        private MicrosoftTeamsConfig _microsoftTeamsConfig;

        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);
            _TeamsStatusTopic = "Status";

            GetManager().PublishDiscoveryMessage(this, _TeamsStatusTopic, "Status", new HassDiscoveryOptions
            {
                Id = "TeamsStatus",
                Unit = "",
                Name = "TeamsStatus",
                Component = HomeAssistantComponent.Sensor,
                Icon = "mdi:microsoft-teams"
            });

            var cfgManager = ConfigurationManager.GetInstance();
            _configPath = Path.Combine(_currentPath, "config.yaml");
            _config = cfgManager.GetConfiguration(_configPath);

            TailLog();
        }

        private async void TailLog()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Get Teams LogFile path from config.yaml
                    _microsoftTeamsConfig = MicrosoftTeamsConfig.FromConfiguration(_config.GetValue("teams"));
                    var logFile = _microsoftTeamsConfig.LogFile;

                    if (File.Exists(logFile))
                    {

                        // Tail log file (Tail.NET by Tailor Wood, https://www.codeproject.com/Articles/7568/Tail-NET)
                        using (StreamReader reader = new StreamReader(new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                        {
                            //start at the end of the file
                            long lastMaxOffset = reader.BaseStream.Length;

                            int i = 0;
                            bool Unknown = false;
                            while (true)
                            {
                                System.Threading.Thread.Sleep(1000);

                                //if the file size has not changed, idle
                                if (reader.BaseStream.Length == lastMaxOffset)
                                {
                                    if (i > 60)  // Check Teams process every minute (not too spamy)
                                    {
                                        if (!TeamsIsRunning())
                                        {
                                            if (!Unknown)
                                            {
                                                // Publish Unknown because Teams isn't running
                                                LoggerHelper.Info($"Sending status: Unknown");
                                                GetManager().PublishMessage(this, _TeamsStatusTopic, "Unknown");
                                                Unknown = true; // Just published "Unknown" lets not do it again until our Unknown status has changed
                                            }
                                        }
                                        i = 0;  // Reset counter because 60 iterations/seconds have passed
                                    }

                                    i++;
                                    continue;
                                }
                                i = 0;  // Reset the counter because log file was written to, meaning the service is running and providing status


                                //seek to the last max offset
                                reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                                //read out of the file until the EOF
                                string line = "";

                                // Use Regex to identify relevant log entries (Egglestron, https://community.home-assistant.io/t/microsoft-teams-status/202388/38?u=ledhed)
                                string pattern = @"(?<=StatusIndicatorStateService: Added )(\w+)";
                                Regex rgx = new Regex(pattern);

                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (rgx.IsMatch(line))
                                    {
                                        string Status = rgx.Split(line)[1];
                                        if (Status != "NewActivity")
                                        {
                                            // Publish Teams Status
                                            LoggerHelper.Info($"Sending status: {Status}");
                                            GetManager().PublishMessage(this, _TeamsStatusTopic, Status);
                                            Unknown = false;    // Status changed, reset so we can publish "Unknown" if the Teams app isn't running
                                        }
                                    }
                                }

                                //update the last max offset
                                lastMaxOffset = reader.BaseStream.Position;
                            }
                        }
                    }
                    else
                    {
                        LoggerHelper.Info($"File not found: {logFile}");
                    }
                }
                catch (Exception exception)
                {
                    LoggerHelper.Error("Failed to send status " + exception);
                }
            });
        }

        private bool TeamsIsRunning()
        {
            const string subkey = @"SOFTWARE\IM Providers\Teams";
            bool IsRunning = false;
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subkey))
                {
                    if (key != null)
                    {
                        Object processName = key.GetValue("ProcessName");
                        if (processName != null)
                        {
                            Process[] pname = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName.ToString()));
                            if (pname.Length > 0)
                            {
                                IsRunning = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //react appropriately
                LoggerHelper.Error(ex.StackTrace);
            }

            return IsRunning;
        }
    }
}