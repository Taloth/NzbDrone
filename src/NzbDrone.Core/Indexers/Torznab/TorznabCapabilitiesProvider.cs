﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Indexers.Torznab
{
    public interface ITorznabCapabilitiesProvider
    {
        TorznabCapabilities GetCapabilities(TorznabSettings settings);
    }

    public class TorznabCapabilitiesProvider : ITorznabCapabilitiesProvider
    {
        private readonly ICached<TorznabCapabilities> _capabilitiesCache;
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public TorznabCapabilitiesProvider(ICacheManager cacheManager, IHttpClient httpClient, Logger logger)
        {
            _capabilitiesCache = cacheManager.GetCache<TorznabCapabilities>(GetType());
            _httpClient = httpClient;
            _logger = logger;
        }

        public TorznabCapabilities GetCapabilities(TorznabSettings indexerSettings)
        {
            var key = indexerSettings.ToJson();
            var capabilities = _capabilitiesCache.Get(key, () => FetchCapabilities(indexerSettings), TimeSpan.FromDays(7));

            return capabilities;
        }

        private TorznabCapabilities FetchCapabilities(TorznabSettings indexerSettings)
        {
            var capabilities = new TorznabCapabilities();

            var url = string.Format("{0}/api?t=caps", indexerSettings.Url.TrimEnd('/'));

            if (indexerSettings.ApiKey.IsNotNullOrWhiteSpace())
            {
                url += "&apikey=" + indexerSettings.ApiKey;
            }

            var request = new HttpRequest(url, HttpAccept.Rss);

            try
            {
                var response = _httpClient.Get(request);

                capabilities = ParseCapabilities(response);
            }
            catch (Exception ex)
            {
                _logger.DebugException(string.Format("Failed to get capabilities from {0}: {1}", indexerSettings.Url, ex.Message), ex);
            }

            return capabilities;
        }

        private TorznabCapabilities ParseCapabilities(HttpResponse response)
        {
            var capabilities = new TorznabCapabilities();

            var document = XDocument.Load(response.Content);

            var xmlSearching = document.Element("searching");
            if (xmlSearching != null)
            {
                var xmlBasicSearch = xmlSearching.Element("search");
                if (xmlBasicSearch == null || xmlBasicSearch.Attribute("available").Value != "yes")
                {
                    capabilities.SupportedSearchParameters = null;
                }
                else if (xmlBasicSearch.Attribute("supportedParams") != null)
                {
                    capabilities.SupportedSearchParameters = xmlBasicSearch.Attribute("supportedParams").Value.Split(',');
                }

                var xmlTvSearch = xmlSearching.Element("tv-search");
                if (xmlTvSearch == null || xmlTvSearch.Attribute("available").Value != "yes")
                {
                    capabilities.SupportedTvSearchParameters = null;
                }
                else if (xmlTvSearch.Attribute("supportedParams") != null)
                {
                    capabilities.SupportedTvSearchParameters = xmlTvSearch.Attribute("supportedParams").Value.Split(',');
                }
            }

            var xmlCategories = document.Element("categories");
            if (xmlCategories != null)
            {
                foreach (var xmlCategory in xmlCategories.Elements("category"))
                {
                    var cat = new TorznabCategory
                    {
                        Id = int.Parse(xmlCategory.Attribute("id").Value),
                        Name = xmlCategory.Attribute("name").Value,
                        Description = xmlCategory.Attribute("description") != null ? xmlCategory.Attribute("description").Value : string.Empty,
                        Subcategories = new List<TorznabCategory>()
                    };

                    foreach (var xmlSubcat in xmlCategory.Elements("subcat"))
                    {
                        cat.Subcategories.Add(new TorznabCategory
                        {
                            Id = int.Parse(xmlSubcat.Attribute("id").Value),
                            Name = xmlSubcat.Attribute("name").Value,
                            Description = xmlSubcat.Attribute("description") != null ? xmlCategory.Attribute("description").Value : string.Empty

                        });
                    }

                    capabilities.Categories.Add(cat);
                }
            }

            return capabilities;
        }
    }
}
