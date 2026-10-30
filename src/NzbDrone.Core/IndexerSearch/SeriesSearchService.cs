using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.IndexerSearch
{
    public class SeriesSearchService : IExecute<SeriesSearchCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly Logger _logger;

        public SeriesSearchService(ISeriesService seriesService,
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

        public void Execute(SeriesSearchCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            var downloadedCount = 0;
            var userInvokedSearch = message.Trigger == CommandTrigger.Manual;
            var coveredEpisodeIds = new HashSet<int>();

            // For an ended series search for a complete series pack first, then only search for what it doesn't cover
            if (series.Status == SeriesStatusType.Ended)
            {
                var decisions = _releaseSearchService.CompleteSeriesSearch(series.Id, true, userInvokedSearch, false).GetAwaiter().GetResult();
                var processDecisions = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();
                downloadedCount += processDecisions.Grabbed.Count;

                coveredEpisodeIds = processDecisions.Grabbed
                                                    .Concat(processDecisions.Pending)
                                                    .SelectMany(d => d.RemoteEpisode.Episodes)
                                                    .Select(e => e.Id)
                                                    .ToHashSet();
            }

            if (series.Seasons.None(s => s.Monitored))
            {
                _logger.Debug("No seasons of {0} are monitored, searching for all monitored episodes", series.Title);

                var episodes = _episodeService.GetEpisodeBySeries(series.Id)
                    .Where(e => e.Monitored &&
                                !e.HasFile &&
                                e.AirDateUtc.HasValue &&
                                e.AirDateUtc.Value.Before(DateTime.UtcNow) &&
                                !coveredEpisodeIds.Contains(e.Id))
                    .ToList();

                foreach (var episode in episodes)
                {
                    var decisions = _releaseSearchService.EpisodeSearch(episode, userInvokedSearch, false).GetAwaiter().GetResult();
                    var processDecisions = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();
                    downloadedCount += processDecisions.Grabbed.Count;
                }
            }
            else
            {
                foreach (var season in series.Seasons.OrderBy(s => s.SeasonNumber))
                {
                    if (!season.Monitored)
                    {
                        _logger.Debug("Season {0} of {1} is not monitored, skipping search", season.SeasonNumber, series.Title);
                        continue;
                    }

                    List<DownloadDecision> decisions;

                    if (coveredEpisodeIds.Any())
                    {
                        var episodes = _episodeService.GetEpisodesBySeason(series.Id, season.SeasonNumber)
                            .Where(e => !coveredEpisodeIds.Contains(e.Id))
                            .ToList();

                        if (episodes.Empty())
                        {
                            _logger.Debug("Season {0} of {1} is covered by a complete series pack, skipping search", season.SeasonNumber, series.Title);
                            continue;
                        }

                        decisions = _releaseSearchService.SeasonSearch(message.SeriesId, season.SeasonNumber, episodes, true, userInvokedSearch, false).GetAwaiter().GetResult();
                    }
                    else
                    {
                        decisions = _releaseSearchService.SeasonSearch(message.SeriesId, season.SeasonNumber, false, true, userInvokedSearch, false).GetAwaiter().GetResult();
                    }

                    var processDecisions = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();
                    downloadedCount += processDecisions.Grabbed.Count;
                }
            }

            _logger.ProgressInfo("Series search completed. {0} reports downloaded.", downloadedCount);
        }
    }
}
