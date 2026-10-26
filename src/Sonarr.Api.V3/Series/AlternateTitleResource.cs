using NzbDrone.Common.Extensions;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.DataAugmentation.Xem;

namespace Sonarr.Api.V3.Series
{
    public enum AlternateTitleSource
    {
        Unknown,
        Services,
        Xem,
        Custom
    }

    public class AlternateTitleResource
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int? SeasonNumber { get; set; }
        public int? SceneSeasonNumber { get; set; }
        public string SceneOrigin { get; set; }
        public string Comment { get; set; }
        public AlternateTitleSource Source { get; set; }
        public bool IsExcluded { get; set; }
        public bool IsIgnored { get; set; }
    }

    public static class AlternateTitleResourceMapper
    {
        public static AlternateTitleResource ToResource(this SceneMapping sceneMapping)
        {
            if (sceneMapping == null)
            {
                return null;
            }

            var comment = sceneMapping.Comment;

            if (comment.IsNullOrWhiteSpace() && sceneMapping.FilterRegex.IsNotNullOrWhiteSpace())
            {
                comment = "Limited matching";
            }

            return new AlternateTitleResource
            {
                Id = sceneMapping.Id,
                Title = sceneMapping.Title,
                SeasonNumber = sceneMapping.SeasonNumber,
                SceneSeasonNumber = sceneMapping.SceneSeasonNumber,
                SceneOrigin = sceneMapping.SceneOrigin,
                Comment = comment,
                Source = GetSource(sceneMapping.Type)
            };
        }

        private static AlternateTitleSource GetSource(string type)
        {
            switch (type)
            {
                case nameof(ServicesProvider):
                    return AlternateTitleSource.Services;
                case nameof(XemService):
                    return AlternateTitleSource.Xem;
                case SceneMappingService.CustomMappingType:
                    return AlternateTitleSource.Custom;
                default:
                    return AlternateTitleSource.Unknown;
            }
        }
    }
}
