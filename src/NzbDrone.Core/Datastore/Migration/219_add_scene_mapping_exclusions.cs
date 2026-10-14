using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(219)]
    public class add_scene_mapping_exclusions : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("SceneMappingExclusions")
                  .WithColumn("TvdbId").AsInt32().NotNullable()
                  .WithColumn("ParseTerm").AsString().NotNullable();

            Create.Index().OnTable("SceneMappingExclusions").OnColumn("TvdbId").Ascending()
                                                             .OnColumn("ParseTerm").Ascending()
                                                             .WithOptions().Unique();
        }
    }
}
