namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class SerieSearchCriteria : SearchCriteriaBase
    {
        public override string ToString()
        {
            return string.Format("[{0}]", Series.Title);
        }
    }
}
