using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.DataAugmentation.Scene
{
    public class SceneMappingExclusion : ModelBase
    {
        public int TvdbId { get; set; }
        public string ParseTerm { get; set; }
    }
}
