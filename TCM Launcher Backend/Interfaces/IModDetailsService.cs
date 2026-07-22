using TCML_Class_library;

namespace TCM_Launcher_Backend
{
    public interface IModDetailsService
    {
        Task<ModDetails?> GetModrinthModDetails(string procjectId, string version);
        Task<ModDetails?> GetCurseforgeModDetails(string procjectId, string version, bool needDesc = true);
    }
}
