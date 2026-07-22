using TCM_Launcher_Backend.Model.Curseforge;
using TCML_Class_library;

namespace TCM_Launcher_Backend.Interfaces
{
    public interface IModSearchService
    {
        Task<List<ModSearchResult>> SearchModsAsync(string query, string version);
    }
}
