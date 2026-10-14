namespace Sonarr.Api.V3.Series
{
    public class AlternateTitleExclusionResource
    {
        public int SeriesId { get; set; }
        public string Title { get; set; }
        public bool IsExcluded { get; set; }
    }
}
