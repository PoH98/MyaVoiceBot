using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyaVoiceBot.Model
{
    internal class Configuration
    {
        public string? Token { get; set; }
        public ulong VoiceCreationChannel { get; set; }
        public ulong VoiceCategory { get; set; }
    }
}
