namespace TCML_Class_library
{
    public class ModSearchResult
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Author { get; set; }
        public string? IconUrl { get; set; }
        public uint DownloadCount { get; set; }
        public ModSource Source { get; set; }
        public string Client_Side { get; set; }
        public string Server_Side { get; set; }
        public List<string> Categories { get; set; }
    }

    public enum ModSource
    {
        Modrinth,
        CurseForge
    }
}
