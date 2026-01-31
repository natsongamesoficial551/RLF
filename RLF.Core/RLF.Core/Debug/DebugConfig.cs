using System.Collections.Generic;

namespace RLF.Core.Debug
{
    public class DebugConfig
    {
        public bool Enabled { get; set; } = true;
        public DebugLevel MinLevel { get; set; } = DebugLevel.Info;

        // Controle fino por canal
        public Dictionary<DebugChannel, bool> ChannelEnabled { get; }
            = new Dictionary<DebugChannel, bool>();

        public bool IsChannelEnabled(DebugChannel channel)
        {
            if (!ChannelEnabled.ContainsKey(channel))
                return true; // padrão ligado

            return ChannelEnabled[channel];
        }
    }
}
