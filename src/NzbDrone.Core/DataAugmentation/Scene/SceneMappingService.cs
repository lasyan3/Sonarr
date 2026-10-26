using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.DataAugmentation.Scene
{
    public interface ISceneMappingService
    {
        List<string> GetSceneNames(int tvdbId, List<int> seasonNumbers, List<int> sceneSeasonNumbers);
        int? FindTvdbId(string sceneTitle, string releaseTitle, int sceneSeasonNumber);
        List<SceneMapping> FindByTvdbId(int tvdbId);
        SceneMapping FindSceneMapping(string sceneTitle, string releaseTitle, int sceneSeasonNumber);
        int? GetSceneSeasonNumber(string seriesTitle, string releaseTitle);
        List<SceneMapping> GetAllByTvdbId(int tvdbId);
        List<SceneMappingExclusion> GetExclusions(int tvdbId);
        SceneMapping AddCustomMapping(SceneMapping mapping);
        void DeleteCustomMapping(int id);
        void Exclude(int tvdbId, string title);
        void RemoveExclusion(int tvdbId, string title);
    }

    public class SceneMappingService : ISceneMappingService,
                                       IHandle<SeriesRefreshStartingEvent>,
                                       IHandle<SeriesAddedEvent>,
                                       IHandle<SeriesImportedEvent>,
                                       IHandle<SeriesEditedEvent>,
                                       IHandle<SeriesBulkEditedEvent>,
                                       IExecute<UpdateSceneMappingCommand>
    {
        public const string CustomMappingType = "Custom";

        private readonly ISceneMappingRepository _repository;
        private readonly ISceneMappingExclusionRepository _exclusionRepository;
        private readonly ISeriesService _seriesService;
        private readonly IEnumerable<ISceneMappingProvider> _sceneMappingProviders;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;
        private readonly ICachedDictionary<List<SceneMapping>> _getTvdbIdCache;
        private readonly ICachedDictionary<List<SceneMapping>> _findByTvdbIdCache;
        private bool _updatedAfterStartup;

        public SceneMappingService(ISceneMappingRepository repository,
                                   ISceneMappingExclusionRepository exclusionRepository,
                                   ISeriesService seriesService,
                                   ICacheManager cacheManager,
                                   IEnumerable<ISceneMappingProvider> sceneMappingProviders,
                                   IEventAggregator eventAggregator,
                                   Logger logger)
        {
            _repository = repository;
            _exclusionRepository = exclusionRepository;
            _seriesService = seriesService;
            _sceneMappingProviders = sceneMappingProviders;
            _eventAggregator = eventAggregator;
            _logger = logger;

            _getTvdbIdCache = cacheManager.GetCacheDictionary<List<SceneMapping>>(GetType(), "tvdb_id");
            _findByTvdbIdCache = cacheManager.GetCacheDictionary<List<SceneMapping>>(GetType(), "find_tvdb_id");
        }

        public List<string> GetSceneNames(int tvdbId, List<int> seasonNumbers, List<int> sceneSeasonNumbers)
        {
            var mappings = FindByTvdbId(tvdbId);

            if (mappings == null)
            {
                return new List<string>();
            }

            var names = mappings.Where(n => seasonNumbers.Contains(n.SeasonNumber ?? -1) ||
                                            sceneSeasonNumbers.Contains(n.SceneSeasonNumber ?? -1) ||
                                            ((n.SeasonNumber ?? -1) == -1 && (n.SceneSeasonNumber ?? -1) == -1 && n.SceneOrigin != "tvdb"))
                                .Where(n => IsEnglish(n.SearchTerm))
                                .Select(n => n.SearchTerm)
                                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                                .ToList();

            return names;
        }

        public int? FindTvdbId(string seriesTitle, string releaseTitle, int sceneSeasonNumber)
        {
            return FindSceneMapping(seriesTitle, releaseTitle, sceneSeasonNumber)?.TvdbId;
        }

        public List<SceneMapping> FindByTvdbId(int tvdbId)
        {
            if (_findByTvdbIdCache.Count == 0)
            {
                RefreshCache();
            }

            var mappings = _findByTvdbIdCache.Find(tvdbId.ToString());

            if (mappings == null)
            {
                return new List<SceneMapping>();
            }

            return mappings;
        }

        public SceneMapping FindSceneMapping(string seriesTitle, string releaseTitle, int sceneSeasonNumber)
        {
            if (seriesTitle.IsNullOrWhiteSpace())
            {
                return null;
            }

            var mappings = FindMappings(seriesTitle, releaseTitle);

            if (mappings == null)
            {
                return null;
            }

            mappings = FilterSceneMappings(mappings, sceneSeasonNumber);

            var distinctMappings = mappings.DistinctBy(v => v.TvdbId).ToList();

            if (distinctMappings.Count == 0)
            {
                return null;
            }

            if (distinctMappings.Count == 1)
            {
                var mapping = distinctMappings.First();
                _logger.Debug("Found scene mapping for: {0}. TVDB ID for mapping: {1}", seriesTitle, mapping.TvdbId);
                return distinctMappings.First();
            }

            throw new InvalidSceneMappingException(mappings, releaseTitle);
        }

        public int? GetSceneSeasonNumber(string seriesTitle, string releaseTitle)
        {
            return FindSceneMapping(seriesTitle, releaseTitle, -1)?.SceneSeasonNumber;
        }

        public List<SceneMapping> GetAllByTvdbId(int tvdbId)
        {
            return _repository.FindByTvdbid(tvdbId);
        }

        public List<SceneMappingExclusion> GetExclusions(int tvdbId)
        {
            return _exclusionRepository.FindByTvdbId(tvdbId);
        }

        public SceneMapping AddCustomMapping(SceneMapping mapping)
        {
            if (mapping.Title.IsNullOrWhiteSpace())
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "Alternate title cannot be empty");
            }

            mapping.Title = mapping.Title.Trim();
            mapping.ParseTerm = mapping.Title.CleanSeriesTitle();
            mapping.SearchTerm = mapping.SearchTerm.IsNullOrWhiteSpace() ? mapping.Title : mapping.SearchTerm.Trim();
            mapping.Type = CustomMappingType;

            var isActive = GetActiveMappingFilter();
            var existing = _repository.All()
                                      .Where(v => v.ParseTerm == mapping.ParseTerm && isActive(v))
                                      .ToList();

            if (existing.Any(v => v.TvdbId != mapping.TvdbId))
            {
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Alternate title '{0}' is already used by another series", mapping.Title);
            }

            if (existing.Any(v => v.Type == CustomMappingType && v.SeasonNumber == mapping.SeasonNumber && v.SceneSeasonNumber == mapping.SceneSeasonNumber))
            {
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Alternate title '{0}' already exists", mapping.Title);
            }

            var series = _seriesService.FindByTitle(mapping.Title);

            if (series != null && series.TvdbId != mapping.TvdbId)
            {
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Alternate title '{0}' matches the title of another series", mapping.Title);
            }

            _repository.Insert(mapping);
            _logger.Debug("Added custom alternate title '{0}' for TVDB ID {1}", mapping.Title, mapping.TvdbId);

            OnMappingsChanged();

            return mapping;
        }

        public void DeleteCustomMapping(int id)
        {
            var mapping = _repository.Get(id);

            if (mapping.Type != CustomMappingType)
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "Only custom alternate titles can be deleted, exclude it instead");
            }

            _repository.Delete(id);
            _logger.Debug("Deleted custom alternate title '{0}' for TVDB ID {1}", mapping.Title, mapping.TvdbId);

            OnMappingsChanged();
        }

        public void Exclude(int tvdbId, string title)
        {
            var parseTerm = title.CleanSeriesTitle();

            if (_exclusionRepository.FindByTvdbId(tvdbId).Any(v => v.ParseTerm == parseTerm))
            {
                return;
            }

            _exclusionRepository.Insert(new SceneMappingExclusion { TvdbId = tvdbId, ParseTerm = parseTerm });
            _logger.Debug("Excluded alternate title '{0}' for TVDB ID {1}", title, tvdbId);

            OnMappingsChanged();
        }

        public void RemoveExclusion(int tvdbId, string title)
        {
            var parseTerm = title.CleanSeriesTitle();
            var exclusions = _exclusionRepository.FindByTvdbId(tvdbId).Where(v => v.ParseTerm == parseTerm).ToList();

            if (exclusions.Empty())
            {
                return;
            }

            _exclusionRepository.DeleteMany(exclusions);
            _logger.Debug("Removed exclusion of alternate title '{0}' for TVDB ID {1}", title, tvdbId);

            OnMappingsChanged();
        }

        private void UpdateMappings()
        {
            _logger.Info("Updating Scene mappings");

            _updatedAfterStartup = true;

            foreach (var sceneMappingProvider in _sceneMappingProviders)
            {
                try
                {
                    var mappings = sceneMappingProvider.GetSceneMappings();

                    if (mappings.Any())
                    {
                        _repository.Clear(sceneMappingProvider.GetType().Name);

                        mappings.RemoveAll(sceneMapping =>
                        {
                            if (sceneMapping.Title.IsNullOrWhiteSpace() ||
                                sceneMapping.SearchTerm.IsNullOrWhiteSpace())
                            {
                                _logger.Warn("Invalid scene mapping found for: {0}, skipping", sceneMapping.TvdbId);
                                return true;
                            }

                            return false;
                        });

                        foreach (var sceneMapping in mappings)
                        {
                            sceneMapping.ParseTerm = sceneMapping.Title.CleanSeriesTitle();
                            sceneMapping.Type = sceneMappingProvider.GetType().Name;
                        }

                        _repository.InsertMany(mappings.ToList());
                    }
                    else
                    {
                        _logger.Warn("Received empty list of mapping. will not update");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to Update Scene Mappings");
                }
            }

            RefreshCache();

            _eventAggregator.PublishEvent(new SceneMappingsUpdatedEvent());
        }

        private List<SceneMapping> FindMappings(string seriesTitle, string releaseTitle)
        {
            if (_getTvdbIdCache.Count == 0)
            {
                RefreshCache();
            }

            var candidates = _getTvdbIdCache.Find(seriesTitle.CleanSeriesTitle());

            if (candidates == null)
            {
                return null;
            }

            candidates = FilterSceneMappings(candidates, releaseTitle);

            if (candidates.Count <= 1)
            {
                return candidates;
            }

            var exactMatch = candidates.OrderByDescending(v => v.SeasonNumber)
                                       .Where(v => v.Title == seriesTitle)
                                       .ToList();

            if (exactMatch.Any())
            {
                return exactMatch;
            }

            var closestMatch = candidates.OrderBy(v => seriesTitle.LevenshteinDistance(v.Title, 10, 1, 10))
                                         .ThenByDescending(v => v.SeasonNumber)
                                         .First();

            return candidates.Where(v => v.Title == closestMatch.Title).ToList();
        }

        private void RefreshCache()
        {
            var isActive = GetActiveMappingFilter();
            var mappings = _repository.All()
                                      .Where(isActive)
                                      .ToList();

            _getTvdbIdCache.Update(mappings.GroupBy(v => v.ParseTerm).ToDictionary(v => v.Key, v => v.ToList()));
            _findByTvdbIdCache.Update(mappings.GroupBy(v => v.TvdbId).ToDictionary(v => v.Key.ToString(), v => v.ToList()));
        }

        private void OnMappingsChanged()
        {
            RefreshCache();

            _eventAggregator.PublishEvent(new SceneMappingsUpdatedEvent());
        }

        // Excluded titles and, for series ignoring alternate titles, all non custom titles are neither used to search nor to parse releases
        private Func<SceneMapping, bool> GetActiveMappingFilter()
        {
            var excludedParseTerms = _exclusionRepository.All().Select(v => GetExclusionKey(v.TvdbId, v.ParseTerm)).ToHashSet();
            var ignoredTvdbIds = _seriesService.GetAllSeries().Where(v => v.IgnoreAlternateTitles).Select(v => v.TvdbId).ToHashSet();

            return v => !excludedParseTerms.Contains(GetExclusionKey(v.TvdbId, v.ParseTerm)) &&
                        (v.Type == CustomMappingType || !ignoredTvdbIds.Contains(v.TvdbId));
        }

        private static string GetExclusionKey(int tvdbId, string parseTerm)
        {
            return $"{tvdbId}:{parseTerm}";
        }

        private List<SceneMapping> FilterSceneMappings(List<SceneMapping> candidates, string releaseTitle)
        {
            var filteredCandidates = candidates.Where(v => v.FilterRegex.IsNotNullOrWhiteSpace()).ToList();
            var normalCandidates = candidates.Except(filteredCandidates).ToList();

            if (releaseTitle.IsNullOrWhiteSpace())
            {
                return normalCandidates;
            }

            var simpleTitle = Parser.Parser.SimplifyTitle(releaseTitle);

            filteredCandidates = filteredCandidates.Where(v => Regex.IsMatch(simpleTitle, v.FilterRegex)).ToList();

            if (filteredCandidates.Any())
            {
                return filteredCandidates;
            }

            return normalCandidates;
        }

        private List<SceneMapping> FilterSceneMappings(List<SceneMapping> candidates, int sceneSeasonNumber)
        {
            var filteredCandidates = candidates.Where(v => (v.SceneSeasonNumber ?? -1) != -1 && (v.SeasonNumber ?? -1) != -1).ToList();
            var normalCandidates = candidates.Except(filteredCandidates).ToList();

            if (sceneSeasonNumber == -1)
            {
                return normalCandidates;
            }

            if (filteredCandidates.Any())
            {
                filteredCandidates = filteredCandidates.Where(v => v.SceneSeasonNumber <= sceneSeasonNumber)
                                                       .GroupBy(v => v.Title)
                                                       .Select(d => d.OrderByDescending(v => v.SceneSeasonNumber)
                                                                     .ThenByDescending(v => v.SeasonNumber)
                                                                     .First())
                                                       .ToList();

                return filteredCandidates;
            }

            return normalCandidates;
        }

        private bool IsEnglish(string title)
        {
            return title.All(c => c <= 255);
        }

        public void Handle(SeriesRefreshStartingEvent message)
        {
            if (message.ManualTrigger && (_findByTvdbIdCache.IsExpired(TimeSpan.FromMinutes(1)) || !_updatedAfterStartup))
            {
                UpdateMappings();
            }
        }

        public void Handle(SeriesAddedEvent message)
        {
            if (!_updatedAfterStartup)
            {
                UpdateMappings();
            }
            else if (message.Series.IgnoreAlternateTitles)
            {
                OnMappingsChanged();
            }
        }

        public void Handle(SeriesImportedEvent message)
        {
            if (!_updatedAfterStartup)
            {
                UpdateMappings();
            }
        }

        public void Handle(SeriesEditedEvent message)
        {
            if (message.Series.IgnoreAlternateTitles != message.OldSeries.IgnoreAlternateTitles)
            {
                OnMappingsChanged();
            }
        }

        public void Handle(SeriesBulkEditedEvent message)
        {
            OnMappingsChanged();
        }

        public void Execute(UpdateSceneMappingCommand message)
        {
            UpdateMappings();
        }
    }
}
