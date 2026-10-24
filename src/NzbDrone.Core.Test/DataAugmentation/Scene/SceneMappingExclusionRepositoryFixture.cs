using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DataAugmentation.Scene
{
    [TestFixture]
    public class SceneMappingExclusionRepositoryFixture : DbTest<SceneMappingExclusionRepository, SceneMappingExclusion>
    {
        [Test]
        public void should_find_exclusions_by_tvdb_id()
        {
            Subject.Insert(new SceneMappingExclusion { TvdbId = 100, ParseTerm = "amareto" });
            Subject.Insert(new SceneMappingExclusion { TvdbId = 101, ParseTerm = "amareto" });

            var exclusions = Subject.FindByTvdbId(100);

            exclusions.Should().HaveCount(1);
            exclusions[0].ParseTerm.Should().Be("amareto");
        }

        [Test]
        public void should_not_allow_duplicate_exclusion()
        {
            Subject.Insert(new SceneMappingExclusion { TvdbId = 100, ParseTerm = "amareto" });

            Assert.Catch<Exception>(() => Subject.Insert(new SceneMappingExclusion { TvdbId = 100, ParseTerm = "amareto" }));
        }
    }
}
