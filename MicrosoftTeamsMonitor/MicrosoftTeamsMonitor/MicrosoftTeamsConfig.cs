using IOTLinkAPI.Configs;
using System.Collections.Generic;

namespace IOTLinkAddon.Common.Configs
{
    public class MicrosoftTeamsConfig
    {
        public string LogFile { get; set; }

        public static MicrosoftTeamsConfig FromConfiguration(Configuration configuration)
        {
            return new MicrosoftTeamsConfig
            {
                LogFile = configuration.GetValue("logfile", "")
            };
        }
    }
}
