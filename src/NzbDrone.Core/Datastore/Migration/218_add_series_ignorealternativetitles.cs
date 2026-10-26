using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(218)]
    public class add_series_ignorealternativetitles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Series").AddColumn("IgnoreAlternateTitles").AsBoolean().WithDefaultValue(true);
        }
    }
}
