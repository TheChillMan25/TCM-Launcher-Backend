using TCML_Class_library;

namespace TCM_Launcher_Backend
{
    public interface IModDetailsService
    {
        Task<List<ModVersion>?> GetCombinedModVersions(string? modrinthId, string? curseforgeId, string mcVersion);
        Task<ModDetails?> GetModrinthModDetails(string procjectId, string modrinthId, string curseforgeId, string version);
        Task<ModDetails?> GetCurseforgeModDetails(string procjectId, string modrinthId, string curseforgeId, string version, bool needDesc = true);
    }
}
