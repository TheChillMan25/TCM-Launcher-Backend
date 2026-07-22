using System;
using System.Collections.Generic;
using System.Text;
using TCM_Launcher_Backend.Model.Curseforge;

namespace TCM_Launcher_Backend.Services
{
    public static class CurseforgeHelper
    {
        public static Dictionary<string, string> ConvertForgeCategoriesToEnviroments(List<CurseforgeCategory> categories)
        {
            var enviroments = new Dictionary<string, string>();

            enviroments["ClientSide"] = "required";
            enviroments["ServerSide"] = "required";

            if (categories != null)
            {
                bool isClientOnly = false;
                bool isServerOnly = false;
                bool hasContent = false;

                foreach (var cat in categories)
                {
                    string slug = cat.Slug?.ToLowerInvariant() ?? "";

                    if (slug == "cosmetic" ||
                        slug == "map-and-information" ||
                        slug == "twitch-integration" ||
                        slug == "mc-miscellaneous")
                    {
                        isClientOnly = true;
                    }
                    else if (slug == "server-utility")
                    {
                        isServerOnly = true;
                    }
                    else if (slug == "armor-tools-and-weapons" ||
                     slug == "technology" ||
                     slug == "magic" ||
                     slug == "storage" ||
                     slug == "food" ||
                     slug == "adventure-and-rpg" ||
                     slug == "world-gen" ||
                     slug == "biomes" ||
                     slug == "dimensions" ||
                     slug == "mobs" ||
                     slug == "ores-and-resources" ||
                     slug == "structures")
                    {
                        hasContent = true;
                    }
                }

                if (isServerOnly)
                {
                    enviroments["ClientSide"] = "unsupported";
                    enviroments["ServerSide"] = "required";
                }
                if (isServerOnly && !hasContent)
                {
                    enviroments["ClientSide"] = "required";
                    enviroments["ServerSide"] = "unsupported";
                }
            }

            return enviroments;
        }

    }
}
