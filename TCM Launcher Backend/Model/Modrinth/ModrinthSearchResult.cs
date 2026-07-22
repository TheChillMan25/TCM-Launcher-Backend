using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TCM_Launcher_Backend.Model.Modrinth
{
    public class ModrinthSearchResult
    {
        [JsonPropertyName("hits")]
        public List<ModrinthHit> Hits { get; set; }
    }
}
