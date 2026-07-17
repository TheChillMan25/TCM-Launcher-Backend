using System.Text.Json.Serialization;

namespace TCM_Launcher_Backend.Model.Curseforge
{
    public class CurseforgeHit
    {
        [JsonPropertyName("id")]
        public uint Id { get; set; }
        [JsonPropertyName ("name")]
        public string Name { get; set; }
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
        [JsonPropertyName("downloadCount")]
        public uint DownloadCount { get; set; }
        [JsonPropertyName("authors")]
        public List<CurseforgeAuthor> Authors { get; set; }
        [JsonPropertyName("categories")]
        public List<CurseforgeCategory> Categories { get; set; }
        [JsonPropertyName("logo")]
        public CurseforgeModAsset Logo { get; set; }
    }

    public class CurseforgeAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class CurseforgeCategory
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
    }

    public class CurseforgeModAsset
    {
        [JsonPropertyName("thumbnailUrl")]
        public string ThumbnailUrl { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}