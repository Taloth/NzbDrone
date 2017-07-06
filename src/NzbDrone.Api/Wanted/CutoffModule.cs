﻿using NzbDrone.Api.Episodes;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Tv;

namespace NzbDrone.Api.Wanted
{
    public class CutoffModule : EpisodeModuleWithSignalR
    {
        private readonly IEpisodeCutoffService _episodeCutoffService;

        public CutoffModule(IEpisodeCutoffService episodeCutoffService,
                            IEpisodeService episodeService,
                            ISeriesService seriesService,
                            IQualityUpgradableSpecification qualityUpgradableSpecification)
            : base(episodeService, seriesService, qualityUpgradableSpecification, "wanted/cutoff")
        {
            _episodeCutoffService = episodeCutoffService;
            GetResourcePaged = GetCutoffUnmetEpisodes;
        }

        private PagingResource<EpisodeResource> GetCutoffUnmetEpisodes(PagingResource<EpisodeResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<EpisodeResource, Episode>("airDateUtc", SortDirection.Descending);

            if (pagingResource.FilterKey == "monitored" && pagingResource.FilterValue == "false")
            {
                pagingSpec.FilterExpression = v => v.Monitored == false || v.Series.Monitored == false;
            }
            else
            {
                pagingSpec.FilterExpression = v => v.Monitored == true && v.Series.Monitored == true;
            }

            var resource = ApplyToPage(_episodeCutoffService.EpisodesWhereCutoffUnmet, pagingSpec, v => MapToResource(v, true, true));

            return resource;
        }
    }
}
