using System.Collections.Generic;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class CompleteSeriesSearchCriteria : SearchCriteriaBase
    {
        public static readonly List<string> Keywords = new List<string> { "Integrale", "Complete" };

        public override string ToString()
        {
            return string.Format("[{0} : Complete Series]", Series.Title);
        }
    }
}
