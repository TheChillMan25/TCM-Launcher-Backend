using System.Text.Json.Serialization;

namespace TCM_Launcher_Backend.Model.Modrinth
{
    public class ModrinthDetails : ModrinthHit
    {
        [JsonPropertyName("id")]
        new public string ProjectId { get; set; }
        [JsonPropertyName("body")]
        public string Body { get; set; }
        [JsonPropertyName("versions")]
        public List<string> Versions { get; set; }
    }

    public class ModrinthVersion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }
        [JsonPropertyName("version_number")]
        public string VersionNumber { get; set; }
        [JsonPropertyName("version_type")]
        public string VersionType { get; set; }
        [JsonPropertyName("downloads")]
        public uint Downloads { get; set; }
        [JsonPropertyName("files")]
        public List<ModrinthFile> Files { get; set; }
        [JsonPropertyName("dependencies")]
        public List<ModrinthDependency> Dependencies { get; set; }
    }

    public class ModrinthFile
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("primary")]
        public bool Primary { get; set; }
        [JsonPropertyName("filename")]
        public string FileName { get; set; }
        [JsonPropertyName("size")]
        public uint Size { get; set; }
    }

    public class ModrinthDependency
    {
        [JsonPropertyName("version_id")]
        public string VersionId { get; set; }
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }
        [JsonPropertyName("file_name")]
        public string FileName { get; set; }
        [JsonPropertyName("dependency_type")]
        public string DependencyType { get; set; }
    }

    public class ModrinthMember
    {
        [JsonPropertyName("user")]
        public ModrinthUser User { get; set; }
    }

    public class ModrinthUser
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }
    }
}
