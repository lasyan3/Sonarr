namespace Sonarr.Api.V3.Series
{
    public class AlternateTitleAddResource
    {
        public int SeriesId { get; set; }
        public string Title { get; set; }
        public int? SeasonNumber { get; set; }
        public int? SceneSeasonNumber { get; set; }
    }
}
