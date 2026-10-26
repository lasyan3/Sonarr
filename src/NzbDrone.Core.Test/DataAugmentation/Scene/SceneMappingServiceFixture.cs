using System.Collections.Generic;
using System.Linq;
using System.Net;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DataAugmentation.Scene
{
    [TestFixture]

    public class SceneMappingServiceFixture : CoreTest<SceneMappingService>
    {
        private List<SceneMapping> _fakeMappings;

        private Mock<ISceneMappingProvider> _provider1;
        private Mock<ISceneMappingProvider> _provider2;

        [SetUp]
        public void Setup()
        {
            _fakeMappings = Builder<SceneMapping>.CreateListOfSize(5)
                                                 .All()
                                                 .With(v => v.FilterRegex = null)
                                                 .BuildListOfNew();

            _fakeMappings[0].SearchTerm = "Words";
            _fakeMappings[1].SearchTerm = "That";
            _fakeMappings[2].SearchTerm = "Can";
            _fakeMappings[3].SearchTerm = "Be";
            _fakeMappings[4].SearchTerm = "Cleaned";

            _fakeMappings[0].ParseTerm = "Words";
            _fakeMappings[1].ParseTerm = "That";
            _fakeMappings[2].ParseTerm = "Can";
            _fakeMappings[3].ParseTerm = "Be";
            _fakeMappings[4].ParseTerm = "Cleaned";

            _provider1 = new Mock<ISceneMappingProvider>();
            _provider1.Setup(s => s.GetSceneMappings()).Returns(_fakeMappings);

            _provider2 = new Mock<ISceneMappingProvider>();
            _provider2.Setup(s => s.GetSceneMappings()).Returns(_fakeMappings);

            Mocker.GetMock<ISeriesService>().Setup(c => c.GetAllSeries()).Returns(new List<Series>());
        }

        private void GivenProviders(IEnumerable<Mock<ISceneMappingProvider>> providers)
        {
            Mocker.SetConstant<IEnumerable<ISceneMappingProvider>>(providers.Select(s => s.Object));
        }

        [Test]
        public void should_purge_existing_mapping_and_add_new_ones()
        {
            GivenProviders(new[] { _provider1 });

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(_fakeMappings);

            Subject.Execute(new UpdateSceneMappingCommand());

            AssertMappingUpdated();
        }

        [Test]
        public void should_not_delete_if_fetch_fails()
        {
            GivenProviders(new[] { _provider1 });

            _provider1.Setup(c => c.GetSceneMappings()).Throws(new WebException());

            Subject.Execute(new UpdateSceneMappingCommand());

            AssertNoUpdate();

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_not_delete_if_fetch_returns_empty_list()
        {
            GivenProviders(new[] { _provider1 });

            _provider1.Setup(c => c.GetSceneMappings()).Returns(new List<SceneMapping>());

            Subject.Execute(new UpdateSceneMappingCommand());

            AssertNoUpdate();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_get_mappings_for_all_providers()
        {
            GivenProviders(new[] { _provider1, _provider2 });

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(_fakeMappings);

            Subject.Execute(new UpdateSceneMappingCommand());

            _provider1.Verify(c => c.GetSceneMappings(), Times.Once());
            _provider2.Verify(c => c.GetSceneMappings(), Times.Once());
        }

        [Test]
        public void should_refresh_cache_if_cache_is_empty_when_looking_for_tvdb_id()
        {
            Subject.FindTvdbId("title", null, -1);

            Mocker.GetMock<ISceneMappingRepository>()
                  .Verify(v => v.All(), Times.Once());
        }

        [Test]
        public void should_not_refresh_cache_if_cache_is_not_empty_when_looking_for_tvdb_id()
        {
            GivenProviders(new[] { _provider1 });

            Mocker.GetMock<ISceneMappingRepository>()
                  .Setup(s => s.All())
                  .Returns(Builder<SceneMapping>.CreateListOfSize(1).Build());

            Subject.Execute(new UpdateSceneMappingCommand());

            Mocker.GetMock<ISceneMappingRepository>()
                  .Verify(v => v.All(), Times.Once());

            Subject.FindTvdbId("title", null, -1);

            Mocker.GetMock<ISceneMappingRepository>()
                  .Verify(v => v.All(), Times.Once());
        }

        [Test]
        public void should_not_add_mapping_with_blank_title()
        {
            GivenProviders(new[] { _provider1 });

            var fakeMappings = Builder<SceneMapping>.CreateListOfSize(2)
                                                    .TheLast(1)
                                                    .With(m => m.Title = null)
                                                    .Build()
                                                    .ToList();

            _provider1.Setup(s => s.GetSceneMappings()).Returns(fakeMappings);

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(_fakeMappings);

            Subject.Execute(new UpdateSceneMappingCommand());

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.InsertMany(It.Is<IList<SceneMapping>>(m => !m.Any(s => s.Title.IsNullOrWhiteSpace()))), Times.Once());
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_add_mapping_with_blank_search_title()
        {
            GivenProviders(new[] { _provider1 });

            var fakeMappings = Builder<SceneMapping>.CreateListOfSize(2)
                                                    .TheLast(1)
                                                    .With(m => m.SearchTerm = null)
                                                    .Build()
                                                    .ToList();

            _provider1.Setup(s => s.GetSceneMappings()).Returns(fakeMappings);

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(_fakeMappings);

            Subject.Execute(new UpdateSceneMappingCommand());

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.InsertMany(It.Is<IList<SceneMapping>>(m => !m.Any(s => s.SearchTerm.IsNullOrWhiteSpace()))), Times.Once());
            ExceptionVerification.ExpectedWarns(1);
        }

        [TestCase("Working!!", "Working!!", 1)]
        [TestCase("Working`!!", "Working`!!", 2)]
        [TestCase("Working!!!", "Working!!!", 3)]
        [TestCase("Working!!!!", "Working!!!", 3)]
        [TestCase("Working !!", "Working!!", 1)]
        public void should_return_single_match(string parseTitle, string title, int expectedSeasonNumber)
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Working!!", ParseTerm = "working", SearchTerm = "Working!!", TvdbId = 100, SceneSeasonNumber = 1 },
                new SceneMapping { Title = "Working`!!", ParseTerm = "working", SearchTerm = "Working`!!", TvdbId = 100, SceneSeasonNumber = 2 },
                new SceneMapping { Title = "Working!!!", ParseTerm = "working", SearchTerm = "Working!!!", TvdbId = 100, SceneSeasonNumber = 3 },
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            var tvdbId = Subject.FindTvdbId(parseTitle, null, -1);
            var seasonNumber = Subject.GetSceneSeasonNumber(parseTitle, null);

            tvdbId.Should().Be(100);
            seasonNumber.Should().Be(expectedSeasonNumber);
        }

        [Test]
        public void should_return_alternate_title_for_global_season()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu 1", ParseTerm = "fudanshikoukouseikatsu1", SearchTerm = "Fudanshi Koukou Seikatsu 1", TvdbId = 100, SeasonNumber = null, SceneSeasonNumber = null },
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu 2", ParseTerm = "fudanshikoukouseikatsu2", SearchTerm = "Fudanshi Koukou Seikatsu 2", TvdbId = 100, SeasonNumber = -1, SceneSeasonNumber = null },
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu 3", ParseTerm = "fudanshikoukouseikatsu3", SearchTerm = "Fudanshi Koukou Seikatsu 3", TvdbId = 100, SeasonNumber = null, SceneSeasonNumber = -1 },
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu 4", ParseTerm = "fudanshikoukouseikatsu4", SearchTerm = "Fudanshi Koukou Seikatsu 4", TvdbId = 100, SeasonNumber = -1, SceneSeasonNumber = -1 },
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            var names = Subject.GetSceneNames(100, new List<int> { 10 }, new List<int> { 10 });
            names.Should().HaveCount(4);
        }

        [Test]
        public void should_return_alternate_title_for_season()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu", ParseTerm = "fudanshikoukouseikatsu", SearchTerm = "Fudanshi Koukou Seikatsu", TvdbId = 100, SeasonNumber = 1, SceneSeasonNumber = null }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            var names = Subject.GetSceneNames(100, new List<int> { 1 }, new List<int> { 10 });
            names.Should().HaveCount(1);
        }

        [Test]
        public void should_not_return_alternate_title_for_season()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu", ParseTerm = "fudanshikoukouseikatsu", SearchTerm = "Fudanshi Koukou Seikatsu", TvdbId = 100, SeasonNumber = 1, SceneSeasonNumber = null }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            var names = Subject.GetSceneNames(100, new List<int> { 2 }, new List<int> { 10 });
            names.Should().BeEmpty();
        }

        [Test]
        public void should_return_alternate_title_for_sceneseason()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu", ParseTerm = "fudanshikoukouseikatsu", SearchTerm = "Fudanshi Koukou Seikatsu", TvdbId = 100, SeasonNumber = null, SceneSeasonNumber = 1 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            var names = Subject.GetSceneNames(100, new List<int> { 10 }, new List<int> { 1 });
            names.Should().HaveCount(1);
        }

        [Test]
        public void should_not_return_alternate_title_for_sceneseason()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu", ParseTerm = "fudanshikoukouseikatsu", SearchTerm = "Fudanshi Koukou Seikatsu", TvdbId = 100, SeasonNumber = null, SceneSeasonNumber = 1 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            var names = Subject.GetSceneNames(100, new List<int> { 10 }, new List<int> { 2 });
            names.Should().BeEmpty();
        }

        [Test]
        public void should_return_alternate_title_for_fairy_tail()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Fairy Tail S2", ParseTerm = "fairytails2", SearchTerm = "Fairy Tail S2", TvdbId = 100, SeasonNumber = null, SceneSeasonNumber = 2 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            Subject.GetSceneNames(100, new List<int> { 4 }, new List<int> { 20 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 5 }, new List<int> { 20 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 6 }, new List<int> { 20 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 7 }, new List<int> { 20 }).Should().BeEmpty();

            Subject.GetSceneNames(100, new List<int> { 20 }, new List<int> { 1 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 20 }, new List<int> { 2 }).Should().NotBeEmpty();
            Subject.GetSceneNames(100, new List<int> { 20 }, new List<int> { 3 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 20 }, new List<int> { 4 }).Should().BeEmpty();
        }

        [Test]
        public void should_return_alternate_title_for_fudanshi()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Fudanshi Koukou Seikatsu", ParseTerm = "fudanshikoukouseikatsu", SearchTerm = "Fudanshi Koukou Seikatsu", TvdbId = 100, SeasonNumber = null, SceneSeasonNumber = 1 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            Subject.GetSceneNames(100, new List<int> { 1 }, new List<int> { 20 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 2 }, new List<int> { 20 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 3 }, new List<int> { 20 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 4 }, new List<int> { 20 }).Should().BeEmpty();

            Subject.GetSceneNames(100, new List<int> { 1 }, new List<int> { 1 }).Should().NotBeEmpty();
            Subject.GetSceneNames(100, new List<int> { 2 }, new List<int> { 2 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 3 }, new List<int> { 3 }).Should().BeEmpty();
            Subject.GetSceneNames(100, new List<int> { 4 }, new List<int> { 4 }).Should().BeEmpty();
        }

        [Test]
        public void should_filter_by_regex()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 100 },
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 101, FilterRegex = "-Viva$" }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            Subject.FindTvdbId("Amareto", "Amareto.S01E01.720p.WEB-DL-Viva", -1).Should().Be(101);
            Subject.FindTvdbId("Amareto", "Amareto.S01E01.720p.WEB-DL-DMO", -1).Should().Be(100);
        }

        [Test]
        public void should_throw_if_multiple_mappings()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 100 },
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 101 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            Assert.Throws<InvalidSceneMappingException>(() => Subject.FindTvdbId("Amareto", "Amareto.S01E01.720p.WEB-DL-Viva", -1));
        }

        [Test]
        public void should_not_throw_if_multiple_mappings_with_same_tvdbid()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 100 },
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 100 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            Subject.FindTvdbId("Amareto", "Amareto.S01E01.720p.WEB-DL-Viva", -1).Should().Be(100);
        }

        [Test]
        public void should_pick_best_season()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", SceneSeasonNumber = 2, SeasonNumber = 3, TvdbId = 100 },
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", SceneSeasonNumber = 3, SeasonNumber = 3, TvdbId = 101 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            Subject.FindTvdbId("Amareto", "Amareto.S01E01.720p.WEB-DL-Viva", 4).Should().Be(101);
        }

        [Test]
        public void should_not_clear_custom_mappings_when_updating()
        {
            GivenProviders(new[] { _provider1, _provider2 });

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(_fakeMappings);

            Subject.Execute(new UpdateSceneMappingCommand());

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Clear(SceneMappingService.CustomMappingType), Times.Never());
        }

        [Test]
        public void should_not_return_excluded_mapping()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 100 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);
            GivenExclusions(new SceneMappingExclusion { TvdbId = 100, ParseTerm = "amareto" });

            Subject.FindTvdbId("Amareto", null, -1).Should().BeNull();
            Subject.GetSceneNames(100, new List<int> { 1 }, new List<int> { 1 }).Should().BeEmpty();
            Subject.FindByTvdbId(100).Should().BeEmpty();
        }

        [Test]
        public void should_only_exclude_mapping_for_excluded_series()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 100 },
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 101 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);
            GivenExclusions(new SceneMappingExclusion { TvdbId = 100, ParseTerm = "amareto" });

            Subject.FindTvdbId("Amareto", null, -1).Should().Be(101);
        }

        [Test]
        public void should_add_custom_mapping()
        {
            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(new List<SceneMapping>());

            var mapping = Subject.AddCustomMapping(new SceneMapping { Title = " Amareto ", TvdbId = 100 });

            mapping.Title.Should().Be("Amareto");
            mapping.ParseTerm.Should().Be("amareto");
            mapping.SearchTerm.Should().Be("Amareto");
            mapping.Type.Should().Be(SceneMappingService.CustomMappingType);

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Insert(mapping), Times.Once());
            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.All(), Times.Exactly(2));
        }

        [Test]
        public void should_not_add_custom_mapping_with_empty_title()
        {
            Assert.Throws<NzbDroneClientException>(() => Subject.AddCustomMapping(new SceneMapping { Title = " ", TvdbId = 100 }));

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Insert(It.IsAny<SceneMapping>()), Times.Never());
        }

        [Test]
        public void should_not_add_custom_mapping_used_by_another_series()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 101 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            Assert.Throws<NzbDroneClientException>(() => Subject.AddCustomMapping(new SceneMapping { Title = "Amareto", TvdbId = 100 }));

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Insert(It.IsAny<SceneMapping>()), Times.Never());
        }

        [Test]
        public void should_add_custom_mapping_used_by_another_series_if_excluded()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 101 }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);
            GivenExclusions(new SceneMappingExclusion { TvdbId = 101, ParseTerm = "amareto" });

            Subject.AddCustomMapping(new SceneMapping { Title = "Amareto", TvdbId = 100 });

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Insert(It.IsAny<SceneMapping>()), Times.Once());
        }

        [Test]
        public void should_not_add_custom_mapping_matching_another_series_title()
        {
            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(new List<SceneMapping>());
            Mocker.GetMock<ISeriesService>().Setup(c => c.FindByTitle("Amareto")).Returns(new Series { TvdbId = 101 });

            Assert.Throws<NzbDroneClientException>(() => Subject.AddCustomMapping(new SceneMapping { Title = "Amareto", TvdbId = 100 }));

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Insert(It.IsAny<SceneMapping>()), Times.Never());
        }

        [Test]
        public void should_not_add_duplicate_custom_mapping()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 100, Type = SceneMappingService.CustomMappingType }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);

            Assert.Throws<NzbDroneClientException>(() => Subject.AddCustomMapping(new SceneMapping { Title = "Amareto", TvdbId = 100 }));
        }

        [Test]
        public void should_delete_custom_mapping()
        {
            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.Get(1)).Returns(new SceneMapping { Id = 1, Title = "Amareto", TvdbId = 100, Type = SceneMappingService.CustomMappingType });

            Subject.DeleteCustomMapping(1);

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Delete(1), Times.Once());
        }

        [Test]
        public void should_not_delete_mapping_from_provider()
        {
            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.Get(1)).Returns(new SceneMapping { Id = 1, Title = "Amareto", TvdbId = 100, Type = "ServicesProvider" });

            Assert.Throws<NzbDroneClientException>(() => Subject.DeleteCustomMapping(1));

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Delete(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_exclude_mapping()
        {
            GivenExclusions();

            Subject.Exclude(100, "Amareto");

            Mocker.GetMock<ISceneMappingExclusionRepository>().Verify(c => c.Insert(It.Is<SceneMappingExclusion>(e => e.TvdbId == 100 && e.ParseTerm == "amareto")), Times.Once());
        }

        [Test]
        public void should_not_exclude_mapping_twice()
        {
            GivenExclusions(new SceneMappingExclusion { TvdbId = 100, ParseTerm = "amareto" });

            Subject.Exclude(100, "Amareto");

            Mocker.GetMock<ISceneMappingExclusionRepository>().Verify(c => c.Insert(It.IsAny<SceneMappingExclusion>()), Times.Never());
        }

        [Test]
        public void should_remove_exclusion()
        {
            GivenExclusions(new SceneMappingExclusion { TvdbId = 100, ParseTerm = "amareto" });

            Subject.RemoveExclusion(100, "Amareto");

            Mocker.GetMock<ISceneMappingExclusionRepository>().Verify(c => c.DeleteMany(It.Is<List<SceneMappingExclusion>>(l => l.Count == 1)), Times.Once());
        }

        [Test]
        public void should_return_all_mappings_for_series_not_ignoring_alternate_titles()
        {
            GivenProviderAndCustomMappings();
            GivenSeriesIgnoringAlternateTitles(false);

            Subject.FindTvdbId("Amareto", null, -1).Should().Be(100);
            Subject.FindTvdbId("Amaretto Custom", null, -1).Should().Be(100);
            Subject.FindByTvdbId(100).Should().HaveCount(2);
        }

        [Test]
        public void should_only_return_custom_mappings_for_series_ignoring_alternate_titles()
        {
            GivenProviderAndCustomMappings();
            GivenSeriesIgnoringAlternateTitles(true);

            Subject.FindTvdbId("Amareto", null, -1).Should().BeNull();
            Subject.FindTvdbId("Amaretto Custom", null, -1).Should().Be(100);
            Subject.GetSceneNames(100, new List<int> { 1 }, new List<int> { 1 }).Should().BeEquivalentTo("Amaretto Custom");
            Subject.FindByTvdbId(100).Should().ContainSingle(m => m.Type == SceneMappingService.CustomMappingType);
        }

        [Test]
        public void should_refresh_mappings_when_series_starts_ignoring_alternate_titles()
        {
            GivenProviderAndCustomMappings();
            GivenSeriesIgnoringAlternateTitles(false);

            Subject.FindByTvdbId(100).Should().HaveCount(2);

            GivenSeriesIgnoringAlternateTitles(true);

            Subject.Handle(new SeriesEditedEvent(new Series { TvdbId = 100, IgnoreAlternateTitles = true },
                                                 new Series { TvdbId = 100, IgnoreAlternateTitles = false }));

            Subject.FindByTvdbId(100).Should().HaveCount(1);
            Subject.FindTvdbId("Amareto", null, -1).Should().BeNull();
        }

        [Test]
        public void should_not_refresh_mappings_when_series_edit_does_not_change_ignore_alternate_titles()
        {
            Subject.Handle(new SeriesEditedEvent(new Series { TvdbId = 100, IgnoreAlternateTitles = true },
                                                 new Series { TvdbId = 100, IgnoreAlternateTitles = true }));

            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.All(), Times.Never());
        }

        private void GivenProviderAndCustomMappings()
        {
            var mappings = new List<SceneMapping>
            {
                new SceneMapping { Title = "Amareto", ParseTerm = "amareto", SearchTerm = "Amareto", TvdbId = 100, Type = "ServicesProvider" },
                new SceneMapping { Title = "Amaretto Custom", ParseTerm = "amarettocustom", SearchTerm = "Amaretto Custom", TvdbId = 100, Type = SceneMappingService.CustomMappingType }
            };

            Mocker.GetMock<ISceneMappingRepository>().Setup(c => c.All()).Returns(mappings);
            GivenExclusions();
        }

        private void GivenSeriesIgnoringAlternateTitles(bool ignoreAlternateTitles)
        {
            Mocker.GetMock<ISeriesService>().Setup(c => c.GetAllSeries())
                  .Returns(new List<Series> { new Series { TvdbId = 100, IgnoreAlternateTitles = ignoreAlternateTitles } });
        }

        private void GivenExclusions(params SceneMappingExclusion[] exclusions)
        {
            Mocker.GetMock<ISceneMappingExclusionRepository>().Setup(c => c.All()).Returns(exclusions);
            Mocker.GetMock<ISceneMappingExclusionRepository>().Setup(c => c.FindByTvdbId(It.IsAny<int>()))
                  .Returns<int>(tvdbId => exclusions.Where(e => e.TvdbId == tvdbId).ToList());
        }

        private void AssertNoUpdate()
        {
            _provider1.Verify(c => c.GetSceneMappings(), Times.Once());
            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Clear(It.IsAny<string>()), Times.Never());
            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.InsertMany(_fakeMappings), Times.Never());
        }

        private void AssertMappingUpdated()
        {
            _provider1.Verify(c => c.GetSceneMappings(), Times.Once());
            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.Clear(It.IsAny<string>()), Times.Once());
            Mocker.GetMock<ISceneMappingRepository>().Verify(c => c.InsertMany(_fakeMappings), Times.Once());

            foreach (var sceneMapping in _fakeMappings)
            {
                Subject.GetSceneNames(sceneMapping.TvdbId, _fakeMappings.Select(m => m.SeasonNumber.Value).Distinct().ToList(), new List<int>()).Should().Contain(sceneMapping.SearchTerm);
                Subject.FindTvdbId(sceneMapping.ParseTerm, null, sceneMapping.SceneSeasonNumber ?? -1).Should().Be(sceneMapping.TvdbId);
            }
        }
    }
}
