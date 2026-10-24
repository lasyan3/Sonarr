using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.IndexerSearch
{
    public class CompleteSeriesSearchService : IExecute<CompleteSeriesSearchCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly Logger _logger;

        public CompleteSeriesSearchService(ISeriesService seriesService,
                                           IEpisodeService episodeService,
                                           ISearchForReleases releaseSearchService,
                                           IProcessDownloadDecisions processDownloadDecisions,
                                           Logger logger)
        {
            _seriesService = seriesService;
            _episodeService = episodeService;
            _releaseSearchService = releaseSearchService;
            _processDownloadDecisions = processDownloadDecisions;
            _logger = logger;
        }

        public void Execute(CompleteSeriesSearchCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            var userInvokedSearch = message.Trigger == CommandTrigger.Manual;

            var decisions = _releaseSearchService.CompleteSeriesSearch(series.Id, true, userInvokedSearch, false).GetAwaiter().GetResult();
            var processDecisions = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();
            var downloadedCount = processDecisions.Grabbed.Count;

            // Episodes grabbed or pending from a pack must not be searched again, the queue may not know about them yet
            var coveredEpisodeIds = processDecisions.Grabbed
                                                    .Concat(processDecisions.Pending)
                                                    .SelectMany(d => d.RemoteEpisode.Episodes)
                                                    .Select(e => e.Id)
                                                    .ToHashSet();

            var missingEpisodes = _episodeService.GetEpisodeBySeries(series.Id)
                                                 .Where(e => e.Monitored &&
                                                             !e.HasFile &&
                                                             e.AirDateUtc.HasValue &&
                                                             e.AirDateUtc.Value.Before(DateTime.UtcNow) &&
                                                             !coveredEpisodeIds.Contains(e.Id))
                                                 .ToList();

            if (missingEpisodes.Any())
            {
                _logger.ProgressInfo("{0} episodes of {1} not covered by a complete series pack, searching by season", missingEpisodes.Count, series.Title);

                foreach (var season in missingEpisodes.GroupBy(e => e.SeasonNumber).OrderBy(g => g.Key))
                {
                    downloadedCount += SearchSeason(series, season.Key, season.ToList(), userInvokedSearch);
                }
            }

            _logger.ProgressInfo("Complete series search completed. {0} reports downloaded.", downloadedCount);
        }

        private int SearchSeason(Series series, int seasonNumber, List<Episode> episodes, bool userInvokedSearch)
        {
            try
            {
                var decisions = _releaseSearchService.SeasonSearch(series.Id, seasonNumber, episodes, true, userInvokedSearch, false).GetAwaiter().GetResult();
                var processDecisions = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();

                return processDecisions.Grabbed.Count;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to search for episodes in season {0} of [{1}]", seasonNumber, series.Title);
            }

            return 0;
        }
    }
}
