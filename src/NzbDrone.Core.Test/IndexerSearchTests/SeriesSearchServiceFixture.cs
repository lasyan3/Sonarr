using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class SeriesSearchServiceFixture : CoreTest<SeriesSearchService>
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = new Series
                      {
                          Id = 1,
                          Title = "Title",
                          Seasons = new List<Season>()
                      };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<int>()))
                  .Returns(_series);

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<int>(), false, false, true, false))
                  .Returns(Task.FromResult(new List<DownloadDecision>()));

            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                  .Returns(Task.FromResult(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>())));
        }

        [Test]
        public void should_only_include_monitored_seasons()
        {
            _series.Seasons = new List<Season>
                              {
                                  new Season { SeasonNumber = 0, Monitored = false },
                                  new Season { SeasonNumber = 1, Monitored = true }
                              };

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeasonSearch(_series.Id, It.IsAny<int>(), false, true, true, false), Times.Exactly(_series.Seasons.Count(s => s.Monitored)));
        }

        [Test]
        public void should_start_with_lower_seasons_first()
        {
            var seasonOrder = new List<int>();

            _series.Seasons = new List<Season>
                              {
                                  new Season { SeasonNumber = 3, Monitored = true },
                                  new Season { SeasonNumber = 1, Monitored = true },
                                  new Season { SeasonNumber = 2, Monitored = true }
                              };

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<int>(), false, true, true, false))
                  .Returns(Task.FromResult(new List<DownloadDecision>()))
                  .Callback<int, int, bool, bool, bool, bool>((seriesId, seasonNumber, missingOnly, monitoredOnly, userInvokedSearch, interactiveSearch) => seasonOrder.Add(seasonNumber));

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            seasonOrder.First().Should().Be(_series.Seasons.OrderBy(s => s.SeasonNumber).First().SeasonNumber);
        }

        [Test]
        public void should_not_search_for_complete_series_pack_if_series_is_not_ended()
        {
            _series.Status = SeriesStatusType.Continuing;
            _series.Seasons = new List<Season>
                              {
                                  new Season { SeasonNumber = 1, Monitored = true }
                              };

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<int>(), false, true, true, false))
                  .Returns(Task.FromResult(new List<DownloadDecision>()));

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.CompleteSeriesSearch(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_search_by_season_if_no_complete_series_pack_was_grabbed()
        {
            GivenEndedSeries();
            GivenCompleteSeriesPackCovering();

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeasonSearch(_series.Id, It.IsAny<int>(), false, true, true, false), Times.Exactly(2));
        }

        [Test]
        public void should_not_search_seasons_covered_by_complete_series_pack()
        {
            GivenEndedSeries();
            GivenCompleteSeriesPackCovering(1, 2);

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeasonSearch(_series.Id, It.IsAny<int>(), It.IsAny<List<Episode>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeasonSearch(_series.Id, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_only_search_episodes_not_covered_by_complete_series_pack()
        {
            GivenEndedSeries();
            GivenCompleteSeriesPackCovering(1);

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeasonSearch(_series.Id, 2, It.Is<List<Episode>>(e => e.Single().Id == 2), true, true, false), Times.Once());

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeasonSearch(_series.Id, 1, It.IsAny<List<Episode>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        }

        private void GivenEndedSeries()
        {
            _series.Status = SeriesStatusType.Ended;
            _series.Seasons = new List<Season>
                              {
                                  new Season { SeasonNumber = 1, Monitored = true },
                                  new Season { SeasonNumber = 2, Monitored = true }
                              };

            Mocker.GetMock<IEpisodeService>()
                  .Setup(s => s.GetEpisodesBySeason(_series.Id, It.IsAny<int>()))
                  .Returns<int, int>((seriesId, seasonNumber) => new List<Episode> { new Episode { Id = seasonNumber, SeasonNumber = seasonNumber } });

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<int>(), It.IsAny<List<Episode>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                  .Returns(Task.FromResult(new List<DownloadDecision>()));

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<int>(), false, true, true, false))
                  .Returns(Task.FromResult(new List<DownloadDecision>()));
        }

        private void GivenCompleteSeriesPackCovering(params int[] episodeIds)
        {
            var packDecisions = new List<DownloadDecision>();

            if (episodeIds.Any())
            {
                packDecisions.Add(new DownloadDecision(new RemoteEpisode
                {
                    Episodes = episodeIds.Select(id => new Episode { Id = id }).ToList()
                }));
            }

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.CompleteSeriesSearch(_series.Id, true, true, false))
                  .Returns(Task.FromResult(packDecisions));

            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(s => s.ProcessDecisions(packDecisions))
                  .Returns(Task.FromResult(new ProcessedDecisions(packDecisions, new List<DownloadDecision>(), new List<DownloadDecision>())));
        }
    }
}
