using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NzbDrone.Common.EnvironmentInfo
{
    public static class BuildInfo
    {
        static BuildInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();

            Version = assembly.GetName().Version;

            var attributes = assembly.GetCustomAttributes(true);

            Branch = "unknow";

            var config = attributes.OfType<AssemblyConfigurationAttribute>().FirstOrDefault();
            if (config != null)
            {
                Branch = config.Configuration;
            }

            Release = $"{Version}-{Branch}";
        }

        public static string AppName { get; } = "Sonarr";

        public static Version Version { get; }
        public static string Branch { get; }
        public static string Release { get; }

        public static DateTime BuildDateTime
        {
            get
            {
                var fileLocation = Assembly.GetCallingAssembly().Location;
                return new FileInfo(fileLocation).LastWriteTimeUtc;
            }
        }

        public static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public static IReadOnlyCollection<string> Addons { get; } = new List<string>()
        {
            "Global Searching: add option to ignore alternate titles",
            "Season Searching: if season not fully aired, search individual episodes",
            "Anime Episode Search: add search using absolute episode with E (One Piece E1152)",
            "Anime Season Search: if no results found and anime has only 1 season, search without season number (GunGrave)",
            "Anime Season Search: do not search individual episodes unless the season is not fully aired",
            "Manual import: ignore files which are links"
        };
    }
}
