using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerRequestGenerator
    {
        IndexerPageableRequestChain GetRecentRequests();
        IndexerPageableRequestChain GetSearchRequests(SingleEpisodeSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(SeasonSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(DailyEpisodeSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(DailySeasonSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(AnimeEpisodeSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(AnimeSeasonSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(SpecialEpisodeSearchCriteria searchCriteria);

        // Only some indexers can search for complete series packs, others return no request
        IndexerPageableRequestChain GetSearchRequests(CompleteSeriesSearchCriteria searchCriteria) => new IndexerPageableRequestChain();
    }
}
