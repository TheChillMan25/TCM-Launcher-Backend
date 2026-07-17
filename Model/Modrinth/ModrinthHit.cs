using System.Text.Json.Serialization;

namespace TCM_Launcher_Backend.Model.Modrinth
{
    public class ModrinthHit
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("client_side")]
        public string ClientSide { get; set; }
        [JsonPropertyName("server_side")]
        public string ServerSide { get; set; }
        [JsonPropertyName("downloads")]
        public uint Downloads { get; set; }
        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }
        [JsonPropertyName("author")]
        public string Author { get; set; }
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; }
    }
}