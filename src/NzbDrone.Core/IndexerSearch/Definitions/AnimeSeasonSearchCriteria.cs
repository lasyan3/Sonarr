namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class AnimeSeasonSearchCriteria : SearchCriteriaBase
    {
        public int SeasonNumber { get; set; }
        public bool IgnoreSeason { get; set; }

        public override string ToString()
        {
            return $"[{Series.Title}" + (IgnoreSeason ? " : ALL" : $" : S{SeasonNumber:00}]");
        }
    }
}
