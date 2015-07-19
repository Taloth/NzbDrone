﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Torznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.TorznabTests
{
    public class TorznabRequestGeneratorFixture : CoreTest<TorznabRequestGenerator>
    {
        private TorznabCapabilities _capabilities = new TorznabCapabilities();

        private SingleEpisodeSearchCriteria _singleEpisodeSearchCriteria;
        private AnimeEpisodeSearchCriteria _animeSearchCriteria;

        [SetUp]
        public void SetUp()
        {
            Subject.Settings = new TorznabSettings()
            {
                 Url = "http://127.0.0.1:1234/",
                 Categories = new [] { 1, 2 },
                 AnimeCategories = new [] { 3, 4 },
                 ApiKey = "abcd",
                 EnableRageIDLookup = true
            };

            _singleEpisodeSearchCriteria = new SingleEpisodeSearchCriteria
            {
                Series = new Tv.Series { TvRageId = 10 },
                SceneTitles = new List<string> { "Monkey Island" },
                SeasonNumber = 1,
                EpisodeNumber = 2
            };

            _animeSearchCriteria = new AnimeEpisodeSearchCriteria()
            {
                SceneTitles = new List<String>() { "Monkey+Island" },
                AbsoluteEpisodeNumber = 100
            };

            Mocker.GetMock<ITorznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<TorznabSettings>()))
                .Returns(_capabilities);
        }
        
        [Test]
        public void should_return_one_page_for_feed()
        {
            var results = Subject.GetRecentRequests();

            results.Should().HaveCount(1);

            var pages = results.First().Take(10).ToList();

            pages.Should().HaveCount(1);
        }

        [Test]
        public void should_use_all_categories_for_feed()
        {
            var results = Subject.GetRecentRequests();

            results.Should().HaveCount(1);

            var page = results.First().First();

            page.Url.Query.Should().Contain("&cat=1,2,3,4&");
        }

        [Test]
        public void should_not_have_duplicate_categories()
        {
            Subject.Settings.Categories = new[] { 1, 2, 3 };

            var results = Subject.GetRecentRequests();

            results.Should().HaveCount(1);

            var page = results.First().First();

            page.Url.Query.Should().Contain("&cat=1,2,3,4&");
        }

        [Test]
        public void should_use_only_anime_categories_for_anime_search()
        {
            var results = Subject.GetSearchRequests(_animeSearchCriteria);

            results.Should().HaveCount(1);

            var page = results.First().First();

            page.Url.Query.Should().Contain("&cat=3,4&");
        }
        
        [Test]
        public void should_use_mode_search_for_anime()
        {
            var results = Subject.GetSearchRequests(_animeSearchCriteria);

            results.Should().HaveCount(1);

            var page = results.First().First();

            page.Url.Query.Should().Contain("?t=search&");
        }

        [Test]
        public void should_return_subsequent_pages()
        {
            var results = Subject.GetSearchRequests(_animeSearchCriteria);

            results.Should().HaveCount(1);

            var pages = results.First().Take(3).ToList();

            pages[0].Url.Query.Should().Contain("&offset=0&");
            pages[1].Url.Query.Should().Contain("&offset=100&");
            pages[2].Url.Query.Should().Contain("&offset=200&");
        }

        [Test]
        public void should_not_get_unlimited_pages()
        {
            var results = Subject.GetSearchRequests(_animeSearchCriteria);

            results.Should().HaveCount(1);

            var pages = results.First().Take(500).ToList();

            pages.Count.Should().BeLessThan(500);
        }

        [Test]
        public void should_not_search_by_rid_if_not_supported()
        {
            _capabilities.SupportedTvSearchParameters = new[] { "q", "season", "ep" };

            var results = Subject.GetSearchRequests(_singleEpisodeSearchCriteria);

            results.Should().HaveCount(1);

            var page = results.First().First();

            page.Url.Query.Should().NotContain("rid=10");
            page.Url.Query.Should().Contain("q=Monkey");
        }

        [Test]
        public void should_search_by_rid_if_supported()
        {
            var results = Subject.GetSearchRequests(_singleEpisodeSearchCriteria);
            results.Should().HaveCount(1);

            var page = results.First().First();

            page.Url.Query.Should().Contain("rid=10");
        }
    }
}
