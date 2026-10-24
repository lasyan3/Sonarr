using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class CompleteSeriesSearchCommand : Command
    {
        public int SeriesId { get; set; }

        public override bool SendUpdatesToClient => true;

        public CompleteSeriesSearchCommand()
        {
        }

        public CompleteSeriesSearchCommand(int seriesId)
        {
            SeriesId = seriesId;
        }
    }
}
