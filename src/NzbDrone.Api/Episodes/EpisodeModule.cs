using System.Collections.Generic;
using NzbDrone.Api.REST;
using NzbDrone.Core.Tv;
using NzbDrone.Core.DecisionEngine;

namespace NzbDrone.Api.Episodes
{
    public class EpisodeModule : EpisodeModuleWithSignalR
    {
        public EpisodeModule(ISeriesService seriesService,
                             IEpisodeService episodeService,
                             IQualityUpgradableSpecification qualityUpgradableSpecification)
            : base(episodeService, seriesService, qualityUpgradableSpecification)
        {
            GetResourceAll = GetEpisodes;
            UpdateResource = SetMonitored;
        }

        private List<EpisodeResource> GetEpisodes()
        {
            if (!Request.Query.SeriesId.HasValue)
            {
                throw new BadRequestException("seriesId is missing");
            }

            var seriesId = (int)Request.Query.SeriesId;

            var resources = MapToResource(_episodeService.GetEpisodeBySeries(seriesId), false, true);

            return resources;
        }

        private void SetMonitored(EpisodeResource episodeResource)
        {
            _episodeService.SetEpisodeMonitored(episodeResource.Id, episodeResource.Monitored);
        }
    }
}
