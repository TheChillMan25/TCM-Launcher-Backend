using System.Text.Json.Serialization;

namespace TCM_Launcher_Backend.Model.Curseforge
{
    public class CurseforgeDetails : CurseforgeHit
    {
        [JsonPropertyName("latestFiles")]
        public List<CurseforgeFile> Files { get; set; }
    }

    public class CurseforgeFile
    {
        [JsonPropertyName("id")]
        public uint Id { get; set; }
        [JsonPropertyName("modId")]
        public uint ModId { get; set; }
        [JsonPropertyName("downloadCount")]
        public uint DownloadCount { get; set; }
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
        [JsonPropertyName("releaseType")]
        public uint ReleaseType { get; set; }
        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; }
        [JsonPropertyName("fileLength")]
        public uint FileLength { get; set; }
        [JsonPropertyName("dependencies")]
        public List<CurseforgeDependency> Dependencies { get; set; }
    }

    public class CurseforgeDependency
    {
        [JsonPropertyName("modId")]
        public uint ModId { get; set; }
        [JsonPropertyName("relationType")]
        public uint RelationType { get; set; }
    }

    public class CurseforgeResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    public class CurseforgeFiles
    {
        [JsonPropertyName("data")]
        public List<CurseforgeFile> Data { get; set; }
    }
}
