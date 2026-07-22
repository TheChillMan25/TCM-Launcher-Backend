using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TCM_Launcher_Backend.Model.Curseforge
{
    public class CurseforgeSearchResult
    {
        [JsonPropertyName("data")]
        public List<CurseforgeHit> Data { get; set; }
    }
}
