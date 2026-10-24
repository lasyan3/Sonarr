using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class MultiSeasonSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MultiSeasonSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            var parsedEpisodeInfo = subject.ParsedEpisodeInfo;
            var isCompleteSeriesPack = parsedEpisodeInfo.IsCompleteSeries || (parsedEpisodeInfo.IsMultiSeason && parsedEpisodeInfo.FullSeason);

            // Complete series and multi-season packs are only grabbed by a complete series search, which only wants those
            if (searchCriteria is CompleteSeriesSearchCriteria)
            {
                if (!isCompleteSeriesPack)
                {
                    _logger.Debug("Release {0} rejected. Not a complete series or multi-season pack", subject.Release.Title);
                    return DownloadSpecDecision.Reject(DownloadRejectionReason.NotCompleteSeriesPack, "Not a complete series or multi-season pack");
                }

                return DownloadSpecDecision.Accept();
            }

            if (parsedEpisodeInfo.IsCompleteSeries)
            {
                _logger.Debug("Complete series release {0} rejected. Only supported by complete series search", subject.Release.Title);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.MultiSeason, "Complete series releases are not supported");
            }

            if (parsedEpisodeInfo.IsMultiSeason)
            {
                _logger.Debug("Multi-season release {0} rejected. Not supported", subject.Release.Title);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.MultiSeason, "Multi-season releases are not supported");
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
