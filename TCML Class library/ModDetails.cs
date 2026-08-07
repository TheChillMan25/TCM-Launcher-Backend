namespace TCML_Class_library
{
    public class ModDetails : ModSearchResult
    {
        public string Description { get; set; }
        public List<ModVersion> Versions{ get; set; }
    }

    public class ModVersion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ProjectId { get; set; }
        public string VersionNumber { get; set; }
        public string VersionType { get; set; }
        public ModSource Source { get; set; }
        public uint Downloads { get; set; }
        public List<ModFile> Files { get; set; }
        public List<ModDependency> Dependencies { get; set; }
    }

    public class ModFile
    {
        public string Url { get; set; }
        public bool Primary { get; set; }
        public string FileName { get; set; }
        public uint Size { get; set; }
    }

    public class ModDependency
    {
        public string VersionId { get; set; }
        public string ProjectId { get; set; }
        public string FileName { get; set; }
        public string DependencyType { get; set; }

    }
}
