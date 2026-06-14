using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bridge
{
    [method: JsonConstructor]
    public class Config()
    {
        [JsonPropertyName("exitTime")]
        public int ExitTime { get; set; } = 5;

        [JsonPropertyName("hideConsole")]
        public bool HideConsole { get; set; } = true;

        [JsonPropertyName("nonSteamAppId")]
        public string NonSteamAppID { get; set; } = "-1";
    }
}