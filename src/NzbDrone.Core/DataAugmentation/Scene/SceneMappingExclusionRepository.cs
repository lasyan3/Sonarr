using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.DataAugmentation.Scene
{
    public interface ISceneMappingExclusionRepository : IBasicRepository<SceneMappingExclusion>
    {
        List<SceneMappingExclusion> FindByTvdbId(int tvdbId);
    }

    public class SceneMappingExclusionRepository : BasicRepository<SceneMappingExclusion>, ISceneMappingExclusionRepository
    {
        public SceneMappingExclusionRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<SceneMappingExclusion> FindByTvdbId(int tvdbId)
        {
            return Query(x => x.TvdbId == tvdbId);
        }
    }
}
