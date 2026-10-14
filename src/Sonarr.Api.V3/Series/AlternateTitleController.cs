using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;
using Sonarr.Http;
using Sonarr.Http.REST.Attributes;

namespace Sonarr.Api.V3.Series
{
    [V3ApiController]
    public class AlternateTitleController : Controller
    {
        private readonly ISceneMappingService _sceneMappingService;
        private readonly ISeriesService _seriesService;

        public AlternateTitleController(ISceneMappingService sceneMappingService, ISeriesService seriesService)
        {
            _sceneMappingService = sceneMappingService;
            _seriesService = seriesService;
        }

        [HttpGet]
        [Produces("application/json")]
        public List<AlternateTitleResource> GetAlternateTitles([FromQuery] int seriesId)
        {
            var series = _seriesService.GetSeries(seriesId);
            var excludedParseTerms = _sceneMappingService.GetExclusions(series.TvdbId).Select(e => e.ParseTerm).ToHashSet();

            return _sceneMappingService.GetAllByTvdbId(series.TvdbId)
                                       .OrderBy(m => m.Title)
                                       .ThenBy(m => m.SeasonNumber)
                                       .Select(m =>
                                       {
                                           var resource = m.ToResource();
                                           resource.IsExcluded = excludedParseTerms.Contains(m.ParseTerm);

                                           return resource;
                                       })
                                       .ToList();
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public AlternateTitleResource AddAlternateTitle([FromBody] AlternateTitleAddResource resource)
        {
            var series = _seriesService.GetSeries(resource.SeriesId);

            var mapping = _sceneMappingService.AddCustomMapping(new SceneMapping
            {
                TvdbId = series.TvdbId,
                Title = resource.Title,
                SeasonNumber = resource.SeasonNumber,
                SceneSeasonNumber = resource.SceneSeasonNumber
            });

            return mapping.ToResource();
        }

        [RestDeleteById]
        public void DeleteAlternateTitle(int id)
        {
            _sceneMappingService.DeleteCustomMapping(id);
        }

        [HttpPut("exclusion")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public object SetExclusion([FromBody] AlternateTitleExclusionResource resource)
        {
            var series = _seriesService.GetSeries(resource.SeriesId);

            if (resource.IsExcluded)
            {
                var parseTerm = resource.Title?.CleanSeriesTitle();

                if (_sceneMappingService.GetAllByTvdbId(series.TvdbId).None(m => m.ParseTerm == parseTerm))
                {
                    throw new NzbDroneClientException(HttpStatusCode.NotFound, "Alternate title '{0}' not found for series", resource.Title);
                }

                _sceneMappingService.Exclude(series.TvdbId, resource.Title);
            }
            else
            {
                _sceneMappingService.RemoveExclusion(series.TvdbId, resource.Title);
            }

            return new { };
        }
    }
}
