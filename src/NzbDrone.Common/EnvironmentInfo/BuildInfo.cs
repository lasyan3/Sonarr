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
            "IgnoreAlternateTitles",
            "SeasonSearch: if season not finished, search individual episodes",
            "AnimeEpisodeSearch: search absolute episode with E",
            "AnimeFullSearchCriteria : if no results found and only 1 season, try search without season number (GunGrave)",
            "SearchAnimeSeason: search individual episodes only if this is the last season and serie is not finished",
        };
    }
}
